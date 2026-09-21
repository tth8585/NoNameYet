using System;
using UnityEngine;
using TTH.Combat.Attributes;

namespace TTH.Game.Inventory
{
    public enum ItemType
    {
        Equipment,
        Consumable,
        Quest,
        Currency,
        Material
    }

    public enum ItemRarity
    {
        Common,
        Magic,
        Rare,
        Legendary
    }

    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Item Definition", fileName = "Item_")]
    public sealed class ItemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Rules")]
        public ItemType itemType;
        public ItemRarity rarity;
        [Min(1)] public int maxStack = 1;
        public string[] tags;
        public int sellValue;

        [Header("Equipment")]
        public string equipmentSlotId;
        public AttributeModifier[] equippedModifiers;

        [Header("Random Affixes")]
        public RandomAffixPoolSO randomAffixPool;
        public RandomAffixRules randomAffixRules;

        public bool IsEquipment => itemType == ItemType.Equipment;

        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || tags == null) return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}