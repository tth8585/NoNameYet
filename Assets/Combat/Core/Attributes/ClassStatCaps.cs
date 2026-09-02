using System.Collections.Generic;

namespace TTH.Combat.Attributes
{
    /// <summary>
    /// Progression cap per class (ROTMG chuẩn):
    /// - Chỉ áp cho Progression (level/potion)
    /// - Bonus (gear/buff/item/event) được phép vượt cap
    /// </summary>
    public sealed class ClassStatCaps
    {
        private readonly Dictionary<AttributeId, float> _caps = new();

        public void SetCap(AttributeId id, float cap) => _caps[id] = cap;

        public bool TryGetCap(AttributeId id, out float cap) => _caps.TryGetValue(id, out cap);
    }
}
