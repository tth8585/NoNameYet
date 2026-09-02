using System;
using System.Linq;
using TTH.Combat.Attributes;
using TTH.Combat.Runtime;

namespace TTH.Combat.Effects
{
    /// <summary>
    /// Phase B (lite):
    /// - Maintains EffectInstances + timers + stacking policies
    /// - Bridges payload into AttributeSystem modifiers and StatusSystem flags
    /// </summary>
    public sealed class EffectSystemLite
    {
        public void Tick(CombatEntity entity, EffectStackContainer container, float dt)
        {
            if (entity == null || container == null || dt <= 0f) return;

            var list = container.ActiveMutable;
            if (list.Count == 0) return;

            // expire pass
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var inst = list[i];
                if (inst.Def.durationSeconds <= 0f) continue;

                inst.RemainingSeconds -= dt;
                if (inst.RemainingSeconds <= 0f)
                {
                    RemoveInstance(entity, inst);
                    list.RemoveAt(i);
                }
            }
        }

        public void ApplyEffect(CombatEntity target, EffectStackContainer container, EffectDefinitionSO def, CombatEntity applier = null)
        {
            if (target == null || container == null || def == null) return;

            var list = container.ActiveMutable;
            int applierId = applier != null ? applier.Id : 0;

            // Find existing according to policy
            EffectInstance existing = null;

            switch (def.policy)
            {
                case EffectStackPolicy.UniqueByKey:
                    if (!string.IsNullOrEmpty(def.uniqueKey))
                        existing = list.FirstOrDefault(x => x.Def == def && x.Def.uniqueKey == def.uniqueKey);
                    else
                        existing = list.FirstOrDefault(x => x.Def == def);
                    break;

                case EffectStackPolicy.RefreshDuration:
                case EffectStackPolicy.ReplaceIfStronger:
                case EffectStackPolicy.StackCount:
                default:
                    existing = list.FirstOrDefault(x => x.Def == def);
                    break;
            }

            if (existing == null)
            {
                // Add new instance
                var inst = new EffectInstance(def, applierId);
                list.Add(inst);
                SpawnPayload(target, inst);
                return;
            }

            // Apply stacking policy router
            switch (def.policy)
            {
                case EffectStackPolicy.RefreshDuration:
                    existing.RefreshDuration();
                    // optional: keep power; no re-spawn needed
                    // If you want refresh also re-apply statuses: do it here
                    ApplyStatuses(target, def);
                    break;

                case EffectStackPolicy.StackCount:
                    {
                        int max = Math.Max(1, def.maxStacks);
                        if (existing.Stacks < max)
                        {
                            existing.Stacks++;
                            existing.RefreshDuration();
                            // simplest: re-spawn modifiers to reflect stacks by removing & spawning again
                            // (safe and deterministic for MVP-lite)
                            RespawnPayload(target, existing);
                        }
                        else
                        {
                            // at cap: just refresh duration
                            existing.RefreshDuration();
                            ApplyStatuses(target, def);
                        }
                    }
                    break;

                case EffectStackPolicy.ReplaceIfStronger:
                    {
                        // Compare incoming strength vs existing snapshot strength
                        if (def.strength > existing.StrengthSnapshot)
                        {
                            // Replace: remove old payload then apply new snapshot
                            RemovePayload(target, existing);
                            existing.StrengthSnapshot = def.strength;
                            existing.Stacks = 1;
                            existing.RefreshDuration();
                            SpawnPayload(target, existing);
                        }
                        else
                        {
                            // weaker: just refresh duration (or ignore)
                            existing.RefreshDuration();
                        }
                    }
                    break;

                case EffectStackPolicy.UniqueByKey:
                    // replace
                    RemovePayload(target, existing);
                    existing.Stacks = 1;
                    existing.StrengthSnapshot = def.strength;
                    existing.RefreshDuration();
                    SpawnPayload(target, existing);
                    break;
            }
        }

        private void RespawnPayload(CombatEntity target, EffectInstance inst)
        {
            RemovePayload(target, inst);
            SpawnPayload(target, inst);
        }

        private void SpawnPayload(CombatEntity target, EffectInstance inst)
        {
            // Spawn modifiers
            var def = inst.Def;
            if (def.modifiers != null && def.modifiers.Length > 0 && target.Attributes != null)
            {
                for (int i = 0; i < def.modifiers.Length; i++)
                {
                    var tpl = def.modifiers[i];
                    if (tpl == null) continue;

                    var m = tpl.CloneRuntime();
                    m.Source = inst; // KEY: so we can RemoveBySource(inst)
                    // If you want stacks to scale modifier value:
                    // Multiply Add value by stacks; Multiply by stacks could be more complex.
                    if (inst.Stacks > 1 && m.Op == ModifierOp.Add)
                        m.Value *= inst.Stacks;

                    // Duration follows effect duration (so modifiers auto-expire if you rely on AttributeSystem.Tick)
                    // BUT we are already expiring effect instances; to avoid double-expire conflicts,
                    // set modifier duration to <=0 and let EffectSystem handle expiry removal.
                    m.DurationSeconds = 0f;
                    m.RemainingSeconds = 0f;

                    target.Attributes.AddModifier(m);
                }
            }

            // Apply statuses
            ApplyStatuses(target, def);
        }

        private void ApplyStatuses(CombatEntity target, EffectDefinitionSO def)
        {
            if (def.statuses != null && def.statuses.Length > 0 && target.Statuses != null)
            {
                for (int i = 0; i < def.statuses.Length; i++)
                {
                    var s = def.statuses[i];
                    target.Statuses.AddOrRefresh(s.id, s.durationSeconds);
                }
            }
        }

        private void RemoveInstance(CombatEntity target, EffectInstance inst)
        {
            RemovePayload(target, inst);
            // Note: statuses are duration-based in StatusSystem; we don't forcibly remove them here.
            // If you want "status exists only while effect exists", add a reference-count scheme later.
        }

        private void RemovePayload(CombatEntity target, EffectInstance inst)
        {
            if (target.Attributes != null)
                target.Attributes.RemoveBySource(inst);
        }
    }
}
