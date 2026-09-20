using System;
using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Runtime;
using TTH.Combat.Effects;

namespace TTH.Combat.Ability
{
    public sealed class AbilityRunner
    {
        private readonly Dictionary<string, float> _cdRemainingById = new();
        private readonly Dictionary<string, ActiveSkillState> _activeSkillStates = new();
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
            return CanUse(new AbilityLoadout(def, def.supportLinks));
        }

        public bool CanUse(AbilityLoadout loadout)
        {
            var def = loadout?.activeAbility;
            if (def == null) return false;
            if (_cdRemainingById.ContainsKey(def.abilityId)) return false;

            if (Owner == null || Owner.Resources == null || Owner.Attributes == null) return false;

            if (def.activationMode == AbilityActivationMode.ReservedToggle)
            {
                float reserveCost = GetReserveManaCost(def, loadout.supportLinks);
                return Owner.Resources.CurrentMP >= reserveCost;
            }

            float manaCost = GetManaCost(def, loadout.supportLinks);
            return Owner.Resources.CurrentMP >= manaCost;
        }

        public AbilityIntent UseAndCreateIntent(AbilityDefinition def, Vector3 aimPoint)
        {
            if (def == null) return null;
            return UseAndCreateIntent(new AbilityLoadout(def, def.supportLinks), aimPoint);
        }

        public AbilityIntent UseAndCreateIntent(AbilityLoadout loadout, Vector3 aimPoint)
        {
            var def = loadout?.activeAbility;
            if (def == null) return null;

            var supports = loadout.supportLinks ?? Array.Empty<SupportAbilityDefinitionSO>();
            var matchingSupports = loadout.GetMatchingSupports();

            float Stat(AbilityScaleStat s) => AbilityDefinition.GetStatFromAttributes(Owner?.Attributes, s);

            float cooldownSeconds = Mathf.Max(0f, def.cooldown.Evaluate(Stat));
            float baseDamage = Mathf.Max(0f, def.damage.Evaluate(Stat));
            float coreRadius = Mathf.Max(0f, def.radius.Evaluate(Stat));
            float coreDuration = Mathf.Max(0f, def.duration.Evaluate(Stat));
            float manaCost = GetManaCost(def, supports);
            float reserveCost = GetReserveManaCost(def, supports);

            if (Owner == null || Owner.Resources == null || Owner.Attributes == null)
                return null;

            if (def.activationMode == AbilityActivationMode.ReservedToggle)
            {
                var state = GetOrCreateState(def, matchingSupports);
                if (state.isActive)
                {
                    CancelReservedActive(def, state);
                    return null;
                }

                if (Owner.Resources.CurrentMP < reserveCost)
                    return null;

                Owner.Resources.ApplyMPDelta(-reserveCost);
                state.isActive = true;
                state.reservedMana = reserveCost;
                state.manaReservoir = reserveCost;
                state.duration = coreDuration;
                state.activeDurationRemaining = coreDuration;
                state.activationMode = def.activationMode;
                state.definition = def;

                ApplyActiveSupportBuff(state, def);

                var finalDuration = state.ComputeEffectiveDuration(coreDuration);
                state.duration = finalDuration;
                state.activeDurationRemaining = finalDuration;

                // Support-based armor / buffer comes from state computation; actual attribute buff can be applied by effect system later.
                ApplyOnCast(def, matchingSupports, Stat);
                var reservedCooldown = ApplySupportCooldown(def, matchingSupports, cooldownSeconds);
                if (reservedCooldown > 0f) _cdRemainingById[def.abilityId] = reservedCooldown;

                _seq++;
                return new AbilityIntent(
                    def,
                    Owner,
                    _seq,
                    aimPoint,
                    baseDamage: baseDamage,
                    onHitRadius: coreRadius,
                    durationSeconds: finalDuration,
                    maxAllies: 0,
                    cooldownSeconds: reservedCooldown,
                    reservedManaCost: reserveCost,
                    supportLinks: matchingSupports
                );
            }

            if (Owner.Resources.CurrentMP < manaCost)
                return null;

            Owner.Resources.ApplyMPDelta(-manaCost);

            ApplyOnCast(def, matchingSupports, Stat);

            var finalCooldown = ApplySupportCooldown(def, matchingSupports, cooldownSeconds);
            if (finalCooldown > 0f)
                _cdRemainingById[def.abilityId] = finalCooldown;

            _seq++;
            return new AbilityIntent(
                def,
                Owner,
                _seq,
                aimPoint,
                baseDamage: baseDamage,
                onHitRadius: coreRadius,
                durationSeconds: ApplySupportDuration(def, matchingSupports, coreDuration),
                maxAllies: 0,
                cooldownSeconds: finalCooldown,
                reservedManaCost: 0f,
                supportLinks: matchingSupports
            );
        }

