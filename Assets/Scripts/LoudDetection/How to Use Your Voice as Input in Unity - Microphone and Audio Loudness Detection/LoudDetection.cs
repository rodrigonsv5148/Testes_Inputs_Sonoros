using System.Drawing;
using UnityEngine;

public class LoudDetection : MonoBehaviour
{
    [SerializeField] private MicrophoneStreamer microphone;

    //[SerializeField] private int sampleWindow = 128;
    [SerializeField] private float loudnessThreshold = 0.02f;
    [SerializeField] private float silenceDuration = 0.5f;

    private AudioClip microphoneClip;
    private string microphoneName;
    private float silenceTimer;

    public bool IsSpeaking { get; private set; }

    private int color;
    float recognitionDuration;
    float speechEndTime;
    float speechStartTime;

    void Start()
    {
        //MicrofoneToAudioClip();
    }

    void Update()
    {
        //float loudness = GetLoudnessFromMicrophone();

        float loudness = microphone.CurrentLoudness;


        if (loudness > loudnessThreshold)
        {
            // Som alto: está falando e zera o contador de silêncio
            IsSpeaking = true;
            silenceTimer = 0f;
            speechStartTime = Time.realtimeSinceStartup;
            Debug.Log("começou a falar");
        }
        else if (IsSpeaking)
        {
            // Som baixo: só considera que parou depois de um tempo em silêncio
            silenceTimer += Time.deltaTime;

            if (silenceTimer >= silenceDuration) 
            {
                IsSpeaking = false;
                speechEndTime = Time.realtimeSinceStartup;
                Debug.Log("parou de falar");
            }
        }
    }

    public float GetLoudnessFromMicrophone() 
    {
        return microphone.CurrentLoudness;
    }

    /*public void MicrofoneToAudioClip()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("Nenhum microfone encontrado!");
            return;
        }

        microphoneName = Microphone.devices[0];
        microphoneClip = Microphone.Start(microphoneName, true, 20, AudioSettings.outputSampleRate);
    }

    public float GetLoudnessFromMicrophone()
    {
        if (microphoneClip == null)
            return 0;

        return GetLoudnessFromAudioClip(Microphone.GetPosition(microphoneName), microphoneClip);
    }

    public float GetLoudnessFromAudioClip(int clipPosition, AudioClip clip)
    {
        int startPosition = clipPosition - sampleWindow;

        if (startPosition < 0)
            return 0;

        float[] waveData = new float[sampleWindow];
        clip.GetData(waveData, startPosition);

        float totalLoudness = 0;

        for (int i = 0; i < sampleWindow; i++)
        {
            totalLoudness += Mathf.Abs(waveData[i]);
        }

        return totalLoudness / sampleWindow;
    }*/

    public void CalculateTimeToRecognize(float recognitionTime, string _hexColor)
    {
        recognitionDuration = recognitionTime - speechEndTime;
        color = Random.Range(0, ColorList.colors.Length);

        Debug.Log(
            $"<color=#{_hexColor}>Tempo de reconhecimento: </color>" +
            $"<color=#{ColorList.colors[color]}>{recognitionDuration:F3}s</color>" +
            $"\nDuração da fala: {speechEndTime - speechStartTime:F3}s"
        );
    }
}