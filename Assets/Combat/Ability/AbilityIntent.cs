using System;
using UnityEngine;
using TTH.Combat.Runtime;

namespace TTH.Combat.Ability
{
    /// <summary>
    /// "Source payload" gắn vào HitEvent.Source để CombatProc đọc ra.
    /// Snapshot tại thời điểm CAST (params không đổi nếu stat thay đổi sau đó).
    /// </summary>
    [Serializable]
    public sealed class AbilityIntent
    {
        public AbilityDefinition definition;

        [Tooltip("Ai cast/ai sở hữu intent này.")]
        public CombatEntity caster;

        [Tooltip("Dùng để debug hoặc phân biệt các projectile khác nhau.")]
        public int seq;

        [Tooltip("World point / aim point nếu cần.")]
        public Vector3 aimPoint;

        // --- Snapshot params (Option A) ---
        [Header("Snapshot Params (evaluated at cast time)")]
        public float baseDamage;        // will be copied into ctx.baseDamage (pre-mitigation)
        public float onHitRadius;       // optional, for future AoE
        public float durationSeconds;   // optional, for future DoT/beam
        public int maxAllies;           // exclude self (only meaningful for OnCast)
        public float cooldownSeconds;   // snapshot cooldown used for this cast
        public float reservedManaCost;
        public SupportAbilityDefinitionSO[] supportLinks;

        public AbilityIntent(
            AbilityDefinition def,
            CombatEntity caster,
            int seq,
            Vector3 aimPoint,
            float baseDamage,
            float onHitRadius,
            float durationSeconds,
            int maxAllies,
            float cooldownSeconds,
            float reservedManaCost = 0f,
            SupportAbilityDefinitionSO[] supportLinks = null)
        {
            this.definition = def;
            this.caster = caster;
            this.seq = seq;
            this.aimPoint = aimPoint;

            this.baseDamage = baseDamage;
            this.onHitRadius = onHitRadius;
            this.durationSeconds = durationSeconds;
            this.maxAllies = maxAllies;
            this.cooldownSeconds = cooldownSeconds;
            this.reservedManaCost = reservedManaCost;
            this.supportLinks = supportLinks ?? Array.Empty<SupportAbilityDefinitionSO>();
        }

        public override string ToString()
        {
            var id = definition != null ? definition.abilityId : "null";
            return $"AbilityIntent({id}, seq={seq}, dmg={baseDamage:0.##})";
        }
    }
}
