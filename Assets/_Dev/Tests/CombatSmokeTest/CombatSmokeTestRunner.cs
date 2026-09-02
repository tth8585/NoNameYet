using TTH.Combat.Ability;
using TTH.Combat.Attributes;
using TTH.Combat.Derived;
using TTH.Combat.Effects;
using TTH.Combat.Runtime;
using TTH.Combat.Simulation;
using TTH.Combat.Status;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TTH.Combat.Tests
{
    public sealed class CombatSmokeTestRunner : MonoBehaviour
    {
        [Header("Configs (optional)")]
        [SerializeField] private DerivedStatsConfigSO _derivedCfg;
        [Header("Ability (Phase B)")]
        [SerializeField] private AbilityDefinition _testAbility;

        [Header("Test Controls")]
        [SerializeField] private KeyCode _hitKey = KeyCode.Space;
        [SerializeField] private float _tickDt = 0.1f;

        private CombatEntity _attacker;
        private CombatEntity _defender;

        private CombatSystem _combat;
        private CombatEvents _events;

        // Phase B (lite)
        private EffectSystemLite _effectSystem;
        private EffectStackContainer _defenderEffects;

        private AbilityRunner _abilityRunner;
        private EffectStackContainer _attackerEffects;

        private float _accum;

        private CombatRuntime _runtime;

        private void Awake()
        {
            // --- Build attacker entity ---
            _attacker = BuildEntity(
                id: 1,
                baseHP: 100,
                baseMP: 50,
                baseATK: 30,
                baseDEF: 5,
                baseSPD: 10,
                baseDEX: 10,
                baseVIT: 5,
                baseWIS: 5);

            // --- Build defender entity ---
            _defender = BuildEntity(
                id: 2,
                baseHP: 120,
                baseMP: 30,
                baseATK: 10,
                baseDEF: 20,
                baseSPD: 8,
                baseDEX: 8,
                baseVIT: 5,
                baseWIS: 5);

            // --- Phase B (lite) wiring (optional) ---
            ICombatProc proc = null;
            _effectSystem = new EffectSystemLite();
            _attackerEffects = new EffectStackContainer();
            _defenderEffects = new EffectStackContainer();

            // Fill current resources to MAX
            _attacker.Resources.FillToMax(_attacker.Attributes);
            _defender.Resources.FillToMax(_defender.Attributes);

            // --- Events ---
            _events = new CombatEvents();
            _events.OnHit += ctx => Debug.Log($"[OnHit] dmg={ctx.finalDamage} hpAfter={ctx.defenderHPAfter} valid={ctx.isValidHit}");
            _events.OnDamage += ctx => Debug.Log($"[OnDamage] base={ctx.baseDamage} mitigated={ctx.mitigatedDamage} final={ctx.finalDamage}");
            _events.OnKilled += ctx => Debug.Log($"[OnKilled] defenderId={ctx.defenderId}");

            // --- Ability Runner ---
            _abilityRunner = new AbilityRunner(
                                    _attacker,
                                    effects: _effectSystem,
                                    getContainer: (entity) =>
                                    {
                                        if (entity == _attacker) return _attackerEffects;
                                        if (entity == _defender) return _defenderEffects;
                                        return null;
                                    },
                                    getAlliesInRange: (caster, radius) =>
                                    {
                                        // Smoke test MVP: chưa có team/world => trả về rỗng (chỉ self được apply).
                                        // Khi bạn có runtime world/team, adapter này sẽ trả danh sách ally thật và sort sẵn (nearest/party order).
                                        return System.Array.Empty<CombatEntity>();
                                    }
                                );

            // Proc bridge: đọc AbilityIntent trong ctx.source và apply effects từ AbilityDefinition
            proc = new AbilityEffectProc(
                _effectSystem,
                getContainer: (entity) =>
                {
                    if (entity == _attacker) return _attackerEffects;
                    if (entity == _defender) return _defenderEffects;
                    return null;
                }
            );

            // --- Combat ---
            _combat = new CombatSystem(_events, proc);

            _runtime = new CombatRuntime(
                                        effectSystem: _effectSystem,
                                        getEffectContainer: (entity) => entity == _defender ? _defenderEffects : null,
                                        getAbilityRunner: e => (e == _attacker ? _abilityRunner : null)
                                    );

            _runtime.Register(_attacker);
            _runtime.Register(_defender);

            Debug.Log("CombatSmokeTest READY. Press SPACE to hit. (Optional: assign _onHitEffect to test Phase B)");
        }

        private void Update()
        {
            // Manual hit
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                DoHitOnce();
            }

            // Tick systems at a fixed dt (cheap smoke test)
            _accum += Time.deltaTime;
            while (_accum >= _tickDt)
            {
                _accum -= _tickDt;
                TickRuntime(_tickDt);
            }
        }

        private void DoHitOnce()
        {
            if (_defender.Resources.IsDead)
            {
                Debug.Log("Defender already dead. (Stop test or reset scene)");
                return;
            }

            if (_testAbility == null)
            {
                Debug.LogWarning("Assign _testAbility (AbilityDefinition) in Inspector to test ability.");
                return;
            }

            // Ability gate + create intent
            if (!_abilityRunner.CanUse(_testAbility))
            {
                Debug.Log("[Ability] On cooldown");
                return;
            }

            var intent = _abilityRunner.UseAndCreateIntent(_testAbility, aimPoint: Vector3.zero);

            LogDefenderState(_defender, "[DEFENDER BEFORE HIT]");
            LogAttackerState(_attacker, "[ATTACKER BEFORE HIT]");

            var hit = new HitEvent(
                attacker: _attacker,
                defender: _defender,
                hitPoint: Vector2.zero,
                source: intent,     // unique enough for smoke test
                hitSeq: Time.frameCount);

            var ctx = _combat.HandleHit(hit, DamageKind.Direct);

            // Quick HUD-like log
            Debug.Log($"HIT => finalDamage={ctx.finalDamage}, DefenderHP={_defender.Resources.CurrentHP}/{_defender.Attributes.Get(AttributeId.HP)}");

            LogDefenderState(_defender, "[DEFENDER AFTER HIT]");
            LogAttackerState(_attacker, "[ATTACKER AFTER HIT]");
        }

        private void TickRuntime(float dt)
        {
            _runtime?.Tick(dt);
        }

     
        private void LogAttackerState(CombatEntity target ,string prefix)
        {
            float dex = target.Attributes.Get(AttributeId.DEX);
            float spd = target.Attributes.Get(AttributeId.SPD);

            float moveSpeed = target.Derived.Get(DerivedStatId.MoveSpeed);
            float fireRate = target.Derived.Get(DerivedStatId.FireRate);

            Debug.Log(
               $"{prefix} " +
               $"DEX={dex:0.##}, SPD={spd:0.##} | " +
               $"MoveSpeed={moveSpeed:0.##}, FireRate={fireRate:0.##}"
           );

            if (_attackerEffects != null && _attackerEffects.Active.Count > 0)
            {
                foreach (var inst in _attackerEffects.Active)
                {
                    Debug.Log(
                        $"    [AttackerEffect] {inst.Def.effectId} | " +
                        $"Remaining={inst.RemainingSeconds:0.##}s | " +
                        $"Stacks={inst.Stacks}"
                    );
                }
            }
            else
            {
                Debug.Log("    [AttackerEffect] none");
            }
        }

        private void LogDefenderState(CombatEntity target, string prefix)
        {
            float dex = target.Attributes.Get(AttributeId.DEX);
            float spd = target.Attributes.Get(AttributeId.SPD);

            float moveSpeed = target.Derived.Get(DerivedStatId.MoveSpeed);
            float fireRate = target.Derived.Get(DerivedStatId.FireRate);

            Debug.Log(
                $"{prefix} " +
                $"DEX={dex:0.##}, SPD={spd:0.##} | " +
                $"MoveSpeed={moveSpeed:0.##}, FireRate={fireRate:0.##}"
            );

            // Phase B (lite): log remaining time of effect instances
            if (_defenderEffects != null && _defenderEffects.Active.Count > 0)
            {
                foreach (var inst in _defenderEffects.Active)
                {
                    Debug.Log(
                        $"    [defender Effect] {inst.Def.effectId} | " +
                        $"Remaining={inst.RemainingSeconds:0.##}s | " +
                        $"Stacks={inst.Stacks}"
                    );
                }
            }
            else
            {
                Debug.Log("    [defender Effect] none");
            }
        }


        private CombatEntity BuildEntity(
            int id,
            float baseHP, float baseMP,
            float baseATK, float baseDEF,
            float baseSPD, float baseDEX,
            float baseVIT, float baseWIS)
        {
            // Base stats
            var stats = new StatBlock();
            stats.SetBase(AttributeId.HP, baseHP);
            stats.SetBase(AttributeId.MP, baseMP);
            stats.SetBase(AttributeId.ATK, baseATK);
            stats.SetBase(AttributeId.DEF, baseDEF);
            stats.SetBase(AttributeId.SPD, baseSPD);
            stats.SetBase(AttributeId.DEX, baseDEX);
            stats.SetBase(AttributeId.VIT, baseVIT);
            stats.SetBase(AttributeId.WIS, baseWIS);

            // Class caps (optional for smoke test)
            var caps = new ClassStatCaps();
            caps.SetCap(AttributeId.HP, baseHP);   // progression cap example
            caps.SetCap(AttributeId.MP, baseMP);

            var attr = new AttributeSystem(stats, caps);
            var status = new StatusSystem();

            // Derived config fallback if not assigned
            var cfg = _derivedCfg != null ? _derivedCfg : ScriptableObject.CreateInstance<DerivedStatsConfigSO>();
            var derived = new DerivedStatSystem(attr, status, cfg);

            // Current resources start at 0, then FillToMax() in Awake
            var res = new ResourcePool(0, 0);

            return new CombatEntity(id, attr, status, derived, res);
        }
    }
}
