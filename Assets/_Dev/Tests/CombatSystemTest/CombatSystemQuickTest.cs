using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Derived;
using TTH.Combat.Runtime;
using TTH.Combat.Status;

namespace TTH.Combat.Tests
{
    /// <summary>
    /// Quick test helper - demonstrates CombatSystem usage
    /// </summary>
    public static class CombatSystemQuickTest
    {
        // Comment out to avoid auto-run (call manually if needed)
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void RunQuickTest()
        {
            if (!Debug.isDebugBuild) return;

            Debug.Log("[CombatSystemQuickTest] Starting...");

            // 1. Create entities
            var attacker = BuildEntity(1, 100f, 30f, 5f);
            var defender = BuildEntity(2, 120f, 15f, 20f);

            // 2. Fill resources
            attacker.Resources.FillToMax(attacker.Attributes);
            defender.Resources.FillToMax(defender.Attributes);

            // 3. Create combat system
            var events = new CombatEvents();
            events.OnHit += (ctx) => Debug.Log($"  [Hit] DMG={ctx.finalDamage}, HP={ctx.defenderHPAfter}");
            events.OnHitRejected += (ctx) => Debug.Log($"  [Rejected] {ctx.rejectReason}");

            var combat = new CombatSystem(events);

            // 4. Test basic hit
            Debug.Log("[Test 1] Basic hit");
            var hit = new HitEvent(attacker, defender, Vector2.zero, null, 1);
            var ctx = combat.HandleHit(hit);
            Debug.Log($"  Damage: {ctx.finalDamage} (ATK={ctx.atk} - DEF={ctx.def})");

            // 5. Test dead target
            Debug.Log("[Test 2] Hit dead target");
            defender.Resources.ApplyHPDelta(-10000f);
            var hit2 = new HitEvent(attacker, defender, Vector2.zero, null, 2);
            var ctx2 = combat.HandleHit(hit2);
            Debug.Log($"  Rejected: {ctx2.rejectReason}");

            // 6. Test null attacker
            Debug.Log("[Test 3] Hit with null attacker");
            var hit3 = new HitEvent(null, defender, Vector2.zero, null, 3);
            var ctx3 = combat.HandleHit(hit3);
            Debug.Log($"  Rejected: {ctx3.rejectReason}");

            Debug.Log("[CombatSystemQuickTest] Complete!");
        }

        private static CombatEntity BuildEntity(int id, float baseHP, float baseATK, float baseDEF)
        {
            var stats = new StatBlock();
            stats.SetBase(AttributeId.HP, baseHP);
            stats.SetBase(AttributeId.MP, 50f);
            stats.SetBase(AttributeId.ATK, baseATK);
            stats.SetBase(AttributeId.DEF, baseDEF);
            stats.SetBase(AttributeId.SPD, 10f);
            stats.SetBase(AttributeId.DEX, 10f);
            stats.SetBase(AttributeId.VIT, 5f);
            stats.SetBase(AttributeId.WIS, 5f);

            var caps = new ClassStatCaps();
            caps.SetCap(AttributeId.HP, 255f);
            caps.SetCap(AttributeId.ATK, 75f);
            caps.SetCap(AttributeId.DEF, 75f);
            caps.SetCap(AttributeId.SPD, 75f);
            caps.SetCap(AttributeId.DEX, 75f);
            caps.SetCap(AttributeId.VIT, 75f);
            caps.SetCap(AttributeId.WIS, 75f);
            caps.SetCap(AttributeId.MP, 255f);

            var attributes = new AttributeSystem(stats, caps);
            var statuses = new StatusSystem();
            var derived = new DerivedStatSystem(attributes, statuses, null);  // cfg can be null
            var resources = new ResourcePool(baseHP, 50f);

            return new CombatEntity(id, attributes, statuses, derived, resources);
        }
    }
}
