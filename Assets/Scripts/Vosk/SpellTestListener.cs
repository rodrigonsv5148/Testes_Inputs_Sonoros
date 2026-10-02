using UnityEngine;

public class SpellTestListener : MonoBehaviour
{
    public SpellCastController controller;
    public LoudDetection loudDetector;

    void OnEnable()
    {
        controller.OnSpellCast += HandleSpellCast;
        controller.OnPartialSpeech += HandlePartialSpeech;
    }

    void OnDisable()
    {
        controller.OnSpellCast -= HandleSpellCast;
        controller.OnPartialSpeech -= HandlePartialSpeech;
    }

    void HandleSpellCast(SpellDefinition spell)
    {
        Debug.Log($"[VOSK] Feitiço reconhecido: {spell.displayName} ({spell.voskPhrase})");
        loudDetector.CalculateTimeToRecognize(Time.realtimeSinceStartup, ColorList.colors[1]);
    }

    void HandlePartialSpeech(string partialText)
    {
        Debug.Log($"[VOSK] Ouvindo: {partialText}");
        loudDetector.CalculateTimeToRecognize(Time.realtimeSinceStartup, ColorList.colors[2]);
    }
}