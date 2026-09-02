using System.Collections.Generic;
using System;
using System.Linq;

namespace TTH.Combat.Attributes
{
    /// <summary>
    /// AttributeSystem
    ///
    /// Runtime owner of all attribute data:
    /// - Base stats
    /// - Runtime modifiers (duration, stacking, source)
    /// - Override layer (highest priority)
    /// - Cache & dirty tracking for derived stats
    ///
    /// NOTE:
    /// - Actual math is delegated to AttributeResolver
    /// - This class is stateful
    /// </summary>
    public sealed class AttributeSystem
    {
        public int Version { get; private set; }

        /// <summary>
        /// Tăng mỗi khi bất kỳ attribute nào dirty. Dùng cho cache Derived layer.
        /// </summary>
        public event Action<AttributeId> OnDirty;

        private readonly StatBlock _stats;
        private readonly ClassStatCaps _caps;

        private readonly List<AttributeModifier> _mods = new();
        private readonly Dictionary<AttributeId, float> _cache = new();
        private readonly HashSet<AttributeId> _dirty = new();

        // Override layer (priority cao nhất, nằm ngoài pipeline)
        private readonly Dictionary<AttributeId, float> _overrides = new();

        public AttributeSystem(StatBlock stats, ClassStatCaps caps)
        {
            _stats = stats;
            _caps = caps;
        }

        public void Tick(float dt)
        {
            for (int i = _mods.Count - 1; i >= 0; i--)
            {
                var m = _mods[i];
                if (m.DurationSeconds <= 0f) continue;

                m.RemainingSeconds -= dt;
                if (m.RemainingSeconds <= 0f)
                {
                    _mods.RemoveAt(i);
                    MarkDirty(m.Attribute);
                }
            }
        }

        public void SetBase(AttributeId id, float value)
        {
            _stats.SetBase(id, value);
            MarkDirty(id);
        }

        /// <summary>
        /// Override dùng cho trạng thái kiểu Paralyzed (SPD=0), Stasis...
        /// Đây là layer cao nhất. Khi bật override thì consumer đọc ra stat override ngay.
        /// </summary>
        public void SetOverride(AttributeId id, float value)
        {
            _overrides[id] = value;
            MarkDirty(id);
        }

        public void ClearOverride(AttributeId id)
        {
            if (_overrides.Remove(id))
                MarkDirty(id);
        }

        public bool HasOverride(AttributeId id) => _overrides.ContainsKey(id);

        public float Get(AttributeId id)
        {
            // 0) Override layer (priority cao nhất)
            if (_overrides.TryGetValue(id, out var ov))
            {
                // hiện tại chỉ clamp min=0 thôi
                // (nếu bạn muốn override bỏ qua cap, comment dòng clamp lại)
                return ov < 0 ? 0 : ov;
            }

            if (!_dirty.Contains(id) && _cache.TryGetValue(id, out var cached))
                return cached;

            var baseValue = _stats.GetBase(id);
            var finalValue = AttributeResolver.Resolve(id, baseValue, _mods, _caps);

            _cache[id] = finalValue;
            _dirty.Remove(id);
            return finalValue;
        }

        public void AddModifier(AttributeModifier modifier)
        {
            if (modifier == null) return;

            var runtime = modifier.CloneRuntime();

            // Normalize stacking policies khi add:
            switch (runtime.Stacking)
            {
                case StackingPolicy.UniqueByStackKey:
                    if (!string.IsNullOrEmpty(runtime.StackKey))
                    {
                        int idx = _mods.FindIndex(m =>
                            m.Attribute == runtime.Attribute &&
                            m.Op == runtime.Op &&
                            m.Stacking == StackingPolicy.UniqueByStackKey &&
                            m.StackKey == runtime.StackKey);

                        if (idx >= 0) _mods[idx] = runtime;
                        else _mods.Add(runtime);

                        MarkDirty(runtime.Attribute);
                        return;
                    }
                    _mods.Add(runtime);
                    MarkDirty(runtime.Attribute);
                    return;
                case StackingPolicy.RefreshDuration:
                    if (!string.IsNullOrEmpty(runtime.StackKey))
                    {
                        var exist = _mods.FirstOrDefault(m =>
                            m.Attribute == runtime.Attribute &&
                            m.Op == runtime.Op &&
                            m.Stacking == StackingPolicy.RefreshDuration &&
                            m.StackKey == runtime.StackKey);

                        if (exist != null)
                        {
                            exist.RemainingSeconds = runtime.DurationSeconds;
                            MarkDirty(runtime.Attribute);
                            return;
                        }
                    }
                    _mods.Add(runtime);
                    MarkDirty(runtime.Attribute);
                    return;
                default:
                    _mods.Add(runtime);
                    MarkDirty(runtime.Attribute);
                    return;
            }
        }

        public void RemoveBySource(object source)
        {
            if (source == null) return;

            for (int i = _mods.Count - 1; i >= 0; i--)
            {
                if (_mods[i].Source == source)
                {
                    var attr = _mods[i].Attribute;
                    _mods.RemoveAt(i);
                    MarkDirty(attr);
                }
            }
        }

        public void ClearAllModifiers()
        {
            if (_mods.Count == 0) return;
            foreach (var m in _mods) MarkDirty(m.Attribute);
            _mods.Clear();
        }

        private float ClampByCap(AttributeId id, float value)
        {
            if (value < 0f) value = 0f;
            if (_caps != null && _caps.TryGetCap(id, out var cap))
            {
                if (value > cap) value = cap;
            }
            return value;
        }

        private void MarkDirty(AttributeId id)
        {
            _dirty.Add(id);
            Version++;
            OnDirty?.Invoke(id);
        }
    }
}
