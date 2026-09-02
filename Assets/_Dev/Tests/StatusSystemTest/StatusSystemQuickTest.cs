using UnityEngine;
using TTH.Combat.Status;

namespace TTH.Combat.Tests
{
    /// <summary>
    /// Quick test helper - call from anywhere to test StatusSystem
    /// Can be used as reference for understanding the API
    /// </summary>
    public static class StatusSystemQuickTest
    {
        // Comment out to avoid auto-run (call manually if needed)
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void RunQuickTest()
        {
            // Only run if in development
            if (!Debug.isDebugBuild) return;

            Debug.Log("[StatusSystemQuickTest] Starting...");

            var system = new StatusSystem();

            // Subscribe to changes
            system.OnChanged += (id) =>
            {
                Debug.Log($"  [Event] Status changed: {id}");
            };

            // Test 1: Add status
            Debug.Log("[Test 1] Add Berserk (3 sec)");
            system.AddOrRefresh(StatusId.Berserk, 3f);
            Debug.Log($"  Has Berserk: {system.Has(StatusId.Berserk)}");
            Debug.Log($"  Remaining: {system.GetRemainingSeconds(StatusId.Berserk):F1}s");

            // Test 2: Add multiple
            Debug.Log("[Test 2] Add Slow + Energized");
            system.AddOrRefresh(StatusId.Slow, 2f);
            system.AddOrRefresh(StatusId.Energized, 5f);
            Debug.Log($"  Version: {system.Version}");

            // Test 3: Refresh (same ID)
            Debug.Log("[Test 3] Refresh Berserk");
            system.AddOrRefresh(StatusId.Berserk, 10f);
            Debug.Log($"  Berserk remaining: {system.GetRemainingSeconds(StatusId.Berserk):F1}s");

            // Test 4: Remove
            Debug.Log("[Test 4] Remove Slow");
            system.Remove(StatusId.Slow);
            Debug.Log($"  Has Slow: {system.Has(StatusId.Slow)}");

            // Test 5: Infinite status
            Debug.Log("[Test 5] Add Healing (infinite)");
            system.AddOrRefresh(StatusId.Healing, -1f);
            Debug.Log($"  Healing remaining: {system.GetRemainingSeconds(StatusId.Healing)}");

            // Test 6: Tick
            Debug.Log("[Test 6] Tick 1 second");
            system.Tick(1f);
            Debug.Log($"  Berserk remaining: {system.GetRemainingSeconds(StatusId.Berserk):F1}s");
            Debug.Log($"  Energized remaining: {system.GetRemainingSeconds(StatusId.Energized):F1}s");

            // Test 7: Clear
            Debug.Log("[Test 7] Clear all");
            system.ClearAll();
            Debug.Log($"  Active statuses: {system.Version} (version incremented)");

            Debug.Log("[StatusSystemQuickTest] Complete!");
        }
    }
}
