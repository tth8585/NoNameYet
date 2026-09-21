using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Ability;
using TTH.Combat.Attributes;
using TTH.Combat.Derived;
using TTH.Combat.Effects;
using TTH.Combat.Runtime;
using TTH.Combat.Simulation;
using TTH.Combat.Status;

namespace TTH.Combat.Tests
{
    /// <summary>
    /// Clean Combat System test runner:
    /// - Test basic hits
    /// - Test damage calculation (ATK - DEF)
    /// - Test invalid hits (dead target, null entity)
    /// - Test effect/proc integration
    /// - Test event emissions (OnHit, OnDamage, OnKilled, OnHitRejected)
    /// </summary>
    public class CombatSystemTestRunner : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private float _tickDt = 0.1f;

        private CombatEntity _attacker;
        private CombatEntity _defender;
        private CombatSystem _combat;
        private CombatEvents _events;
        private CombatRuntime _runtime;

        // For effects (Phase B - optional)
        private EffectSystemLite _effectSystem;
        private EffectStackContainer _attackerEffects;
        private EffectStackContainer _defenderEffects;

        private float _accum;
        private int _hitCounter;

        private void Awake()
        {
            if (!_autoInitialize) return;
            Initialize();
        }

        private void Update()
        {
            // Note: CombatRuntime.Tick requires DerivedStatsConfigSO which we don't have in test.
            // For this test, we only care about CombatSystem hit logic, not derived stats.
            // If you need derived stats, create DerivedStatsConfigSO asset and assign it.
        }

        public void Initialize()
        {
            Debug.Log("[CombatSystemTest] Initializing...");

            // Create attacker
            _attacker = BuildEntity(
                id: 1,
                baseHP: 100f,
                baseMP: 50f,
                baseATK: 30f,
                baseDEF: 5f,
                baseSPD: 15f,
                baseDEX: 12f,
                baseVIT: 8f,
                baseWIS: 6f
            );

            // Create defender
            _defender = BuildEntity(
                id: 2,
                baseHP: 120f,
                baseMP: 40f,
                baseATK: 15f,
                baseDEF: 20f,
                baseSPD: 10f,
                baseDEX: 8f,
                baseVIT: 10f,
                baseWIS: 5f
            );

            // Setup effects system (Phase B)
            _effectSystem = new EffectSystemLite();
            _attackerEffects = new EffectStackContainer();
            _defenderEffects = new EffectStackContainer();

            // Fill to max HP/MP
            _attacker.Resources.FillToMax(_attacker.Attributes);
            _defender.Resources.FillToMax(_defender.Attributes);

            // Setup events
            _events = new CombatEvents();
            _events.OnHit += (ctx) => Debug.Log($"[OnHit] Valid={ctx.isValidHit}, DMG={ctx.finalDamage}, HP={ctx.defenderHPAfter}");
            _events.OnDamage += (ctx) => Debug.Log($"[OnDamage] Base={ctx.baseDamage}, Mitigated={ctx.mitigatedDamage}, Final={ctx.finalDamage}");
            _events.OnKilled += (ctx) => Debug.Log($"[OnKilled] Defender {ctx.defenderId} is dead!");
            _events.OnHitRejected += (ctx) => Debug.Log($"[OnHitRejected] Reason={ctx.rejectReason}");

            // Setup combat system (no proc initially)
            _combat = new CombatSystem(_events);

            // Setup runtime (without DerivedStatsConfigSO - not needed for hit testing)
            // If you need derived stats, create ScriptableObject asset and pass it to DerivedStatSystem
            _runtime = new CombatRuntime(
                effectSystem: _effectSystem,
                getEffectContainer: (e) => (e == _attacker ? _attackerEffects : _defenderEffects)
                // Not ticking runtime to avoid DerivedStatSystem.Tick which needs config
            );
            _runtime.Register(_attacker);
            _runtime.Register(_defender);

            Debug.Log("[CombatSystemTest] Ready!");
            LogEntityState("[INITIAL STATE]");

            // Auto-run some tests
            Debug.Log("\n=== AUTO TESTS ===");
            TestBasicHit();
            TestMultipleHits();
            Debug.Log("=== END AUTO TESTS ===\n");
        }

        public void TestBasicHit()
        {
            if (_combat == null) return;

            LogEntityState("[BEFORE HIT]");

            var hit = new HitEvent(
                attacker: _attacker,
                defender: _defender,
                hitPoint: Vector2.zero,
                source: null,
                hitSeq: _hitCounter++
            );

            var ctx = _combat.HandleHit(hit, DamageKind.Direct);

            Debug.Log($"[TestBasicHit] Damage dealt: {ctx.finalDamage}");
            LogEntityState("[AFTER HIT]");
        }

        public void TestMultipleHits()
        {
            if (_combat == null) return;

            Debug.Log("[TestMultipleHits] Firing 5 hits...");
            for (int i = 0; i < 5; i++)
            {
                var hit = new HitEvent(_attacker, _defender, Vector2.zero, null, _hitCounter++);
                var ctx = _combat.HandleHit(hit);
                Debug.Log($"  Hit {i + 1}: DMG={ctx.finalDamage}, DefenderHP={ctx.defenderHPAfter}");

                if (_defender.Resources.IsDead)
                {
                    Debug.Log("[TestMultipleHits] Defender is dead!");
                    break;
                }
            }

            LogEntityState("[AFTER HITS]");
        }

