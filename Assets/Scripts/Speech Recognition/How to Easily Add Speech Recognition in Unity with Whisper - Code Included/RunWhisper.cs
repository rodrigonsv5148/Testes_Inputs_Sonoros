using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;

[DefaultExecutionOrder(10)] // roda depois do MicrophoneStreamer (-10) e do LoudDetection (0)
public class RunWhisper : MonoBehaviour
{
    Worker decoder1, decoder2, encoder, spectrogram;
    Worker argmax;

    [Header("Fontes")]
    public MicrophoneStreamer microphone;
    public LoudDetection loudDetection;

    [Header("Segmentação")]
    [Tooltip("Segmentos menores que isso (s) são descartados.")]
    public float minSegmentSeconds = 0.4f;
    [Tooltip("Áudio guardado antes do início da fala (s), para não cortar a primeira sílaba.")]
    public float preRollSeconds = 0.3f;

    [Header("Debug")]
    [Tooltip("Cor (hex, sem #) do rótulo no log de tempo.")]
    public string logColorHex = "00FFFF";

    public event Action<string> OnTranscription;

    const int maxTokens = 200;

    const int END_OF_TEXT = 50257;
    const int START_OF_TRANSCRIPT = 50258;
    const int PORTUGUESE = 50267;
    const int TRANSCRIBE = 50359;
    const int NO_TIME_STAMPS = 50363;

    const int PERIOD = 13;
    const int EXCLAMATION = 0;

    static readonly (string name, int[] tokens)[] spellSequences =
    {
	// Ecovisão
	("Ecovisão", new[] { 40263, 4938, 1076 }),
    ("Ecovisão", new[] { 30226, 4938, 1076 }),
    ("Ecovisão", new[] { 40263, 49949 }),
    ("Ecovisão", new[] { 30226, 49949 }),

	// Cartografia sonora de Isolde
	("Cartografia sonora de Isolde", new[] { 22478, 19815, 654, 1872, 3252, 368, 286, 45537, 68 }),
    ("Cartografia sonora de Isolde", new[] { 22478, 19815, 654, 5185, 3252, 368, 286, 45537, 68 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 286, 45537, 68 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 307, 2641, 68 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 286, 1404, 368 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 741, 1404, 368 }),

	// Variantes "Izolde"
	("Cartografia sonora de Isolde", new[] { 22478, 19815, 654, 1872, 3252, 368, 30296, 2641, 68 }),
    ("Cartografia sonora de Isolde", new[] { 22478, 19815, 654, 5185, 3252, 368, 30296, 2641, 68 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 30296, 2641, 68 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 14736, 2641, 68 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 286, 710, 401, 368 }),
    ("Cartografia sonora de Isolde", new[] { 5467, 19815, 654, 1872, 3252, 368, 741, 710, 401, 368 }),
};

    class TrieNode
    {
        public Dictionary<int, TrieNode> next = new Dictionary<int, TrieNode>();
        public string spell; // preenchido onde um feitiço está completo
    }

    TrieNode trieRoot = new TrieNode(), trieCurrent;
    readonly float[] maskData = new float[51865];

    [Header("Feitiços")]
    [Tooltip("Fração máxima de passos em que o Whisper 'discorda' do token forçado. Acima disso, rejeita.")]
    [Range(0f, 1f)] public float maxMismatchRatio = 0.5f;
    public event Action<string> OnSpellRecognized;

    int mismatches, stepCount;

    void BuildTrie()
    {
        foreach (var (name, seq) in spellSequences)
        {
            var node = trieRoot;
            foreach (int t in seq)
            {
                if (!node.next.TryGetValue(t, out var child))
                    node.next[t] = child = new TrieNode();
                node = child;
            }

            node.spell = name;
            AddEnd(node, name);

            // aceita ponto ou exclamação no final
            foreach (int p in new[] { PERIOD, EXCLAMATION })
            {
                if (!node.next.TryGetValue(p, out var punct))
                    node.next[p] = punct = new TrieNode();
                punct.spell = name;
                AddEnd(punct, name);
            }
        }
    }

    void AddEnd(TrieNode node, string name)
    {
        if (!node.next.ContainsKey(END_OF_TEXT))
            node.next[END_OF_TEXT] = new TrieNode { spell = name };
    }

    void BuildMask(TrieNode node)
    {
        Array.Fill(maskData, -1e9f);
        foreach (int t in node.next.Keys) maskData[t] = 0f;
    }

    const int SampleRate = MicrophoneStreamer.TargetSampleRate;
    const int maxSamples = 30 * SampleRate;
    const int maxSegmentSamples = 29 * SampleRate;

    string[] tokens;

    int tokenCount = 0;
    NativeArray<int> outputTokens;

    int[] whiteSpaceCharacters = new int[256];

    Tensor<float> encodedAudio;

