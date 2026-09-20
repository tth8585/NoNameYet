using System;
using UnityEngine;
using TTH.Combat.Effects;
using TTH.Combat.Runtime;

namespace TTH.Combat.Ability
{
    /// <summary>
    /// Combat Proc (Phase B):
    /// - Đọc ctx.source nếu là AbilityIntent
    /// - Option A: ctx.baseDamage = intent.baseDamage (pre-mitigation)
    /// - Apply OnHit effects (buff/debuff) qua EffectSystemLite
    /// </summary>
    public sealed class AbilityEffectProc : ICombatProc
    {
        private readonly EffectSystemLite _effects;
        private readonly Func<CombatEntity, EffectStackContainer> _getContainer;

        public AbilityEffectProc(EffectSystemLite effects, Func<CombatEntity, EffectStackContainer> getContainer)
        {
            _effects = effects ?? new EffectSystemLite();
            _getContainer = getContainer;
        }

        public void OnHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
        {
            if (ctx.source is not AbilityIntent intent) return;
            if (intent.definition == null) return;

            // Option A: snapshot base damage from intent (CombatSystem will keep it now)
            ctx.baseDamage = intent.baseDamage;
            ctx.hasBaseDamage = true;

            var list = intent.definition.onHit;
            if (list == null || list.Length == 0) return;

            for (int i = 0; i < list.Length; i++)
            {
                var e = list[i];
                if (e == null || e.effect == null) continue;

                if (e.chance < 1f && UnityEngine.Random.value > e.chance)
                    continue;

                var target = (e.target == AbilityTarget.Attacker) ? attacker : defender;
                if (target == null) continue;

                var container = _getContainer?.Invoke(target);
                if (container == null) continue;

                _effects.ApplyEffect(target, container, e.effect, attacker);
            }
        }

        public void AfterHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
        {
            // MVP: để trống.
            // Lifesteal / on-kill / on-damage-confirmed sẽ ở đây sau.
        }

        public void OnTurnStart(CombatEntity actor)
        {
            // Abilities do not own turn-start passive behavior.
        }

        public void OnTurnEnd(CombatEntity actor)
        {
            // Abilities do not own turn-end passive behavior.
        }
    }
}
