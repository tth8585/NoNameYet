using TTH.Combat.Attributes;

namespace TTH.Game.Inventory
{
    public readonly struct GameplayAffixModifier
    {
        public GameplayModifierType Type { get; }
        public float Value { get; }
        public ItemInstance Source { get; }

        public GameplayAffixModifier(GameplayModifierType type, float value, ItemInstance source)
        {
            Type = type;
            Value = value;
            Source = source;
        }
    }

    public sealed class AffixModifierBridge
    {
        private readonly AttributeSystem attributes;
        public event System.Action<GameplayAffixModifier> GameplayModifierAdded;

        public AffixModifierBridge(AttributeSystem attributes)
        {
            this.attributes = attributes;
        }

        public void Equip(ItemInstance item)
        {
            if (attributes == null || item == null || item.RandomAffixes == null) return;

            foreach (var affix in item.RandomAffixes)
            {
                if (affix == null) continue;

                if (affix.effectType == AffixEffectType.Gameplay)
                {
                    GameplayModifierAdded?.Invoke(new GameplayAffixModifier(affix.gameplayModifier, affix.value, item));
                    continue;
                }

                attributes.AddModifier(new AttributeModifier
                {
                    Attribute = affix.attribute,
                    Op = affix.operation,
                    Value = affix.value,
                    SourceType = AttributeSourceType.Bonus,
                    Stacking = StackingPolicy.StackAll,
                    StackKey = $"Item_{item.InstanceId}_{affix.affixFamilyId}",
                    Source = item
                });
            }
        }

        public void Unequip(ItemInstance item)
        {
            if (attributes != null && item != null)
                attributes.RemoveBySource(item);
        }
    }
}