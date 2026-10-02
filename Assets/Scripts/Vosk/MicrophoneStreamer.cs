using System.Collections.Concurrent;
using UnityEngine;

[DefaultExecutionOrder(-10)] // roda antes dos scripts que consomem o volume
public class MicrophoneStreamer : MonoBehaviour
{
    public const int TargetSampleRate = 16000;

    private AudioClip _micClip;
    private string _micDevice;
    private int _lastSamplePos;

    public AudioClip MicClip => _micClip;
    public string MicDevice => _micDevice;

    // Volume (média do valor absoluto, 0..1) das amostras lidas no último frame com áudio novo
    public float CurrentLoudness { get; private set; }

    public ConcurrentQueue<short[]> AudioChunks { get; } = new ConcurrentQueue<short[]>();

    void Start()
    {
        _micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (_micDevice == null)
        {
            Debug.LogError("Nenhum microfone encontrado.");
            return;
        }

        _micClip = Microphone.Start(_micDevice, true, 10, TargetSampleRate);
        _lastSamplePos = 0;
    }

    void Update()
    {
        if (_micClip == null || !Microphone.IsRecording(_micDevice)) return;

        int currentPos = Microphone.GetPosition(_micDevice);
        if (currentPos <= 0 || currentPos == _lastSamplePos) return; // sem áudio novo: mantém o último volume

        int total = _micClip.samples;
        int count = currentPos - _lastSamplePos;
        if (count < 0) count += total; // wrap

        short[] pcm16 = new short[count];
        float sumAbs = 0f;

        if (_lastSamplePos + count <= total)
        {
            sumAbs += ReadInto(pcm16, 0, _lastSamplePos, count);
        }
        else
        {
            int firstPart = total - _lastSamplePos;
            sumAbs += ReadInto(pcm16, 0, _lastSamplePos, firstPart);
            sumAbs += ReadInto(pcm16, firstPart, 0, count - firstPart);
        }

        CurrentLoudness = sumAbs / count;

        AudioChunks.Enqueue(pcm16);
        _lastSamplePos = currentPos;
    }

    // Retorna a soma dos valores absolutos (para calcular o volume)
    private float ReadInto(short[] dest, int destOffset, int clipOffset, int length)
    {
        if (length <= 0) return 0f;

        float[] tmp = new float[length];
        _micClip.GetData(tmp, clipOffset);

        float sum = 0f;
        for (int i = 0; i < length; i++)
        {
            float s = Mathf.Clamp(tmp[i], -1f, 1f);
            sum += Mathf.Abs(s);
            dest[destOffset + i] = (short)(s * short.MaxValue);
        }
        return sum;
    }

    void OnDestroy()
    {
        if (_micDevice != null && Microphone.IsRecording(_micDevice))
            Microphone.End(_micDevice);
    }
}