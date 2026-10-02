using UnityEngine;
using Vosk;

public class VoskSmokeTest : MonoBehaviour
{
    void Start()
    {
        Vosk.Vosk.SetLogLevel(0);
        string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "vosk-model-small-pt");
        var model = new Model(modelPath);
        Debug.Log("Modelo Vosk carregado com sucesso.");
        model.Dispose();
    }
}