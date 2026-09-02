using System;
using TTH.Combat.Attributes;

namespace TTH.Combat.Runtime
{
    /// <summary>
    /// Orchestrates a single hit: PreHit -> BuildContext -> Proc_OnHit -> Calc/Mitigate/Clamp -> Apply -> Proc_AfterHit -> Emit
    /// Phase A: Proc hooks can be null.
    /// Phase B: plug EffectSystemLite / Ability proc into proc hooks.
    /// </summary>
    public sealed class CombatSystem
    {
        private readonly CombatEvents _events;
        private readonly ICombatProc _proc; // optional

        public CombatSystem(CombatEvents events, ICombatProc proc = null)
        {
            _events = events ?? new CombatEvents();
            _proc = proc;
        }

        public CombatEvents Events => _events;

        public DamageContext HandleHit(in HitEvent hit, DamageKind kind = DamageKind.Direct)
        {
            var ctx = new DamageContext
            {
                kind = kind,
                isValidHit = true,
                rejectReason = HitRejectReason.None,
                hitPoint = hit.HitPoint,
                source = hit.Source,
                hitSeq = hit.HitSeq
            };

            // --- PreHit: null checks ---
            if (hit.Attacker == null || hit.Defender == null)
            {
                ctx.isValidHit = false;
                ctx.rejectReason = HitRejectReason.NullEntity;
                EmitEnd(ctx);
                return ctx;
            }

            ctx.attackerId = hit.Attacker.Id;
            ctx.defenderId = hit.Defender.Id;

            // --- PreHit: dead target? ---
            if (hit.Defender.Resources == null || hit.Defender.Resources.IsDead)
            {
                ctx.isValidHit = false;
                ctx.rejectReason = HitRejectReason.DeadTarget;
                EmitEnd(ctx);
                return ctx;
            }

            // --- PreHit: invulnerable? (placeholder) ---
            ctx.isInvulnerable = false;
            if (ctx.isInvulnerable)
            {
                ctx.isValidHit = false;
                ctx.rejectReason = HitRejectReason.Invulnerable;
                EmitEnd(ctx);
                return ctx;
            }

            // --- Snapshot stats/resources ---
            ctx.atk = SafeGet(hit.Attacker.Attributes, AttributeId.ATK);
            ctx.def = SafeGet(hit.Defender.Attributes, AttributeId.DEF);
            ctx.defenderMaxHP = SafeGet(hit.Defender.Attributes, AttributeId.HP);
            ctx.defenderCurrentHP = hit.Defender.Resources.CurrentHP;

            // --- Proc_OnHit (Phase B plugs in) ---
            // IMPORTANT: Proc may set ctx.baseDamage (Option A ability payload)
            try
            {
                _proc?.OnHit(ref ctx, hit.Attacker, hit.Defender);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[CombatSystem] Error in Proc.OnHit: {ex.Message}\n{ex.StackTrace}");
            }

            // --- Calc base damage fallback ---
            // If no payload/proc provided baseDamage, fallback to ATK (MVP "weapon/basic attack")
            if (ctx.baseDamage <= 0f)
                ctx.baseDamage = Math.Max(0f, ctx.atk);

            // --- Mitigation (MVP): minus DEF ---
            ctx.mitigatedDamage = ctx.baseDamage - Math.Max(0f, ctx.def);

            // --- Clamp: MIN DAMAGE = 0 (your rule) ---
            ctx.finalDamage = ctx.mitigatedDamage;
            if (ctx.finalDamage < 0f) ctx.finalDamage = 0f;

            // --- Apply to ResourcePool ---
            if (ctx.finalDamage > 0f)
            {
                ctx.hpDeltaApplied = -ctx.finalDamage;
                hit.Defender.Resources.ApplyHPDelta(ctx.hpDeltaApplied);
            }
            else
            {
                ctx.hpDeltaApplied = 0f;
            }

            // Clamp current to max (in case MaxHP changed recently)
            if (hit.Defender.Attributes != null)
                hit.Defender.Resources.ClampToMax(hit.Defender.Attributes);

            ctx.defenderHPAfter = hit.Defender.Resources.CurrentHP;
            ctx.didDamage = ctx.finalDamage > 0f;
            ctx.didKill = hit.Defender.Resources.IsDead;

            // --- Proc_AfterHit (Phase B plugs in) ---
            try
            {
                _proc?.AfterHit(ref ctx, hit.Attacker, hit.Defender);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"[CombatSystem] Error in Proc.AfterHit: {ex.Message}\n{ex.StackTrace}");
            }

            // --- Emit events ---
            EmitEnd(ctx);
            return ctx;
        }

        private void EmitEnd(in DamageContext ctx)
        {
            if (!ctx.isValidHit)
            {
                _events.EmitHitRejected(ctx);
                return;
            }

            _events.EmitHit(ctx);
            if (ctx.didDamage) _events.EmitDamage(ctx);
            if (ctx.didKill) _events.EmitKilled(ctx);
        }

        private static float SafeGet(AttributeSystem sys, AttributeId id)
        {
            if (sys == null)
            {
                UnityEngine.Debug.LogWarning($"[CombatSystem] SafeGet called with null AttributeSystem for {id}. Returning 0. Check entity initialization.");
                return 0f;
            }
            return sys.Get(id);
        }
    }
}
