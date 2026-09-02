using System;

namespace TTH.Combat.Runtime
{
    /// <summary>
    /// Thin event hub for combat/UI/VFX hooks.
    /// Keep it dumb: payload is DamageContext (snapshot).
    /// </summary>
    public sealed class CombatEvents
    {
        public event Action<DamageContext> OnHit;         // fired when a valid hit processed (even if dmg=0)
        public event Action<DamageContext> OnDamage;      // fired when finalDamage > 0
        public event Action<DamageContext> OnKilled;      // fired when defender dies after apply
        public event Action<DamageContext> OnHitRejected; // fired when hit is rejected (dead target, null entity, invulnerable, etc.)

        internal void EmitHit(in DamageContext ctx) => OnHit?.Invoke(ctx);
        internal void EmitDamage(in DamageContext ctx) => OnDamage?.Invoke(ctx);
        internal void EmitKilled(in DamageContext ctx) => OnKilled?.Invoke(ctx);
        internal void EmitHitRejected(in DamageContext ctx) => OnHitRejected?.Invoke(ctx);
    }
}
