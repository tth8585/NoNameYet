using UnityEngine;

[CreateAssetMenu(menuName = "TTH/Game/Character Stats", fileName = "CharacterStats_")]
public sealed class CharacterStatsSO : ScriptableObject
{
    [Header("Primary Stats")]
    [Min(0f)] public float hitPoints = 100f;
    [Min(0f)] public float magicPoints = 150f;
    [Min(0f)] public float attack = 23f;
    [Min(0f)] public float defense = 0f;
    [Min(0f)] public float speed = 17f;
    [Min(0f)] public float dexterity = 17f;
    [Min(0f)] public float vitality = 17f;
    [Min(0f)] public float wisdom = 17f;

}