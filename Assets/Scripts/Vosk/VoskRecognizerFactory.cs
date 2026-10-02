using Vosk;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class VoskRecognizerFactory
{
    public static VoskRecognizer CreateWithGrammar(Model model, List<string> spellPhrases)
    {
        var allPhrases = new List<string>(spellPhrases);
        //if (includeUnknown) allPhrases.Add("[unk]");

        string grammarJson = BuildJsonArray(allPhrases);
        Debug.Log($"[VOSK] Grammar construída: {grammarJson}");

        var recognizer = new VoskRecognizer(model, MicrophoneStreamer.TargetSampleRate, grammarJson);
        recognizer.SetWords(true);
        return recognizer;

        /*var allPhrases = new List<string>(spellPhrases) { "[unk]" };
        string grammarJson = BuildJsonArray(allPhrases);



        var recognizer = new VoskRecognizer(model, MicrophoneStreamer.TargetSampleRate, grammarJson);
        recognizer.SetWords(true);
        return recognizer;*/
    }

    private static string BuildJsonArray(List<string> items)
    {
        var sb = new StringBuilder();
        sb.Append("[");
        for (int i = 0; i < items.Count; i++)
        {
            sb.Append("\"").Append(items[i].Replace("\"", "\\\"")).Append("\"");
            if (i < items.Count - 1) sb.Append(",");
        }
        sb.Append("]");
        return sb.ToString();
    }
}