using System;
using System.Collections.Generic;
using UnityEngine;

namespace TTH.Game.Inventory
{
    public enum InventoryContainer
    {
        Bag,
        Equipment,
        Quest,
        Storage
    }

    public enum InventoryFailure
    {
        None,
        InvalidItem,
        InvalidQuantity,
        InventoryFull,
        ItemNotFound,
        NotEnoughQuantity,
        InvalidContainer,
        InvalidSlot,
        WrongItemType,
        SlotTagMismatch,
        AlreadyEquipped
    }

    public readonly struct InventoryResult
    {
        public bool Success { get; }
        public InventoryFailure Failure { get; }
        public string Message { get; }

        private InventoryResult(bool success, InventoryFailure failure, string message)
        {
            Success = success;
            Failure = failure;
            Message = message;
        }

        public static InventoryResult Ok() => new(true, InventoryFailure.None, string.Empty);

        public static InventoryResult Fail(InventoryFailure failure, string message)
            => new(false, failure, message);
    }

    [Serializable]
    public sealed class ItemInstance
    {
        [SerializeField] private string instanceId;
        [SerializeField] private ItemDefinitionSO definition;
        [SerializeField] private int quantity = 1;
        [SerializeField] private int level;
        [SerializeField] private string rolledStateKey;
        [SerializeField] private float durability;
        [SerializeField] private bool isEquipped;
        [SerializeField] private bool isBound;
        [SerializeField] private string acquiredAt;
        [SerializeField] private int rollSeed;
        [SerializeField] private List<RolledAffix> randomAffixes = new();
        [SerializeField] private ItemRarity rolledRarity;

        public string InstanceId => instanceId;
        public ItemDefinitionSO Definition => definition;
        public int Quantity => quantity;
        public int Level => level;
        public string RolledStateKey => rolledStateKey;
        public float Durability => durability;
        public bool IsEquipped => isEquipped;
        public bool IsBound => isBound;
        public string AcquiredAt => acquiredAt;
        public int RollSeed => rollSeed;
        public IReadOnlyList<RolledAffix> RandomAffixes => randomAffixes;
        public ItemRarity RolledRarity => rolledRarity;

        public ItemInstance(ItemDefinitionSO definition, int quantity = 1, string rolledStateKey = "", bool isBound = false)
            : this(definition, quantity, rolledStateKey, isBound, null, 0)
        {
        }

        public ItemInstance(ItemDefinitionSO definition, int quantity, string rolledStateKey, bool isBound,
            IEnumerable<RolledAffix> randomAffixes, int rollSeed, ItemRarity rolledRarity = ItemRarity.Common)
        {
            instanceId = Guid.NewGuid().ToString("N");
            this.definition = definition;
            this.quantity = quantity;
            this.rolledStateKey = rolledStateKey ?? string.Empty;
            this.isBound = isBound;
            acquiredAt = DateTime.UtcNow.ToString("O");
            this.rollSeed = rollSeed;
            this.rolledRarity = rolledRarity;
            if (randomAffixes != null)
                this.randomAffixes = new List<RolledAffix>(randomAffixes);
        }

        internal ItemInstance(string instanceId, ItemDefinitionSO definition, int quantity, int level,
            string rolledStateKey, float durability, bool isEquipped, bool isBound, string acquiredAt,
            int rollSeed, IEnumerable<RolledAffix> randomAffixes, ItemRarity rolledRarity)
        {
            this.instanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
            this.definition = definition;
            this.quantity = quantity;
            this.level = level;
            this.rolledStateKey = rolledStateKey ?? string.Empty;
            this.durability = durability;
            this.isEquipped = isEquipped;
            this.isBound = isBound;
            this.acquiredAt = acquiredAt ?? string.Empty;
            this.rollSeed = rollSeed;
            this.rolledRarity = rolledRarity;
            this.randomAffixes = randomAffixes == null
                ? new List<RolledAffix>()
                : new List<RolledAffix>(randomAffixes);
        }

        internal ItemInstanceSaveData CreateSaveData(InventoryContainer container, string slotId = "")
        {
            return new ItemInstanceSaveData
            {
                instanceId = instanceId,
                itemId = definition == null ? string.Empty : definition.itemId,
                quantity = quantity,
                level = level,
                rolledStateKey = rolledStateKey,
                durability = durability,
                isEquipped = isEquipped,
                isBound = isBound,
                acquiredAt = acquiredAt,
                rollSeed = rollSeed,
                randomAffixes = new List<RolledAffix>(randomAffixes),
                rolledRarity = rolledRarity,
                container = container,
                slotId = slotId
            };
        }

        public bool CanStackWith(ItemInstance other)
        {
            return other != null && definition == other.definition &&
                   string.Equals(rolledStateKey, other.rolledStateKey, StringComparison.Ordinal) &&
                   isBound == other.isBound && definition != null && definition.maxStack > 1;
        }

        internal int AddQuantity(int amount)
        {
            int added = Mathf.Clamp(amount, 0, definition.maxStack - quantity);
            quantity += added;
            return added;
        }

        internal bool RemoveQuantity(int amount)
        {
            if (amount < 1 || quantity < amount) return false;
            quantity -= amount;
            return true;
        }

        internal void SetEquipped(bool value) => isEquipped = value;
        internal void SetDurability(float value) => durability = value;
    }

    [Serializable]
    public sealed class ItemInstanceSaveData
    {
        public string instanceId;
        public string itemId;
        public int quantity;
        public int level;
        public string rolledStateKey;
        public float durability;
        public bool isEquipped;
        public bool isBound;
        public string acquiredAt;
        public int rollSeed;
        public List<RolledAffix> randomAffixes = new();
        public ItemRarity rolledRarity;
        public InventoryContainer container;
        public string slotId;
    }

    [Serializable]
    public sealed class EquipmentSlotDefinition
    {
        public string slotId;
        public string[] acceptedTags;
        [Min(1)] public int maxCount = 1;

        public bool Accepts(ItemDefinitionSO definition)
        {
            if (definition == null || acceptedTags == null || acceptedTags.Length == 0) return false;
            for (int i = 0; i < acceptedTags.Length; i++)
            {
                if (definition.HasTag(acceptedTags[i])) return true;
            }
            return false;
        }
    }
}