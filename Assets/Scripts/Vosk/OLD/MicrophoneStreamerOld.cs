using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class MicrophoneStreamerOld : MonoBehaviour
{
    public const int TargetSampleRate = 16000;

    private AudioClip _micClip;
    private string _micDevice;
    private int _lastSamplePos;
    private float[] _floatBuffer;

    // Fila thread-safe consumida pela thread do Vosk
    public ConcurrentQueue<short[]> AudioChunks { get; } = new ConcurrentQueue<short[]>();

    void Start()
    {
        _micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (_micDevice == null)
        {
            Debug.LogError("Nenhum microfone encontrado.");
            return;
        }

        // Pedimos 16000 Hz diretamente; a maioria dos drivers de desktop aceita.
        // Se o dispositivo não suportar, o Unity cai para a taxa nativa dele
        // e você precisa reamostrar manualmente (ver nota abaixo).
        _micClip = Microphone.Start(_micDevice, true, 10, TargetSampleRate);
        _lastSamplePos = 0;
    }

    void Update()
    {
        if (_micClip == null) return;

        int currentPos = Microphone.GetPosition(_micDevice);
        int sampleDiff = currentPos - _lastSamplePos;
        if (sampleDiff < 0) sampleDiff += _micClip.samples; // wrap do buffer circular
        if (sampleDiff <= 0) return;

        if (_floatBuffer == null || _floatBuffer.Length < sampleDiff)
            _floatBuffer = new float[sampleDiff];

        _micClip.GetData(_floatBuffer, _lastSamplePos);

        // Converte float [-1,1] para PCM16 (formato que o Vosk exige)
        short[] pcm16 = new short[sampleDiff];
        for (int i = 0; i < sampleDiff; i++)
        {
            float sample = Mathf.Clamp(_floatBuffer[i], -1f, 1f);
            pcm16[i] = (short)(sample * short.MaxValue);
        }

        AudioChunks.Enqueue(pcm16);
        _lastSamplePos = currentPos;
    }

    void OnDestroy()
    {
        if (_micDevice != null) Microphone.End(_micDevice);
    }
}