        public void TestHitDeadTarget()
        {
            if (_combat == null) return;

            // Kill defender first
            _defender.Resources.ApplyHPDelta(-10000f);
            Debug.Log($"[TestHitDeadTarget] Defender killed (HP={_defender.Resources.CurrentHP})");

            // Try to hit dead target
            var hit = new HitEvent(_attacker, _defender, Vector2.zero, null, _hitCounter++);
            var ctx = _combat.HandleHit(hit);

            Debug.Log($"[TestHitDeadTarget] Hit rejected: {!ctx.isValidHit}, Reason={ctx.rejectReason}");
        }

        public void TestHitNullAttacker()
        {
            if (_combat == null) return;

            var hit = new HitEvent(
                attacker: null,  // Invalid
                defender: _defender,
                hitPoint: Vector2.zero,
                source: null,
                hitSeq: _hitCounter++
            );

            var ctx = _combat.HandleHit(hit);
            Debug.Log($"[TestHitNullAttacker] Hit rejected: {!ctx.isValidHit}, Reason={ctx.rejectReason}");
        }

        public void TestDamageVariation()
        {
            if (_combat == null) return;

            Debug.Log("[TestDamageVariation] Testing damage calculation...");

            // Base: ATK=30, DEF=20 -> damage = 30-20 = 10
            var baseDmg = _attacker.Attributes.Get(AttributeId.ATK) - _defender.Attributes.Get(AttributeId.DEF);
            Debug.Log($"  Expected: {baseDmg}");

            var hit = new HitEvent(_attacker, _defender, Vector2.zero, null, _hitCounter++);
            var ctx = _combat.HandleHit(hit);
            Debug.Log($"  Actual: {ctx.finalDamage}");
            Debug.Log($"  Match: {Mathf.Approximately(ctx.finalDamage, baseDmg)}");
        }

        public void TestDefenderModification()
        {
            if (_combat == null) return;

            // Add DEF buff to defender
            var defBuff = new AttributeModifier
            {
                Attribute = AttributeId.DEF,
                Op = ModifierOp.Add,
                Value = 10f,
                DurationSeconds = -1f,
                SourceType = AttributeSourceType.Bonus
            };
            _defender.Attributes.AddModifier(defBuff);

            Debug.Log($"[TestDefenderModification] Added +10 DEF");
            var newDEF = _defender.Attributes.Get(AttributeId.DEF);
            Debug.Log($"  New DEF: {newDEF}");

            var hit = new HitEvent(_attacker, _defender, Vector2.zero, null, _hitCounter++);
            var ctx = _combat.HandleHit(hit);
            Debug.Log($"  Damage with buff: {ctx.finalDamage} (was 10, now should be 0)");

            LogEntityState("[AFTER BUFF]");
        }

        public void TestReset()
        {
            if (_attacker == null || _defender == null) return;

            // Reset defender to full HP
            _defender.Resources.FillToMax(_defender.Attributes);
            // Clear all modifiers
            _defender.Attributes.ClearAllModifiers();

            Debug.Log("[TestReset] Defender reset to full HP and clear modifiers");
            LogEntityState("[AFTER RESET]");
        }

        private CombatEntity BuildEntity(int id, float baseHP, float baseMP, float baseATK, float baseDEF, float baseSPD, float baseDEX, float baseVIT, float baseWIS)
        {
            var stats = new StatBlock();
            stats.SetBase(AttributeId.HP, baseHP);
            stats.SetBase(AttributeId.MP, baseMP);
            stats.SetBase(AttributeId.ATK, baseATK);
            stats.SetBase(AttributeId.DEF, baseDEF);
            stats.SetBase(AttributeId.SPD, baseSPD);
            stats.SetBase(AttributeId.DEX, baseDEX);
            stats.SetBase(AttributeId.VIT, baseVIT);
            stats.SetBase(AttributeId.WIS, baseWIS);

            var caps = new ClassStatCaps();
            caps.SetCap(AttributeId.HP, 255f);
            caps.SetCap(AttributeId.MP, 255f);
            caps.SetCap(AttributeId.ATK, 75f);
            caps.SetCap(AttributeId.DEF, 75f);
            caps.SetCap(AttributeId.SPD, 75f);
            caps.SetCap(AttributeId.DEX, 75f);
            caps.SetCap(AttributeId.VIT, 75f);
            caps.SetCap(AttributeId.WIS, 75f);

            var attributes = new AttributeSystem(stats, caps);
            var statuses = new StatusSystem();
            var derived = new DerivedStatSystem(attributes, statuses, (DerivedStatsConfigSO)null);  // cfg can be null
            var resources = new ResourcePool(baseHP, baseMP);

            return new CombatEntity(id, attributes, statuses, derived, resources);
        }

        private void LogEntityState(string prefix)
        {
            Debug.Log($"{prefix}");
            Debug.Log($"  Attacker: HP={_attacker.Resources.CurrentHP}, ATK={_attacker.Attributes.Get(AttributeId.ATK)}");
            Debug.Log($"  Defender: HP={_defender.Resources.CurrentHP}, DEF={_defender.Attributes.Get(AttributeId.DEF)}, Dead={_defender.Resources.IsDead}");
        }

        // Quick test methods
        public void BtnTest1() => TestBasicHit();
        public void BtnTest2() => TestMultipleHits();
        public void BtnTest3() => TestHitDeadTarget();
        public void BtnTest4() => TestHitNullAttacker();
        public void BtnTest5() => TestDamageVariation();
    }
}