        public void TickReservedAbility(AbilityDefinition def, float dt)
        {
            if (def == null || !_activeSkillStates.TryGetValue(def.abilityId, out var state) || !state.isActive) return;
            if (dt <= 0f) return;

            state.activeDurationRemaining = Mathf.Max(0f, state.activeDurationRemaining - dt);
            if (state.activeDurationRemaining <= 0f)
            {
                CancelReservedActive(def, state);
            }
        }

        public void CancelReservedActive(AbilityDefinition def, ActiveSkillState state = null)
        {
            if (def == null) return;
            if (state == null && !_activeSkillStates.TryGetValue(def.abilityId, out state)) return;

            if (state != null && state.isActive)
            {
                RemoveActiveSupportBuff(state);
                state.isActive = false;
                state.activeDurationRemaining = 0f;
                state.reservedMana = 0f;
                state.manaReservoir = 0f;
            }

            _activeSkillStates.Remove(def.abilityId);
        }

        public ActiveSkillState GetStateFor(AbilityDefinition def)
        {
            if (def == null) return null;
            _activeSkillStates.TryGetValue(def.abilityId, out var state);
            return state;
        }

        private ActiveSkillState GetOrCreateState(AbilityDefinition def, SupportAbilityDefinitionSO[] supports)
        {
            if (def == null) return null;
            if (!_activeSkillStates.TryGetValue(def.abilityId, out var state))
            {
                state = new ActiveSkillState
                {
                    definition = def,
                    activationMode = def.activationMode,
                    linkedSupports = supports != null ? new List<SupportAbilityDefinitionSO>(supports) : new List<SupportAbilityDefinitionSO>()
                };
                _activeSkillStates[def.abilityId] = state;
            }
            return state;
        }

        private float GetManaCost(AbilityDefinition def, SupportAbilityDefinitionSO[] supports)
        {
            if (def == null) return 0f;
            float Stat(AbilityScaleStat s) => AbilityDefinition.GetStatFromAttributes(Owner?.Attributes, s);
            var baseCost = Mathf.Max(0f, def.manaCost.Evaluate(Stat));
            return ApplySupportManaCost(def, supports, baseCost);
        }

        private float GetReserveManaCost(AbilityDefinition def, SupportAbilityDefinitionSO[] supports)
        {
            if (def == null) return 0f;
            float Stat(AbilityScaleStat s) => AbilityDefinition.GetStatFromAttributes(Owner?.Attributes, s);
            var baseCost = Mathf.Max(0f, def.reserveManaCost.Evaluate(Stat));
            return ApplySupportManaCost(def, supports, baseCost);
        }

        private static float ApplySupportManaCost(AbilityDefinition def, SupportAbilityDefinitionSO[] supports, float baseCost)
        {
            if (supports == null || supports.Length == 0)
                return baseCost;

            float multiplier = 1f;
            for (int i = 0; i < supports.Length; i++)
            {
                var support = supports[i];
                if (support == null) continue;
                multiplier += support.GetModifierValue(AbilitySupportModifierType.ManaCostIncrease);
                multiplier -= support.GetModifierValue(AbilitySupportModifierType.ManaCostReduction);
            }

            return Mathf.Max(0f, baseCost * multiplier);
        }

        private static float ApplySupportDuration(AbilityDefinition def, SupportAbilityDefinitionSO[] supports, float baseDuration)
        {
            if (supports == null) return baseDuration;

            float multiplier = 1f;
            for (int i = 0; i < supports.Length; i++)
            {
                var support = supports[i];
                if (support != null)
                    multiplier += support.GetModifierValue(AbilitySupportModifierType.DurationMultiplier);
            }

            return Mathf.Max(0f, baseDuration * Mathf.Max(0.01f, multiplier));
        }

        private static float ApplySupportCooldown(AbilityDefinition def, SupportAbilityDefinitionSO[] supports, float baseCooldown)
        {
            if (supports == null) return baseCooldown;

            float multiplier = 1f;
            for (int i = 0; i < supports.Length; i++)
            {
                var support = supports[i];
                if (support != null)
                    multiplier -= support.GetModifierValue(AbilitySupportModifierType.CooldownReduction);
            }

            return Mathf.Max(0f, baseCooldown * Mathf.Max(0.01f, multiplier));
        }

