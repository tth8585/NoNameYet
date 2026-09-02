using UnityEngine;

namespace TTH.Combat.Runtime
{
    /// <summary>
    /// Produced by hit detection (projectile vs collider).
    /// Keep it lightweight; CombatSystem will build DamageContext.
    /// </summary>
    public readonly struct HitEvent
    {
        public readonly CombatEntity Attacker;
        public readonly CombatEntity Defender;
        public readonly Vector2 HitPoint;
        public readonly object Source;     // projectile instance / ability cast instance / etc.
        public readonly int HitSeq;        // optional sequence for debugging

        public HitEvent(CombatEntity attacker, CombatEntity defender, Vector2 hitPoint, object source, int hitSeq = 0)
        {
            Attacker = attacker;
            Defender = defender;
            HitPoint = hitPoint;
            Source = source;
            HitSeq = hitSeq;
        }
    }
}
