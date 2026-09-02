using System;
using TTH.Combat.Attributes;

namespace TTH.Combat.Effects
{
    /// <summary>
    /// Runtime instance stored in EffectStackContainer.
    /// Acts as "Source" handle for spawned AttributeModifiers so we can remove them via AttributeSystem.RemoveBySource(this).
    /// </summary>
    public sealed class EffectInstance
    {
        public readonly EffectDefinitionSO Def;
        public readonly int AppliedByEntityId;

        public int Stacks { get; set; } = 1;
        public float RemainingSeconds { get; set; }
        public float StrengthSnapshot { get; set; }

        public bool IsExpired => Def.durationSeconds > 0f && RemainingSeconds <= 0f;

        public EffectInstance(EffectDefinitionSO def, int appliedByEntityId)
        {
            Def = def;
            AppliedByEntityId = appliedByEntityId;
            RemainingSeconds = def.durationSeconds;
            StrengthSnapshot = def.strength;
        }

        public void RefreshDuration()
        {
            if (Def.durationSeconds > 0f)
                RemainingSeconds = Def.durationSeconds;
        }
    }
}
