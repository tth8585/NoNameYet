using UnityEngine;
using TTH.Combat.Attributes;

namespace TTH.Combat.Tests
{
    /// <summary>
    /// Quick test helper - call from anywhere to test AttributeSystem
    /// Can be used as reference for understanding the API
    /// </summary>
    public static class AttributeSystemQuickTest
    {
        // Comment out to avoid auto-run (call manually if needed)
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void RunQuickTest()
        {
            // Only run if in development
            if (!Debug.isDebugBuild) return;

            Debug.Log("[AttributeSystemQuickTest] Starting...");

            // 1. Create base stats
            var stats = new StatBlock();
            stats.SetBase(AttributeId.ATK, 30f);
            stats.SetBase(AttributeId.DEF, 10f);
            stats.SetBase(AttributeId.HP, 100f);

            // 2. Create class caps
            var caps = new ClassStatCaps();
            caps.SetCap(AttributeId.ATK, 75f);
            caps.SetCap(AttributeId.DEF, 75f);
            caps.SetCap(AttributeId.HP, 255f);

            // 3. Create system
            var system = new AttributeSystem(stats, caps);

            // 4. Test basic Get
            Debug.Log($"Base ATK: {system.Get(AttributeId.ATK)}");
            Debug.Log($"Base DEF: {system.Get(AttributeId.DEF)}");

            // 5. Test Add modifier
            var addMod = new AttributeModifier
            {
                Attribute = AttributeId.ATK,
                Op = ModifierOp.Add,
                Value = 15f,
                DurationSeconds = -1f,
                SourceType = AttributeSourceType.Bonus
            };
            system.AddModifier(addMod);
            Debug.Log($"After +15 ATK modifier: {system.Get(AttributeId.ATK)}");

            // 6. Test Override
            system.SetOverride(AttributeId.DEF, 5f);
            Debug.Log($"After DEF override to 5: {system.Get(AttributeId.DEF)}");

            Debug.Log("[AttributeSystemQuickTest] Complete!");
        }
    }
}
