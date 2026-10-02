using UnityEngine;
using Vosk;

public class VoskRecognizerSmokeTest : MonoBehaviour
{
    void Start()
    {
        Vosk.Vosk.SetLogLevel(0);
        string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "vosk-model-small-pt");

        var model = new Model(modelPath);
        Debug.Log("Modelo carregado.");

        var recognizer = new VoskRecognizer(model, 16000f);
        Debug.Log("Recognizer criado com sucesso.");

        recognizer.Dispose();
        model.Dispose();
    }
}