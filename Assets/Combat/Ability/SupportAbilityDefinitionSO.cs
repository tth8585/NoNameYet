using UnityEngine;

namespace TTH.Combat.Ability
{
    [CreateAssetMenu(menuName = "TTH/Combat/Support Ability Definition", fileName = "Support_")]
    public sealed class SupportAbilityDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string supportId = "support_id";
        public AbilityTagSO[] requiredTags;
        public AbilityTagSO[] excludedTags;

        [Header("Reusable Modifiers")]
        public AbilitySupportModifierSO[] modifiers;

        [Header("Legacy Modifiers")]
        [HideInInspector]
        public float damageMultiplier = 0f;

        [HideInInspector]
        public float flatDamageBonus = 0f;

        [HideInInspector]
        public float healMultiplier = 0f;

        [HideInInspector]
        public float armorBonus = 0f;

        [HideInInspector]
        public float manaCostReduction = 0f;

        [HideInInspector]
        public float manaCostIncrease = 0f;

        [HideInInspector]
        public float durationMultiplier = 1f;

        [HideInInspector]
        public float cooldownReduction = 0f;

        public float GetModifierValue(AbilitySupportModifierType type)
        {
            float total = GetLegacyModifierValue(type);
            if (modifiers == null) return total;

            for (int i = 0; i < modifiers.Length; i++)
            {
                var modifier = modifiers[i];
                if (modifier != null && modifier.modifierType == type)
                    total += modifier.value;
            }

            return total;
        }

        private float GetLegacyModifierValue(AbilitySupportModifierType type)
        {
            return type switch
            {
                AbilitySupportModifierType.DamageMultiplier => damageMultiplier,
                AbilitySupportModifierType.FlatDamageBonus => flatDamageBonus,
                AbilitySupportModifierType.HealMultiplier => healMultiplier,
                AbilitySupportModifierType.ManaCostReduction => manaCostReduction,
                AbilitySupportModifierType.ManaCostIncrease => manaCostIncrease,
                AbilitySupportModifierType.DurationMultiplier => durationMultiplier,
                AbilitySupportModifierType.CooldownReduction => cooldownReduction,
                AbilitySupportModifierType.ArmorBonus => armorBonus,
                _ => 0f
            };
        }

        public bool Matches(AbilityDefinition ability)
        {
            if (ability == null) return false;

            if (requiredTags != null)
            {
                for (int i = 0; i < requiredTags.Length; i++)
                {
                    var requiredTag = requiredTags[i];
                    if (requiredTag == null) continue;
                    if (!ContainsTag(ability.tags, requiredTag)) return false;
                }
            }

            if (excludedTags != null)
            {
                for (int i = 0; i < excludedTags.Length; i++)
                {
                    var excludedTag = excludedTags[i];
                    if (excludedTag != null && ContainsTag(ability.tags, excludedTag))
                        return false;
                }
            }

            return true;
        }

        private static bool ContainsTag(AbilityTagSO[] tags, AbilityTagSO tag)
        {
            if (tags == null) return false;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }
    }
}