    string outputString = "";

    public ModelAsset audioDecoder1, audioDecoder2;
    public ModelAsset audioEncoder;
    public ModelAsset logMelSpectro;
    public TextAsset vocabAsset;

    NativeArray<int> lastToken;
    Tensor<int> lastTokenTensor;
    Tensor<int> tokensTensor;
    Tensor<float> audioInput;

    // Segmentação (guiada pelo LoudDetection.IsSpeaking)
    readonly List<float> segment = new List<float>();
    readonly List<float> preRoll = new List<float>();
    readonly Queue<float[]> pendingSegments = new Queue<float[]>();
    bool inSegment = false;

    void Awake()
    {
        if (microphone == null)
            microphone = FindFirstObjectByType<MicrophoneStreamer>();
        if (loudDetection == null)
            loudDetection = FindFirstObjectByType<LoudDetection>();
    }

    // Roda todo frame: drena os chunks e os classifica conforme o IsSpeaking atual,
    // mesmo enquanto o Whisper está ocupado transcrevendo.
    void Update()
    {
        if (microphone == null || loudDetection == null) return;

        int preRollMax = Mathf.RoundToInt(preRollSeconds * SampleRate);

        while (microphone.AudioChunks.TryDequeue(out short[] chunk))
        {
            float[] f = new float[chunk.Length];
            for (int i = 0; i < chunk.Length; i++)
                f[i] = chunk[i] / 32768f;

            if (loudDetection.IsSpeaking)
            {
                if (!inSegment)
                {
                    inSegment = true;
                    segment.AddRange(preRoll);
                    preRoll.Clear();
                }

                segment.AddRange(f);

                if (segment.Count >= maxSegmentSamples)
                    FinishSegment(); // limite do Whisper (30 s)
            }
            else
            {
                if (inSegment)
                    FinishSegment(); // o LoudDetection confirmou o silêncio

                preRoll.AddRange(f);
                if (preRoll.Count > preRollMax)
                    preRoll.RemoveRange(0, preRoll.Count - preRollMax);
            }
        }
    }

    void FinishSegment()
    {
        if (segment.Count >= Mathf.RoundToInt(minSegmentSeconds * SampleRate))
            pendingSegments.Enqueue(segment.ToArray());

        segment.Clear();
        inSegment = false;
    }

