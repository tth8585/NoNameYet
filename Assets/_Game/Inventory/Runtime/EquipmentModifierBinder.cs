using TTH.Combat.Attributes;

namespace TTH.Game.Inventory
{
    public sealed class EquipmentModifierBinder
    {
        private readonly AttributeSystem attributes;

        public EquipmentModifierBinder(AttributeSystem attributes)
        {
            this.attributes = attributes;
        }

        public void Equip(ItemInstance instance)
        {
            if (attributes == null || instance?.Definition?.equippedModifiers == null) return;
            foreach (var modifier in instance.Definition.equippedModifiers)
            {
                if (modifier == null) continue;
                var runtime = modifier.CloneRuntime();
                runtime.Source = instance;
                attributes.AddModifier(runtime);
            }
        }

        public void Unequip(ItemInstance instance)
        {
            if (attributes != null && instance != null)
                attributes.RemoveBySource(instance);
        }
    }
}