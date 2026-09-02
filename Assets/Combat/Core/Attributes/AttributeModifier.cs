using System;

namespace TTH.Combat.Attributes
{
    [Serializable]
    public sealed class AttributeModifier
    {
        public AttributeId Attribute;
        public ModifierOp Op;

        /// <summary>
        /// Với Add: value là số cộng thẳng.
        /// Với Multiply: value là tỉ lệ (0.2 = +20%).
        /// Với Override/Min/Max: value là giá trị tuyệt đối.
        /// </summary>
        public float Value;

        /// <summary>
        /// Progression: level/potion (bị cap theo ClassStatCaps)
        /// Bonus: gear/buff/item/event (không cap, có thể vượt cap)
        /// </summary>
        public AttributeSourceType SourceType = AttributeSourceType.Bonus;

        /// <summary>
        /// Nhỏ hơn chạy trước. Bạn có thể dùng để đảm bảo thứ tự aura/equip/buff…
        /// </summary>
        public int Order;

        public StackingPolicy Stacking;

        /// <summary>
        /// Dùng cho UniqueByStackKey / RefreshDuration… ví dụ: "Potion_ATK", "Aura_Blessing"
        /// </summary>
        public string StackKey;

        /// <summary>
        /// null/empty = vô hạn. >0 = có thời hạn.
        /// </summary>
        public float DurationSeconds;

        /// <summary>
        /// Ai tạo ra modifier (skill, item, enemy…). Dùng để remove theo source.
        /// </summary>
        public object Source;

        public bool IsExpired => DurationSeconds > 0f && RemainingSeconds <= 0f;

        [NonSerialized] public float RemainingSeconds;

        public AttributeModifier CloneRuntime()
        {
            return new AttributeModifier
            {
                Attribute = Attribute,
                Op = Op,
                SourceType = SourceType,
                Value = Value,
                Order = Order,
                Stacking = Stacking,
                StackKey = StackKey,
                DurationSeconds = DurationSeconds,
                RemainingSeconds = DurationSeconds,
                Source = Source
            };
        }
    }
}
