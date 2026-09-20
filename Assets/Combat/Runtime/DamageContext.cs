using UnityEngine;

namespace TTH.Combat.Runtime
{
    public enum HitRejectReason
    {
        None = 0,
        NullEntity,
        DeadTarget,
        Invulnerable,
        AlreadyHit,   // optional (if you add anti-double-hit)
        Invalid
    }

    public enum DamageKind
    {
        Direct = 0,
        Dot = 1,
        Environment = 2
    }

    /// <summary>
    /// Minimal, extensible snapshot for a single hit resolution.
    /// Mutable during pipeline; emitted to events at the end.
    /// </summary>
    public struct DamageContext
    {
        // Identity
        public int attackerId;
        public int defenderId;
        public object source;
        public int hitSeq;

        // Metadata
        public Vector2 hitPoint;
        public DamageKind kind;

        // PreHit
        public bool isValidHit;
        public bool isInvulnerable;
        public HitRejectReason rejectReason;

        // Snapshots
        public float atk;
        public float def;
        public float defenderMaxHP;
        public float defenderCurrentHP;

        // Computation
        public float baseDamage;
        public bool hasBaseDamage;
        public float mitigatedDamage;
        public float finalDamage;       // min = 0 in your rule
        public float hpDeltaApplied;    // usually -finalDamage

        // Outputs
        public bool didDamage;
        public bool didKill;
        public float defenderHPAfter;
    }
}
