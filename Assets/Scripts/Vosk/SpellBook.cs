using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellBook", menuName = "Magia/Livro de Feiticos")]
public class SpellBook : ScriptableObject
{
    public List<SpellDefinition> spells = new List<SpellDefinition>();

    /// Retorna só os feitiços desbloqueados, prontos para virar grammar do Vosk
    public List<string> GetActiveVocabulary()
    {
        return spells.Where(s => s.unlocked)
                      .Select(s => s.voskPhrase)
                      .ToList();
    }

    public SpellDefinition FindByRecognizedPhrase(string phrase)
    {
        return spells.FirstOrDefault(s => s.unlocked && s.voskPhrase == phrase);
    }
}