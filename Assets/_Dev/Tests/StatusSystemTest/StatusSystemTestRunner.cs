using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Status;

namespace TTH.Combat.Tests
{
    /// <summary>
    /// Test StatusSystem:
    /// - Add/Remove statuses
    /// - Duration tracking
    /// - Refresh behavior (same status ID refreshes duration)
    /// - Version tracking
    /// - Event system
    /// </summary>
    public class StatusSystemTestRunner : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _autoRunOnAwake = true;
        [SerializeField] private float _testTickDt = 0.1f;

        private StatusSystem _system;
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
                PrintStatuses();
            }
        }

        public void Initialize()
        {
            _system = new StatusSystem();
            _system.OnChanged += OnStatusChanged;

            Debug.Log("[StatusSystemTest] Initialized");
            PrintStatuses();
        }

        private void OnStatusChanged(StatusId id)
        {
            Debug.Log($"[StatusChanged] {id}");
        }

        public void TestAddStatus()
        {
            if (_system == null) return;

            _system.AddOrRefresh(StatusId.Berserk, 3f);
            Debug.Log("[Test] Added Berserk status (3 seconds)");
            PrintStatuses();
        }

        public void TestAddMultiple()
        {
            if (_system == null) return;

            _system.AddOrRefresh(StatusId.Slow, 4f);
            _system.AddOrRefresh(StatusId.Paralyze, 2f);
            _system.AddOrRefresh(StatusId.Energized, 5f);

            Debug.Log("[Test] Added 3 statuses");
            PrintStatuses();
        }

        public void TestRefreshDuration()
        {
            if (_system == null) return;

            if (!_system.Has(StatusId.Berserk))
            {
                _system.AddOrRefresh(StatusId.Berserk, 2f);
                Debug.Log("[Test] Added Berserk (2 sec)");
            }
            else
            {
                var remaining = _system.GetRemainingSeconds(StatusId.Berserk);
                _system.AddOrRefresh(StatusId.Berserk, 5f);
                Debug.Log($"[Test] Refreshed Berserk - was {remaining:F1}s, now 5s");
            }

            PrintStatuses();
        }

        public void TestRemove()
        {
            if (_system == null) return;

            if (_system.Has(StatusId.Berserk))
            {
                _system.Remove(StatusId.Berserk);
                Debug.Log("[Test] Removed Berserk");
            }
            else
            {
                Debug.LogWarning("[Test] Berserk not active");
            }

            PrintStatuses();
        }

        public void TestClearAll()
        {
            if (_system == null) return;

            _system.ClearAll();
            Debug.Log("[Test] Cleared all statuses");
            PrintStatuses();
        }

        public void TestInfiniteStatus()
        {
            if (_system == null) return;

            _system.AddOrRefresh(StatusId.Healing, -1f);  // Negative = infinite
            Debug.Log("[Test] Added Healing status (infinite duration)");
            PrintStatuses();
        }

        public void TestVersionTracking()
        {
            if (_system == null) return;

            var v1 = _system.Version;
            _system.AddOrRefresh(StatusId.Dazed, 2f);
            var v2 = _system.Version;
            _system.AddOrRefresh(StatusId.Dazed, 3f);  // Refresh, should increment version
            var v3 = _system.Version;
            _system.Remove(StatusId.Dazed);
            var v4 = _system.Version;

            Debug.Log($"[Test] Version tracking: {v1} → {v2} → {v3} → {v4}");
            Debug.Log($"[Test] Version incremented: {v1 < v2 && v2 < v3 && v3 < v4}");
        }

        public void PrintStatuses()
        {
            var allStatuses = new[] { StatusId.Berserk, StatusId.Slow, StatusId.Paralyze, StatusId.Speedy, StatusId.Dazed, StatusId.Healing, StatusId.Energized };

            Debug.Log("=== ACTIVE STATUSES ===");
            int activeCount = 0;
            foreach (var status in allStatuses)
            {
                if (_system.Has(status))
                {
                    var remaining = _system.GetRemainingSeconds(status);
                    var durStr = remaining > 0 ? $"{remaining:F1}s" : "∞";
                    Debug.Log($"  ✓ {status} [{durStr}]");
                    activeCount++;
                }
            }

            if (activeCount == 0)
                Debug.Log("  (none)");

            Debug.Log($"[Version: {_system.Version}]");
        }

        // Quick test methods for UI buttons
        public void BtnTest1() => TestAddStatus();
        public void BtnTest2() => TestAddMultiple();
        public void BtnTest3() => TestRefreshDuration();
        public void BtnTest4() => TestRemove();
        public void BtnTest5() => TestClearAll();
    }
}