    public async void Start()
    {
        if (microphone == null || loudDetection == null)
        {
            Debug.LogError("RunWhisper precisa de MicrophoneStreamer e LoudDetection na cena.");
            return;
        }

        SetupWhiteSpaceShifts();
        GetTokens();
        BuildTrie();
        ValidateSpellTokens();

        decoder1 = new Worker(ModelLoader.Load(audioDecoder1), BackendType.GPUCompute);
        decoder2 = new Worker(ModelLoader.Load(audioDecoder2), BackendType.GPUCompute);

        FunctionalGraph graph = new FunctionalGraph();
        var input = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, 51865));
        var maskInput = graph.AddInput(DataType.Float, new TensorShape(1, 1, 51865));
        var masked = Functional.ArgMax(input + maskInput, -1, false);
        var free = Functional.ArgMax(input, -1, false);
        argmax = new Worker(graph.Compile(masked, free), BackendType.GPUCompute);

        encoder = new Worker(ModelLoader.Load(audioEncoder), BackendType.GPUCompute);
        spectrogram = new Worker(ModelLoader.Load(logMelSpectro), BackendType.GPUCompute);

        outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);
        lastToken = new NativeArray<int>(1, Allocator.Persistent);

        tokensTensor = new Tensor<int>(new TensorShape(1, maxTokens));
        ComputeTensorData.Pin(tokensTensor);
        lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { NO_TIME_STAMPS });

        try
        {
            while (true)
            {
                if (pendingSegments.Count > 0)
                    await TranscribeSegment(pendingSegments.Dequeue());
                else
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // objeto destruído: encerra o loop silenciosamente
        }
    }

    async Awaitable TranscribeSegment(float[] samples)
    {
        PrepareAudio(samples);
        ResetDecoderState();

        bool ended = false;
        while (tokenCount < outputTokens.Length - 1)
        {
            ended = await InferenceStep();
            if (ended) break;
        }

        float ratio = stepCount > 0 ? (float)mismatches / stepCount : 1f;
        string spell = ended ? trieCurrent.spell : null;

        if (spell != null && ratio <= maxMismatchRatio)
        {
            Debug.Log($"Feitiço: {spell} (discordância {ratio:P0})");
            loudDetection.CalculateTimeToRecognize(Time.realtimeSinceStartup, logColorHex);
            OnSpellRecognized?.Invoke(spell);
        }
        else
        {
            Debug.Log($"Rejeitado (discordância {ratio:P0}): {outputString.Trim()}");
        }
    }

    void PrepareAudio(float[] samples)
    {
        var data = new float[maxSamples]; // o Whisper espera 30 s (restante em zero)
        Array.Copy(samples, data, Mathf.Min(samples.Length, maxSamples));

        audioInput?.Dispose();
        audioInput = new Tensor<float>(new TensorShape(1, maxSamples), data);

        spectrogram.Schedule(audioInput);
        var logmel = spectrogram.PeekOutput() as Tensor<float>;

        encoder.Schedule(logmel);
        encodedAudio = encoder.PeekOutput() as Tensor<float>;
    }

    void ResetDecoderState()
    {
        outputTokens[0] = START_OF_TRANSCRIPT;
        outputTokens[1] = PORTUGUESE;
        outputTokens[2] = TRANSCRIBE;
        tokenCount = 3;

        trieCurrent = trieRoot;
        mismatches = 0;
        stepCount = 0;

        lastToken[0] = NO_TIME_STAMPS;
        outputString = "";

        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);
        lastTokenTensor.dataOnBackend.Upload<int>(lastToken, 1);
    }

    // Retorna true quando o token de fim de texto foi gerado.
    async Awaitable<bool> InferenceStep()
    {
        decoder1.SetInput("input_ids", tokensTensor);
        decoder1.SetInput("encoder_hidden_states", encodedAudio);
        decoder1.Schedule();

        decoder2.SetInput("input_ids", lastTokenTensor);

        for (int i = 0; i < 4; i++)
        {
            decoder2.SetInput($"past_key_values.{i}.decoder.key", decoder1.PeekOutput($"present.{i}.decoder.key") as Tensor<float>);
            decoder2.SetInput($"past_key_values.{i}.decoder.value", decoder1.PeekOutput($"present.{i}.decoder.value") as Tensor<float>);
            decoder2.SetInput($"past_key_values.{i}.encoder.key", decoder1.PeekOutput($"present.{i}.encoder.key") as Tensor<float>);
            decoder2.SetInput($"past_key_values.{i}.encoder.value", decoder1.PeekOutput($"present.{i}.encoder.value") as Tensor<float>);
        }

        decoder2.Schedule();

        var logits = decoder2.PeekOutput("logits") as Tensor<float>;

        // Máscara: só os tokens que continuam algum feitiço recebem 0; o resto, -1e9
        BuildMask(trieCurrent);
        using var maskTensor = new Tensor<float>(new TensorShape(1, 1, 51865), maskData);
        argmax.Schedule(logits, maskTensor);

        // Saída 0: token forçado (com máscara). Saída 1: token que o Whisper escolheria livremente.
        using var t_Token = await argmax.PeekOutput(0).ReadbackAndCloneAsync() as Tensor<int>;
        using var t_Free = await argmax.PeekOutput(1).ReadbackAndCloneAsync() as Tensor<int>;

        int index = t_Token[0];

        // Contagem para o critério de rejeição
        stepCount++;
        if (t_Free[0] != index) mismatches++;

        // Avança na trie
        if (trieCurrent.next.TryGetValue(index, out var nextNode))
            trieCurrent = nextNode;

        outputTokens[tokenCount] = lastToken[0];
        lastToken[0] = index;
        tokenCount++;

        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);
        lastTokenTensor.dataOnBackend.Upload<int>(lastToken, 1);

        if (index == END_OF_TEXT)
            return true;

        if (index < tokens.Length)
            outputString += GetUnicodeText(tokens[index]);

        return false;
    }

    void GetTokens()
    {
        var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabAsset.text);

        tokens = new string[vocab.Count];

        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }
    }

    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }

    string ShiftCharacterDown(string text)
    {
        var sb = new StringBuilder();

        foreach (char letter in text)
        {
            sb.Append(((int)letter <= 256) ? letter : (char)whiteSpaceCharacters[(int)(letter - 256)]);
        }

        return sb.ToString();
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i))
                whiteSpaceCharacters[n++] = i;
        }
    }

    bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('¡' <= c && c <= '¬') || ('®' <= c && c <= 'ÿ'));
    }

    private void OnDestroy()
    {
        decoder1?.Dispose();
        decoder2?.Dispose();
        encoder?.Dispose();
        spectrogram?.Dispose();
        argmax?.Dispose();

        audioInput?.Dispose();
        lastTokenTensor?.Dispose();
        tokensTensor?.Dispose();

        if (outputTokens.IsCreated)
            outputTokens.Dispose();

        if (lastToken.IsCreated)
            lastToken.Dispose();
    }

    void ValidateSpellTokens()
    {
        foreach (var (name, seq) in spellSequences)
        {
            var sb = new StringBuilder();
            foreach (int t in seq)
                sb.Append(GetUnicodeText(tokens[t]));

            Debug.Log($"{name} -> '{sb}'");
        }
    }
}