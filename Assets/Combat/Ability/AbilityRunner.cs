using System;
using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Runtime;
using TTH.Combat.Effects;

namespace TTH.Combat.Ability
{
    public sealed class AbilityRunner
    {
        private readonly Dictionary<string, float> _cdRemainingById = new();
        private int _seq;

        public CombatEntity Owner { get; }

        // Optional (only needed if you use ApplyEffect on-cast)
        private readonly EffectSystemLite _effects;
        private readonly Func<CombatEntity, EffectStackContainer> _getContainer;

        // Game-layer query: return allies in range (ordering decides who is picked)
        private readonly Func<CombatEntity, float, IEnumerable<CombatEntity>> _getAlliesInRange;

        public AbilityRunner(
            CombatEntity owner,
            EffectSystemLite effects = null,
            Func<CombatEntity, EffectStackContainer> getContainer = null,
            Func<CombatEntity, float, IEnumerable<CombatEntity>> getAlliesInRange = null)
        {
            Owner = owner;
            _effects = effects;
            _getContainer = getContainer;
            _getAlliesInRange = getAlliesInRange;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f || _cdRemainingById.Count == 0) return;

            _keysCache.Clear();
            foreach (var kv in _cdRemainingById) _keysCache.Add(kv.Key);

            for (int i = 0; i < _keysCache.Count; i++)
            {
                var key = _keysCache[i];
                var t = _cdRemainingById[key] - dt;
                if (t <= 0f) _cdRemainingById.Remove(key);
                else _cdRemainingById[key] = t;
            }
        }

        public bool CanUse(AbilityDefinition def)
        {
            if (def == null) return false;
            return !_cdRemainingById.ContainsKey(def.abilityId);
        }

        /// <summary>
        /// CAST ability:
        /// - Snapshot core params from current stats
        /// - Apply OnCast actions (HealHP / ApplyEffect)
        /// - Consume cooldown (snapshot)
        /// - Return intent to attach to projectile/hit source (for Archer projectile etc.)
        /// </summary>
        public AbilityIntent UseAndCreateIntent(AbilityDefinition def, Vector3 aimPoint)
        {
            if (def == null) return null;

            float Stat(AbilityScaleStat s) => AbilityDefinition.GetStatFromAttributes(Owner?.Attributes, s);

            float cooldownSeconds = Mathf.Max(0f, def.cooldown.Evaluate(Stat));
            float baseDamage = Mathf.Max(0f, def.damage.Evaluate(Stat));
            float coreRadius = Mathf.Max(0f, def.radius.Evaluate(Stat));
            float coreDuration = Mathf.Max(0f, def.duration.Evaluate(Stat));

            // Apply OnCast (instant)
            ApplyOnCast(def, Stat);

            // Consume cooldown
            if (cooldownSeconds > 0f)
                _cdRemainingById[def.abilityId] = cooldownSeconds;

            // Build intent (for projectile/hit source)
            _seq++;
            return new AbilityIntent(
                def,
                Owner,
                _seq,
                aimPoint,
                baseDamage: baseDamage,
                onHitRadius: coreRadius,
                durationSeconds: coreDuration,
                maxAllies: 0,
                cooldownSeconds: cooldownSeconds
            );
        }

        private void ApplyOnCast(AbilityDefinition def, Func<AbilityScaleStat, float> statGetter)
        {
            var list = def.onCast;
            if (list == null || list.Length == 0) return;

            for (int i = 0; i < list.Length; i++)
            {
                var rule = list[i];
                if (rule == null) continue;

                if (rule.chance < 1f && UnityEngine.Random.value > rule.chance)
                    continue;

                float radius = Mathf.Max(0f, rule.radius.Evaluate(statGetter));
                int maxAllies = Mathf.Max(0, rule.maxAllies.Evaluate(statGetter)); // exclude self

                // Apply action to targets
                ApplyOnCastToTargets(rule, radius, maxAllies, statGetter);
            }
        }

        private void ApplyOnCastToTargets(
            AbilityOnCastEffect rule,
            float radius,
            int maxAllies,
            Func<AbilityScaleStat, float> statGetter)
        {
            // Self
            if (rule.target == AbilityCastTarget.Self || rule.target == AbilityCastTarget.SelfAndAlliesInRange)
            {
                ApplyOnCastAction(Owner, rule, statGetter);
            }

            // Allies
            if (rule.target == AbilityCastTarget.AlliesInRange || rule.target == AbilityCastTarget.SelfAndAlliesInRange)
            {
                if (_getAlliesInRange == null) return;

                int picked = 0;
                foreach (var ally in _getAlliesInRange(Owner, radius))
                {
                    if (ally == null) continue;
                    if (ally == Owner) continue; // exclude self

                    ApplyOnCastAction(ally, rule, statGetter);

                    picked++;
                    if (maxAllies > 0 && picked >= maxAllies)
                        break;
                }
            }
        }

        private void ApplyOnCastAction(CombatEntity target, AbilityOnCastEffect rule, Func<AbilityScaleStat, float> statGetter)
        {
            if (target == null) return;

            switch (rule.action)
            {
                case AbilityOnCastActionKind.ResourceDelta:
                    {
                        float delta = rule.amount.Evaluate(statGetter); // có thể âm
                        if (Mathf.Approximately(delta, 0f)) return;
                        if (target.Resources == null) return;

                        switch (rule.resource)
                        {
                            case AbilityResourceType.HP:
                                target.Resources.ApplyHPDelta(delta);
                                break;

                            case AbilityResourceType.MP:
                                target.Resources.ApplyMPDelta(delta);
                                break;

                            default:
                                return;
                        }

                        if (target.Attributes != null)
                            target.Resources.ClampToMax(target.Attributes);

                        //Debug.Log(
                        //$"[Ability][OnCast][ResourceDelta] " +
                        //$"Target={target.Id} Resource={rule.resource} Amount={delta}"
                        //);

                        return;
                    }

                case AbilityOnCastActionKind.ApplyEffect:
                default:
                    {
                        // Needs effect system wired
                        if (_effects == null || _getContainer == null) return;
                        if (rule.effect == null) return;

                        var c = _getContainer.Invoke(target);
                        if (c == null) return;

                        _effects.ApplyEffect(target, c, rule.effect, Owner);

                        //Debug.Log(
                        //    $"[Ability][OnCast][ApplyEffect] " +
                        //    $"Target={target.Id} Effect={rule.effect.name}"
                        //);

                        return;
                    }
            }
        }

        private static readonly List<string> _keysCache = new();
    }
}
