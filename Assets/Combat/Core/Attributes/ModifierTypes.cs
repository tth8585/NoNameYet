namespace TTH.Combat.Attributes
{
    public enum ModifierOp
    {
        Add,        // + flat
        Multiply,   // * (1 + value)  (value=0.2 => +20%)
    }

    public enum StackingPolicy
    {
        StackAll,           // cộng dồn tất cả modifiers cùng type/op
        HighestOnly,        // chỉ lấy cái mạnh nhất
        LowestOnly,         // chỉ lấy cái yếu nhất
        RefreshDuration,    // không cộng dồn value, nhưng refresh duration
        UniqueByStackKey    // mỗi stackKey chỉ tồn tại 1 (replace)
    }
}
