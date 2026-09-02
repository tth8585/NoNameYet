using System;
using System.Collections.Generic;

namespace TTH.Combat.Status
{
    /// <summary>
    /// Runtime statuses: add/remove, tick duration.
    /// Stacking policy (current): Unique + RefreshDuration (same StatusId refreshes duration).
    /// </summary>
    public sealed class StatusSystem
    {
        public event Action<StatusId> OnChanged;
        public int Version { get; private set; }

        private readonly Dictionary<StatusId, StatusInstance> _active = new();

        public void Tick(float dt)
        {
            if (_active.Count == 0) return;

            // Iterate over a copy to allow remove while iterating
            _toRemove.Clear();

            foreach (var kv in _active)
            {
                var s = kv.Value;
                if (s.DurationSeconds <= 0f) continue;

                s.RemainingSeconds -= dt;
                if (s.RemainingSeconds <= 0f)
                    _toRemove.Add(kv.Key);
            }

            for (int i = 0; i < _toRemove.Count; i++)
                Remove(_toRemove[i]);
        }

        /// <summary>
        /// Add or refresh a status. Same StatusId refreshes duration (if duration > 0).
        /// </summary>
        public void AddOrRefresh(StatusId id, float durationSeconds)
        {
            if (_active.TryGetValue(id, out var exist))
            {
                // Refresh duration semantics: reset to full duration
                if (durationSeconds > 0f)
                {
                    exist.DurationSeconds = durationSeconds;
                    exist.RemainingSeconds = durationSeconds;
                }
                MarkChanged(id);
                return;
            }

            var inst = new StatusInstance(id, durationSeconds);
            _active[id] = inst;
            MarkChanged(id);
        }

        public void Remove(StatusId id)
        {
            if (_active.Remove(id))
                MarkChanged(id);
        }

        public void ClearAll()
        {
            if (_active.Count == 0) return;
            _active.Clear();
            Version++;
            // not firing events per status to keep it cheap
            OnChanged?.Invoke(default);
        }

        public bool Has(StatusId id) => _active.ContainsKey(id);

        public float GetRemainingSeconds(StatusId id)
        {
            return _active.TryGetValue(id, out var s) ? s.RemainingSeconds : 0f;
        }

        private void MarkChanged(StatusId id)
        {
            Version++;
            OnChanged?.Invoke(id);
        }

        private static readonly List<StatusId> _toRemove = new();
    }
}
