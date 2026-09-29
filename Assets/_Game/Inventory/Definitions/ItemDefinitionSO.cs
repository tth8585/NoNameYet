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

    public enum ConsumableResource
    {
        None,
        HP,
        MP
    }

    public enum WeaponType
    {
        None,
        Sword,
        Bow,
        Staff
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

        [Header("Consumable")]
        public ConsumableResource consumeResource;
        [Min(0f)] public float consumeAmount;

        [Header("Equipment")]
        public string equipmentSlotId;
        public AttributeModifier[] equippedModifiers;

        [Header("Weapon")]
        public WeaponType weaponType;
        [Min(0)] public int damageMin = 45;
        [Min(0)] public int damageMax = 90;
        [Min(0f)] public float attackRange = 3.5f;

        public bool IsWeapon => weaponType != WeaponType.None;
        public float WeaponRange => attackRange;
        public int WeaponDamageMin => damageMin;
        public int WeaponDamageMax => damageMax;

        [Header("Random Affixes")]
        public RandomAffixPoolSO randomAffixPool;
        public RandomAffixRulesSO randomAffixRules;

        [Header("Ability Loadout Slots")]
        public AbilitySlotRulesSO abilitySlotRules;

        public bool IsEquipment => itemType == ItemType.Equipment;

        public float RollWeaponDamage()
        {
            return IsWeapon
                ? UnityEngine.Random.Range(WeaponDamageMin, WeaponDamageMax)
                : 0f;
        }

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