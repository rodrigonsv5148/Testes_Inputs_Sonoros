using System;
using System.Collections.Generic;
using UnityEngine;
using Vosk;

public class SpellCastController : MonoBehaviour
{
    public SpellBook spellBook;
    public MicrophoneStreamer micStreamer;

    private Model _model;
    private VoskRecognizer _recognizer;
    private VoskRecognitionThread _recognitionThread;

    public event Action<SpellDefinition> OnSpellCast;
    public event Action<string> OnPartialSpeech; // para feedback visual de "ouvindo..."

    float time;

    void Start()
    {
        Vosk.Vosk.SetLogLevel(0); // silencia logs verbosos do Kaldi no console

        string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "vosk-model-small-pt");
        _model = new Model(modelPath);

        RebuildRecognizer();

        _recognitionThread = new VoskRecognitionThread(_recognizer, micStreamer.AudioChunks);
        _recognitionThread.Start();
    }

    public void RebuildRecognizer()
    {
        _recognizer?.Dispose();

        var vocabulary = new List<string>
        {
            "igni", "agaruda"
        };

        vocabulary = spellBook.GetActiveVocabulary();

        _recognizer = VoskRecognizerFactory.CreateWithGrammar(
            _model,
            vocabulary
        );
    }
    /*public void RebuildRecognizer()
    {
        _recognizer?.Dispose();

        //_recognizer = new VoskRecognizer(_model, MicrophoneStreamer.TargetSampleRate);


        _recognizer = VoskRecognizerFactory.CreateWithGrammar(_model, spellBook.GetActiveVocabulary());
    }*/

    void Update()
    {
        // Feedback visual em tempo real (opcional, mas melhora a percepção de latência)
        while (_recognitionThread.PartialResults.TryDequeue(out string partialJson))
        {
            var partial = JsonUtility.FromJson<PartialResultDTO>(partialJson);
            if (!string.IsNullOrEmpty(partial.partial))
                OnPartialSpeech?.Invoke(partial.partial);
        }

        // Resultado definitivo -> dispara o feitiço
        while (_recognitionThread.FinalResults.TryDequeue(out string finalJson))
        {
            /*var final = JsonUtility.FromJson<FinalResultDTO>(finalJson);
            if (string.IsNullOrEmpty(final.text) || final.text == "[unk]")
                continue; // silêncio ou fala não reconhecida como feitiço válido

            SpellDefinition spell = spellBook.FindByRecognizedPhrase(final.text.Trim());
            if (spell != null)
                OnSpellCast?.Invoke(spell);*/
                var final = JsonUtility.FromJson<FinalResultDTO>(finalJson);
                if (string.IsNullOrEmpty(final.text)) continue;

                float avgConfidence = 1f;
                if (final.result != null && final.result.Count > 0)
                {
                    float sum = 0f;
                    foreach (var w in final.result) sum += w.conf;
                    avgConfidence = sum / final.result.Count;
                }

                const float ConfidenceThreshold = 0.6f;
                if (avgConfidence < ConfidenceThreshold)
                {
                    Debug.Log($"[VOSK] Descartado por baixa confiança: {final.text} ({avgConfidence:F2})");
                    continue;
                }

                SpellDefinition spell = spellBook.FindByRecognizedPhrase(final.text.Trim());
                if (spell != null)
                    OnSpellCast?.Invoke(spell);
        }
    }

    void OnDestroy()
    {
        _recognitionThread?.Stop();
        _recognizer?.Dispose();
        _model?.Dispose();
    }

    [Serializable] private class PartialResultDTO { public string partial; }
    //[Serializable] private class FinalResultDTO { public string text; }

    [Serializable] private class WordInfo { public string word; public float conf; public float start; public float end; }
    [Serializable] private class FinalResultDTO { public string text; public List<WordInfo> result; }
}