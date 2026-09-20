using TTH.Combat.Runtime;

namespace TTH.Combat.Effects
{
    /// <summary>
    /// Simple proc adapter:
    /// - OnHit / AfterHit can apply effects by reading whatever rule you use (projectile tags, ability, etc.)
    /// This is intentionally minimal: you decide which EffectDefinitionSO to apply.
    /// </summary>
    public sealed class EffectCombatProc : ICombatProc
    {
        private readonly EffectSystemLite _effects;

        // You can keep per-entity containers elsewhere; simplest is to store on entity wrapper.
        private readonly System.Func<CombatEntity, EffectStackContainer> _getContainer;

        // Minimal rule: apply this effect on hit (for testing)
        private readonly EffectDefinitionSO _onHitEffect;

        public EffectCombatProc(EffectSystemLite effects,
                                System.Func<CombatEntity, EffectStackContainer> getContainer,
                                EffectDefinitionSO onHitEffect)
        {
            _effects = effects;
            _getContainer = getContainer;
            _onHitEffect = onHitEffect;
        }

        public void OnHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
        {
            if (_onHitEffect == null) return;
            var c = _getContainer?.Invoke(defender);
            if (c == null) return;

            _effects.ApplyEffect(defender, c, _onHitEffect, attacker);
        }

        public void AfterHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
        {
            // optional: after-hit effects
        }

        public void OnTurnStart(CombatEntity actor)
        {
            // Effects with turn timing are handled by their status system.
        }

        public void OnTurnEnd(CombatEntity actor)
        {
            // Effects with turn timing are handled by their status system.
        }
    }
}
