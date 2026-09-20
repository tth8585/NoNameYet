using UnityEngine;
using TTH.Combat.Ability;

[CreateAssetMenu(menuName = "TTH/Game/Relic Definition", fileName = "Relic_")]
public sealed class RelicDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    public string relicId = "relic_id";
    public string displayName = "Relic";
    [TextArea] public string description;

    [Header("Passive Skill")]
    public PassiveAbilityDefinitionSO passiveAbility;

    [Header("Active Skill (Optional)")]
    public AbilityDefinition activeAbility;
}