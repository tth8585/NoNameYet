using System;
using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Effects;
using TTH.Combat.Ability;
using TTH.Combat.Runtime;
using TTH.Combat.Derived;

namespace TTH.Combat.Simulation
{
    /// <summary>
    /// Core tick loop for combat runtime:
    /// - Tick Attributes (timers / versions)
    /// - Tick Statuses (durations)
    /// - Tick Effects (EffectSystemLite containers)
    /// - Tick Ability cooldowns (optional)
    /// - Apply regen (Derived -> Resources) each dt
    /// - Clamp resources to max
    ///
    /// Smoke tests should call CombatRuntime.Tick(dt) instead of duplicating logic.
    /// Game runtime can own 1 instance and register entities.
    /// </summary>
    public sealed class CombatRuntime
    {
        private readonly List<CombatEntity> _entities = new();

        // Optional systems
        private readonly EffectSystemLite _effectSystem;
        private readonly Func<CombatEntity, EffectStackContainer> _getEffectContainer;

        // Optional: ability runners per entity (cooldown tick etc.)
        private readonly Func<CombatEntity, AbilityRunner> _getAbilityRunner;

        public CombatRuntime(
            EffectSystemLite effectSystem = null,
            Func<CombatEntity, EffectStackContainer> getEffectContainer = null,
            Func<CombatEntity, AbilityRunner> getAbilityRunner = null)
        {
            _effectSystem = effectSystem;
            _getEffectContainer = getEffectContainer;
            _getAbilityRunner = getAbilityRunner;
        }

        public IReadOnlyList<CombatEntity> Entities => _entities;

        public void Register(CombatEntity e)
        {
            if (e == null) return;
            if (_entities.Contains(e)) return;
            _entities.Add(e);
        }

        public void Unregister(CombatEntity e)
        {
            if (e == null) return;
            _entities.Remove(e);
        }

        public void Clear()
        {
            _entities.Clear();
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            // 1) Tick attribute timers / internal versions (if any)
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e?.Attributes != null)
                    e.Attributes.Tick(dt);
            }

            // 2) Tick status durations
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e?.Statuses != null)
                    e.Statuses.Tick(dt);
            }

            // 3) Tick effects (may add/remove statuses or add attribute modifiers)
            if (_effectSystem != null && _getEffectContainer != null)
            {
                for (int i = 0; i < _entities.Count; i++)
                {
                    var e = _entities[i];
                    if (e == null) continue;

                    var c = _getEffectContainer(e);
                    if (c == null) continue;

                    _effectSystem.Tick(e, c, dt);
                }
            }

            // 4) Tick ability runners (cooldowns)
            if (_getAbilityRunner != null)
            {
                for (int i = 0; i < _entities.Count; i++)
                {
                    var e = _entities[i];
                    var runner = e != null ? _getAbilityRunner(e) : null;
                    runner?.Tick(dt);
                }
            }

            // 5) Apply regen from Derived -> Resources (this is the "core regen loop")
            for (int i = 0; i < _entities.Count; i++)
            {
                ApplyRegen(_entities[i], dt);
            }

            // 6) Clamp current resources to max (in case max changed)
            for (int i = 0; i < _entities.Count; i++)
            {
                var e = _entities[i];
                if (e?.Resources != null && e.Attributes != null)
                    e.Resources.ClampToMax(e.Attributes);
            }
        }

        private static void ApplyRegen(CombatEntity e, float dt)
        {
            if (e == null || e.Resources == null || e.Derived == null) return;
            if (e.Resources.IsDead) return;

            // DerivedStatSystem is lazy; calling Get ensures it recalculates when attr/status versions changed.
            float hpPerSec = Mathf.Max(0f, e.Derived.Get(DerivedStatId.HPRegen));
            float mpPerSec = Mathf.Max(0f, e.Derived.Get(DerivedStatId.MPRegen));

            if (hpPerSec > 0f) e.Resources.ApplyHPDelta(hpPerSec * dt);
            if (mpPerSec > 0f) e.Resources.ApplyMPDelta(mpPerSec * dt);
        }
    }
}
