using UnityEngine;

namespace TTH.Combat.Ability
{
    public enum AbilitySupportModifierType
    {
        DamageMultiplier,
        FlatDamageBonus,
        HealMultiplier,
        ManaCostReduction,
        ManaCostIncrease,
        DurationMultiplier,
        CooldownReduction,
        ArmorBonus
    }

    [CreateAssetMenu(menuName = "TTH/Combat/Ability Support Modifier", fileName = "SupportModifier_")]
    public sealed class AbilitySupportModifierSO : ScriptableObject
    {
        public AbilitySupportModifierType modifierType;
        public float value;
    }
}
