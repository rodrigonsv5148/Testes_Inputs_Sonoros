using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Magia/Feitico")]
public class SpellDefinition : ScriptableObject
{
    [Tooltip("Nome exibido na UI")]
    public string displayName;

    [Tooltip("Frase EXATA que o Vosk deve reconhecer, em minúsculas e sem acentos")]
    public string voskPhrase; // ex: "ignis maximus"

    public GameObject effectPrefab;
    public float manaCost;
    public bool unlocked = true;
}