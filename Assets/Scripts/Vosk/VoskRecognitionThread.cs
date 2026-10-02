using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using Vosk;

public class VoskRecognitionThread
{
    private readonly VoskRecognizer _recognizer;
    private readonly ConcurrentQueue<short[]> _inputQueue;
    public ConcurrentQueue<string> PartialResults { get; } = new ConcurrentQueue<string>();
    public ConcurrentQueue<string> FinalResults { get; } = new ConcurrentQueue<string>();

    private Thread _thread;
    private volatile bool _running;

    public VoskRecognitionThread(VoskRecognizer recognizer, ConcurrentQueue<short[]> inputQueue)
    {
        _recognizer = recognizer;
        _inputQueue = inputQueue;
    }

    public void Start()
    {
        _running = true;
        _thread = new Thread(Run) { IsBackground = true, Priority = System.Threading.ThreadPriority.AboveNormal };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(500);
    }

    private void Run()
    {
        while (_running)
        {
            if (_inputQueue.TryDequeue(out short[] chunk))
            {
                // Converte short[] para byte[] (formato bruto que AcceptWaveform espera)
                byte[] bytes = new byte[chunk.Length * 2];
                Buffer.BlockCopy(chunk, 0, bytes, 0, bytes.Length);

                bool isFinal = _recognizer.AcceptWaveform(bytes, bytes.Length);
                if (isFinal)
                {
                    FinalResults.Enqueue(_recognizer.Result());
                }
                else
                {
                    PartialResults.Enqueue(_recognizer.PartialResult());
                }
            }
            else
            {
                Thread.Sleep(5); // evita busy-wait consumindo 100% de um core
            }
        }
    }
}