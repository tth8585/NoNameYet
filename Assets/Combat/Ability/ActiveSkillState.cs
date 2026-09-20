using System;
using System.Collections.Generic;
using UnityEngine;

namespace TTH.Combat.Ability
{
    [Serializable]
    public sealed class ActiveSkillState
    {
        public AbilityDefinition definition;
        public AbilityActivationMode activationMode;
        public bool isActive;
        public float reservedMana;
        public float manaReservoir;
        public float activeDurationRemaining;
        public float duration;
        public List<SupportAbilityDefinitionSO> linkedSupports = new();

        public float FinalDamageMultiplier => 1f + GetSupportValue(AbilitySupportModifierType.DamageMultiplier);
        public float FinalFlatDamageBonus => GetSupportValue(AbilitySupportModifierType.FlatDamageBonus);
        public float FinalHealMultiplier => 1f + GetSupportValue(AbilitySupportModifierType.HealMultiplier);
        public float FinalArmorBonus => GetSupportValue(AbilitySupportModifierType.ArmorBonus);
        public float FinalManaCostMultiplier => Mathf.Max(0f, 1f - GetSupportValue(AbilitySupportModifierType.ManaCostReduction));
        public float FinalDurationMultiplier => Mathf.Max(0.01f, 1f + GetSupportValue(AbilitySupportModifierType.DurationMultiplier));
        public float FinalCooldownMultiplier => Mathf.Max(0.01f, 1f - GetSupportValue(AbilitySupportModifierType.CooldownReduction));

        public float ComputeEffectiveDamage(float baseDamage)
        {
            if (definition == null) return baseDamage;
            var total = baseDamage * FinalDamageMultiplier + FinalFlatDamageBonus;
            return Math.Max(0f, total);
        }

        public float ComputeEffectiveHeal(float baseHeal)
        {
            if (definition == null) return baseHeal;
            return Math.Max(0f, baseHeal * FinalHealMultiplier);
        }

        public float ComputeEffectiveManaCost(float baseCost)
        {
            if (definition == null) return baseCost;
            var reduced = baseCost * FinalManaCostMultiplier;
            return Math.Max(0f, reduced);
        }

        public float ComputeEffectiveCooldown(float baseCooldown)
        {
            if (definition == null) return baseCooldown;
            var reduced = baseCooldown * FinalCooldownMultiplier;
            return Math.Max(0f, reduced);
        }

        public float ComputeEffectiveDuration(float baseDuration)
        {
            if (definition == null) return baseDuration;
            var scaled = baseDuration * FinalDurationMultiplier;
            return Math.Max(0f, scaled);
        }

        public void AddSupport(SupportAbilityDefinitionSO support)
        {
            if (support == null) return;
            if (!linkedSupports.Contains(support)) linkedSupports.Add(support);
        }

        public void RemoveSupport(SupportAbilityDefinitionSO support)
        {
            if (support == null) return;
            linkedSupports.Remove(support);
        }

        private float GetSupportValue(AbilitySupportModifierType type)
        {
            if (linkedSupports == null || linkedSupports.Count == 0) return 0f;
            float total = 0f;
            for (int i = 0; i < linkedSupports.Count; i++)
            {
                var support = linkedSupports[i];
                if (support != null) total += support.GetModifierValue(type);
            }
            return total;
        }
    }
}
