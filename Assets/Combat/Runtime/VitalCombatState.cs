using TTH.Combat.Attributes;
using UnityEngine;

namespace TTH.Combat.Runtime
{
    public sealed class VitalCombatState
    {
        private readonly AttributeSystem attributes;
        private readonly float armorProficiencyTimeReduction;

        public bool IsInCombat { get; private set; }
        public float TimeRemaining { get; private set; }
        public float CombatTrigger => CalculateCombatTrigger(attributes?.Get(AttributeId.DEF) ?? 0f);
        public float CombatDuration => Mathf.Max(1f,
            7f - 0.04f * Mathf.Max(0f, attributes?.Get(AttributeId.VIT) ?? 0f) - armorProficiencyTimeReduction);
        public float RegenMultiplier => IsInCombat ? 0.5f : 1f;

        public VitalCombatState(AttributeSystem attributes, float armorProficiencyTimeReduction = 0f)
        {
            this.attributes = attributes;
            this.armorProficiencyTimeReduction = Mathf.Clamp(armorProficiencyTimeReduction, 0f, 1f);
        }

        public bool RegisterDamage(float damageTaken)
        {
            if (damageTaken <= 0f || damageTaken < CombatTrigger)
                return false;

            IsInCombat = true;
            TimeRemaining = CombatDuration;
            return true;
        }

        public float Advance(float deltaTime)
        {
            if (deltaTime <= 0f || !IsInCombat)
                return 1f;

            float inCombatTime = Mathf.Min(deltaTime, TimeRemaining);
            TimeRemaining = Mathf.Max(0f, TimeRemaining - deltaTime);
            if (TimeRemaining <= 0f)
                IsInCombat = false;

            return 1f - 0.5f * (inCombatTime / deltaTime);
        }

        public static float CalculateCombatTrigger(float defense)
        {
            defense = Mathf.Max(0f, defense);
            float trigger;

            if (defense <= 15f)
                trigger = defense;
            else if (defense <= 35f)
                trigger = 15f + (defense - 15f) * 0.75f;
            else if (defense <= 65f)
                trigger = 30f + (defense - 35f) * 0.5f;
            else if (defense <= 125f)
                trigger = 45f + (defense - 65f) * 0.25f;
            else
                trigger = 60f;

            return Mathf.Max(1f, Mathf.Floor(trigger));
        }
    }
}