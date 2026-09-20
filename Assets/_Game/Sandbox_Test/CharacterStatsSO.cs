using UnityEngine;

[CreateAssetMenu(menuName = "TTH/Game/Character Stats", fileName = "CharacterStats_")]
public sealed class CharacterStatsSO : ScriptableObject
{
    [Header("Primary Stats")]
    [Min(0f)] public float strength = 10f;
    [Min(0f)] public float intelligence = 8f;
    [Min(0f)] public float dexterity = 12f;

    [Header("Independent Combat Stats")]
    [Min(0f)] public float baseAttack = 20f;
    [Min(0f)] public float baseDefense = 5f;

    [Header("MVP Formulas")]
    [Min(0f)] public float hpPerStrength = 10f;
    [Min(0f)] public float mpPerIntelligence = 10f;

    public float MaxHealth => strength * hpPerStrength;
    public float MaxMana => intelligence * mpPerIntelligence;
    public float ActionSpeed => dexterity;
}