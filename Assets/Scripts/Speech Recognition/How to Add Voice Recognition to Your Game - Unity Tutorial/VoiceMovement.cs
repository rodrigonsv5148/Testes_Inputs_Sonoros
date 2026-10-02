using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceMovement : MonoBehaviour
{
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, Action> actions = new Dictionary<string, Action>();

    [SerializeField] private LoudDetection loudDetection;



    private void Start()
    {
        actions.Add("Cartografia Sonora de isolde", CartografiaSonoraDeisolde);
        actions.Add("Ecovisão", Ecovisao);

        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += RegocnizedSpeech;
        keywordRecognizer.Start();
        //keywordRecognizer.Stop(); para de ouvir
    }

    private void RegocnizedSpeech(PhraseRecognizedEventArgs speech) 
    {
        float recognitionTime = Time.realtimeSinceStartup;

        Debug.Log(speech.text);

        //Debug.Log("recognition time:" + recognitionTime);
        loudDetection.CalculateTimeToRecognize(recognitionTime, ColorList.colors[0]);

        actions[speech.text].Invoke();
    }

    private void CartografiaSonoraDeisolde() 
    {
        //loudDetection.CalculateTimeToAct(Time.realtimeSinceStartup, ColorList.colors[0]);
        //loudDetection.CalculateTimeToAct(Time.realtimeSinceStartup, ColorList.colors[0]);
        Debug.Log("act time:" + Time.realtimeSinceStartup);

        Debug.Log("Feitiço Cartografia sonora de isolde ativado");
    }

    private void Ecovisao()
    {
        //loudDetection.CalculateTimeToAct(Time.realtimeSinceStartup, ColorList.colors[0]);
        //loudDetection.CalculateTimeToAct(Time.realtimeSinceStartup, ColorList.colors[0]);
        Debug.Log("act time:" + Time.realtimeSinceStartup);

        Debug.Log("Feitiço Ecovisão ativado");
    }

}
