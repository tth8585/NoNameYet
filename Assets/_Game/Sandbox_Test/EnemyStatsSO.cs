using UnityEngine;

[CreateAssetMenu(menuName = "TTH/Game/Enemy Stats", fileName = "EnemyStats_")]
public sealed class EnemyStatsSO : ScriptableObject
{
    [Header("Resources")]
    [Min(0f)] public float maxHealth = 200f;
    [Min(0f)] public float maxMana;

    [Header("Combat Stats")]
    [Min(0f)] public float attack = 20f;
    [Min(0f)] public float defense = 0f;
    [Min(0f)] public float dexterity = 10f;
}