using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Attributes;

namespace TTH.Combat.Tests
{
    /// <summary>
    /// Test AttributeSystem:
    /// - Base stats
    /// - Modifiers (add, multiply, set)
    /// - Stacking policies
    /// - Override layer
    /// - Dirty tracking / caching
    /// - Class stat caps
    /// </summary>
    public class AttributeSystemTestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _autoRunOnAwake = true;
        [SerializeField] private float _testTickDt = 0.1f;

        private AttributeSystem _system;
        private float _accum;

        private void Awake()
        {
            if (!_autoRunOnAwake) return;
            Initialize();
        }

        private void Update()
        {
            if (_system == null) return;

            _accum += Time.deltaTime;
            while (_accum >= _testTickDt)
            {
                _accum -= _testTickDt;
                _system.Tick(_testTickDt);
                PrintStats();
            }
        }

        public void Initialize()
        {
            // Create stat block with base values
            var stats = new StatBlock();
            stats.SetBase(AttributeId.HP, 100f);
            stats.SetBase(AttributeId.MP, 50f);
            stats.SetBase(AttributeId.ATK, 30f);
            stats.SetBase(AttributeId.DEF, 10f);
            stats.SetBase(AttributeId.SPD, 15f);
            stats.SetBase(AttributeId.DEX, 12f);
            stats.SetBase(AttributeId.VIT, 8f);
            stats.SetBase(AttributeId.WIS, 6f);

            // Create class caps (ROTMG style)
            var caps = new ClassStatCaps();
            caps.SetCap(AttributeId.HP, 255f);
            caps.SetCap(AttributeId.MP, 255f);
            caps.SetCap(AttributeId.ATK, 75f);
            caps.SetCap(AttributeId.DEF, 75f);
            caps.SetCap(AttributeId.SPD, 75f);
            caps.SetCap(AttributeId.DEX, 75f);
            caps.SetCap(AttributeId.VIT, 75f);
            caps.SetCap(AttributeId.WIS, 75f);

            _system = new AttributeSystem(stats, caps);

            Debug.Log("[AttributeSystemTest] Initialized with base stats");
            PrintStats();
            PrintBaseStats();
        }

        public void TestAddModifier()
        {
            if (_system == null) return;

            var mod = new AttributeModifier
            {
                Attribute = AttributeId.ATK,
                Op = ModifierOp.Add,
                Value = 10f,
                DurationSeconds = 3f,
                RemainingSeconds = 3f,
                SourceType = AttributeSourceType.Bonus
            };

            _system.AddModifier(mod);
            Debug.Log("[Test] Added +10 ATK for 3 seconds");
            PrintStats();
        }

        public void TestMultiplyModifier()
        {
            if (_system == null) return;

            var mod = new AttributeModifier
            {
                Attribute = AttributeId.DEF,
                Op = ModifierOp.Multiply,
                Value = 1.5f,
                DurationSeconds = 4f,
                RemainingSeconds = 4f,
                SourceType = AttributeSourceType.Bonus
            };

            _system.AddModifier(mod);
            Debug.Log("[Test] Added x1.5 DEF for 4 seconds");
            PrintStats();
        }

        public void TestOverride()
        {
            if (_system == null) return;

            _system.SetOverride(AttributeId.SPD, 0f);
            Debug.Log("[Test] Set SPD override to 0 (Paralyzed effect)");
            PrintStats();

            // Schedule clear after 2 seconds
            Invoke(nameof(ClearOverride), 2f);
        }

        public void ClearOverride()
        {
            if (_system == null) return;
            _system.ClearOverride(AttributeId.SPD);
            Debug.Log("[Test] Cleared SPD override");
            PrintStats();
        }

        public void TestCapHitting()
        {
            if (_system == null) return;

            // Add progression mods that would exceed cap
            for (int i = 0; i < 3; i++)
            {
                var mod = new AttributeModifier
                {
                    Attribute = AttributeId.ATK,
                    Op = ModifierOp.Add,
                    Value = 30f,  // Total would be 30 + 90 = 120, but capped at 75
                    DurationSeconds = -1f,  // Permanent for this test
                    SourceType = AttributeSourceType.Progression
                };
                _system.AddModifier(mod);
            }

            Debug.Log("[Test] Added 3x +30 ATK (Progression source) - should be capped at 75");
            PrintStats();
        }

        public void TestStackingPolicy()
        {
            if (_system == null) return;

            // Add unique modifier (can stack multiple times)
            for (int i = 0; i < 3; i++)
            {
                var mod = new AttributeModifier
                {
                    Attribute = AttributeId.VIT,
                    Op = ModifierOp.Add,
                    Value = 5f,
                    DurationSeconds = 5f,
                    RemainingSeconds = 5f,
                    SourceType = AttributeSourceType.Bonus,
                    Stacking = StackingPolicy.UniqueByStackKey,
                    StackKey = "buff_vit"
                };
                _system.AddModifier(mod);
            }

            Debug.Log("[Test] Added 3 VIT modifiers with UniqueByStackKey - should stack");
            PrintStats();
        }

        public void PrintBaseStats()
        {
            var baseStats = new[] { AttributeId.HP, AttributeId.MP, AttributeId.ATK, AttributeId.DEF, AttributeId.SPD, AttributeId.DEX, AttributeId.VIT, AttributeId.WIS };
            Debug.Log("=== BASE STATS ===");
            foreach (var attr in baseStats)
            {
                Debug.Log($"  {attr}: [BASE]");
            }
        }

        public void PrintStats()
        {
            var attrs = new[] { AttributeId.HP, AttributeId.MP, AttributeId.ATK, AttributeId.DEF, AttributeId.SPD, AttributeId.DEX, AttributeId.VIT, AttributeId.WIS };

            Debug.Log("=== CURRENT STATS ===");
            foreach (var attr in attrs)
            {
                var value = _system.Get(attr);
                var hasOverride = _system.HasOverride(attr);
                var overrideStr = hasOverride ? " [OVERRIDE]" : "";
                Debug.Log($"  {attr}: {value}{overrideStr}");
            }
        }

        // Quick test methods for UI buttons
        public void BtnTest1() => TestAddModifier();
        public void BtnTest2() => TestMultiplyModifier();
        public void BtnTest3() => TestOverride();
        public void BtnTest4() => TestCapHitting();
        public void BtnTest5() => TestStackingPolicy();
    }
}
