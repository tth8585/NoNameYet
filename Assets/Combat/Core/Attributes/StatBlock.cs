using System.Collections.Generic;

namespace TTH.Combat.Attributes
{
    public sealed class StatBlock
    {
        private readonly Dictionary<AttributeId, float> _base = new();

        public void SetBase(AttributeId id, float value) => _base[id] = value;

        public float GetBase(AttributeId id)
        {
            return _base.TryGetValue(id, out var v) ? v : 0f;
        }
    }
}