        private float ComputeEffectiveDamage(AbilityDefinition def, float baseDamage)
        {
            if (def == null) return baseDamage;
            var state = GetStateFor(def);
            if (state != null)
            {
                return state.ComputeEffectiveDamage(baseDamage);
            }

            float totalMultiplier = 1f;
            float totalBonus = 0f;
            if (def.supportLinks != null)
            {
                for (int i = 0; i < def.supportLinks.Length; i++)
                {
                    var support = def.supportLinks[i];
                    if (support == null || !support.Matches(def)) continue;
                    totalMultiplier += support.GetModifierValue(AbilitySupportModifierType.DamageMultiplier);
                    totalBonus += support.GetModifierValue(AbilitySupportModifierType.FlatDamageBonus);
                }
            }
            return Math.Max(0f, baseDamage * totalMultiplier + totalBonus);
        }

        private static List<SupportAbilityDefinitionSO> GetMatchingSupports(AbilityDefinition def)
        {
            var matches = new List<SupportAbilityDefinitionSO>();
            if (def?.supportLinks == null) return matches;

            for (int i = 0; i < def.supportLinks.Length; i++)
            {
                var support = def.supportLinks[i];
                if (support != null && support.Matches(def))
                    matches.Add(support);
            }

            return matches;
        }

        private void ApplyActiveSupportBuff(ActiveSkillState state, AbilityDefinition def)
        {
            if (Owner == null || Owner.Attributes == null || state == null || def == null) return;
            if (state.FinalArmorBonus <= 0f) return;

            var modifier = new AttributeModifier
            {
                Attribute = AttributeId.DEF,
                Op = ModifierOp.Add,
                Value = state.FinalArmorBonus,
                DurationSeconds = state.duration,
                RemainingSeconds = state.duration,
                Source = state,
                Stacking = StackingPolicy.UniqueByStackKey,
                StackKey = $"ActiveSkill_{def.abilityId}_DEF"
            };

            Owner.Attributes.AddModifier(modifier);
        }

        private void RemoveActiveSupportBuff(ActiveSkillState state)
        {
            if (Owner == null || Owner.Attributes == null || state == null) return;
            Owner.Attributes.RemoveBySource(state);
        }

        private void ApplyOnCast(AbilityDefinition def, SupportAbilityDefinitionSO[] supports, Func<AbilityScaleStat, float> statGetter)
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
                ApplyOnCastToTargets(def, supports, rule, radius, maxAllies, statGetter);
            }
        }

        private void ApplyOnCastToTargets(
            AbilityDefinition def,
            SupportAbilityDefinitionSO[] supports,
            AbilityOnCastEffect rule,
            float radius,
            int maxAllies,
            Func<AbilityScaleStat, float> statGetter)
        {
            // Self
            if (rule.target == AbilityCastTarget.Self || rule.target == AbilityCastTarget.SelfAndAlliesInRange)
            {
                ApplyOnCastAction(def, supports, Owner, rule, statGetter);
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

                    ApplyOnCastAction(def, supports, ally, rule, statGetter);

                    picked++;
                    if (maxAllies > 0 && picked >= maxAllies)
                        break;
                }
            }
        }

        private void ApplyOnCastAction(AbilityDefinition def, SupportAbilityDefinitionSO[] supports, CombatEntity target, AbilityOnCastEffect rule, Func<AbilityScaleStat, float> statGetter)
        {
            if (target == null) return;

            switch (rule.action)
            {
                case AbilityOnCastActionKind.ResourceDelta:
                    {
                        float delta = rule.amount.Evaluate(statGetter); // có thể âm
                        if (rule.resource == AbilityResourceType.HP && delta > 0f)
                            delta *= GetHealMultiplier(supports);
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

        private static float GetHealMultiplier(SupportAbilityDefinitionSO[] supports)
        {
            if (supports == null) return 1f;

            float multiplier = 1f;
            for (int i = 0; i < supports.Length; i++)
            {
                var support = supports[i];
                if (support != null)
                    multiplier += support.GetModifierValue(AbilitySupportModifierType.HealMultiplier);
            }

            return Mathf.Max(0f, multiplier);
        }

        private static readonly List<string> _keysCache = new();
    }
}
