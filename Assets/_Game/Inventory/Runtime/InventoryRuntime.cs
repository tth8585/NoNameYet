using System;
using System.Collections.Generic;
using System.Linq;

namespace TTH.Game.Inventory
{
    public sealed class InventoryRuntime
    {
        private readonly Dictionary<InventoryContainer, List<ItemInstance>> items = new();
        private readonly Dictionary<string, List<ItemInstance>> equipped = new();
        private readonly Dictionary<string, EquipmentSlotDefinition> slotDefinitions = new(StringComparer.Ordinal);
        private readonly Dictionary<InventoryContainer, int> capacities = new();

        public event Action<ItemInstance> ItemAdded;
        public event Action<ItemInstance> ItemChanged;
        public event Action<ItemInstance> ItemRemoved;
        public event Action<ItemInstance, string> ItemEquipped;
        public event Action<ItemInstance, string> ItemUnequipped;

        public InventoryRuntime(IEnumerable<EquipmentSlotDefinition> slots = null,
            IReadOnlyDictionary<InventoryContainer, int> containerCapacities = null)
        {
            foreach (InventoryContainer container in Enum.GetValues(typeof(InventoryContainer)))
            {
                items[container] = new List<ItemInstance>();
                capacities[container] = container == InventoryContainer.Bag ? 20 : 100;
            }

            capacities[InventoryContainer.Equipment] = 0;

            if (containerCapacities != null)
            {
                foreach (var capacity in containerCapacities)
                {
                    if (capacities.ContainsKey(capacity.Key) && capacity.Value >= 0)
                        capacities[capacity.Key] = capacity.Value;
                }
            }

            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot != null && !string.IsNullOrEmpty(slot.slotId))
                    slotDefinitions[slot.slotId] = slot;
            }
        }

        public IReadOnlyList<ItemInstance> GetItems(InventoryContainer container) => items[container];

        public IReadOnlyList<ItemInstance> GetEquipped(string slotId)
        {
            return equipped.TryGetValue(slotId, out var result) ? result : Array.Empty<ItemInstance>();
        }

        public IEnumerable<string> GetEquipmentSlotIds() => slotDefinitions.Keys;

        public void Clear()
        {
            foreach (var list in items.Values)
                list.Clear();
            equipped.Clear();
        }

        internal InventoryResult Restore(ItemInstance instance, InventoryContainer container)
        {
            if (instance == null || instance.Definition == null || instance.Quantity < 1)
                return InventoryResult.Fail(InventoryFailure.InvalidItem, "Saved item is invalid.");
            if (!items.ContainsKey(container) || container == InventoryContainer.Equipment)
                return InventoryResult.Fail(InventoryFailure.InvalidContainer, "Saved item container is invalid.");
            if (items[container].Count >= capacities[container])
                return InventoryResult.Fail(InventoryFailure.InventoryFull, "Saved inventory exceeds capacity.");

            items[container].Add(instance);
            return InventoryResult.Ok();
        }

        internal InventoryResult RestoreEquipped(ItemInstance instance, string slotId)
        {
            if (instance == null || instance.Definition == null || string.IsNullOrEmpty(slotId))
                return InventoryResult.Fail(InventoryFailure.InvalidItem, "Saved equipment is invalid.");
            if (!slotDefinitions.TryGetValue(slotId, out var slot))
                return InventoryResult.Fail(InventoryFailure.InvalidSlot, "Saved equipment slot is not configured.");
            if (!slot.Accepts(instance.Definition))
                return InventoryResult.Fail(InventoryFailure.SlotTagMismatch, "Saved equipment does not match its slot.");
            if (!equipped.TryGetValue(slotId, out var slotItems))
                equipped[slotId] = slotItems = new List<ItemInstance>();
            if (slotItems.Count >= slot.maxCount)
                return InventoryResult.Fail(InventoryFailure.AlreadyEquipped, "Saved equipment slot is full.");

            slotItems.Add(instance);
            instance.SetEquipped(true);
            return InventoryResult.Ok();
        }

        public InventoryResult TryAdd(ItemInstance instance, InventoryContainer container = InventoryContainer.Bag)
        {
            if (instance == null || instance.Definition == null)
                return InventoryResult.Fail(InventoryFailure.InvalidItem, "Item definition is missing.");
            if (instance.Quantity < 1)
                return InventoryResult.Fail(InventoryFailure.InvalidQuantity, "Quantity must be positive.");
            if (!items.ContainsKey(container) || container == InventoryContainer.Equipment)
                return InventoryResult.Fail(InventoryFailure.InvalidContainer, "Items cannot be added to this container directly.");
            if (!CanAdd(instance.Definition, instance.Quantity, container, instance.RolledStateKey, instance.IsBound))
                return InventoryResult.Fail(InventoryFailure.InventoryFull, "There is not enough inventory capacity.");

            int remaining = instance.Quantity;
            foreach (var existing in items[container].Where(item => item.CanStackWith(instance)))
            {
                int added = existing.AddQuantity(remaining);
                remaining -= added;
                if (remaining == 0) return Added(instance);
            }

            while (remaining > 0)
            {
                int amount = Math.Min(remaining, instance.Definition.maxStack);
                var stack = remaining == instance.Quantity && amount == instance.Quantity
                    ? instance
                    : new ItemInstance(instance.Definition, amount, instance.RolledStateKey, instance.IsBound);
                items[container].Add(stack);
                remaining -= amount;
                ItemAdded?.Invoke(stack);
            }

            return InventoryResult.Ok();
        }

        public InventoryResult TryRemove(string instanceId, int quantity, InventoryContainer container = InventoryContainer.Bag)
        {
            if (quantity < 1)
                return InventoryResult.Fail(InventoryFailure.InvalidQuantity, "Quantity must be positive.");
            var instance = Find(instanceId, container);
            if (instance == null)
                return InventoryResult.Fail(InventoryFailure.ItemNotFound, "Item instance was not found.");
            if (!instance.RemoveQuantity(quantity))
                return InventoryResult.Fail(InventoryFailure.NotEnoughQuantity, "Not enough quantity in the item stack.");

            if (instance.Quantity == 0)
            {
                items[container].Remove(instance);
                ItemRemoved?.Invoke(instance);
            }
            else
            {
                ItemChanged?.Invoke(instance);
            }
            return InventoryResult.Ok();
        }

        public InventoryResult TryEquip(string instanceId, string slotId)
        {
            var instance = Find(instanceId, InventoryContainer.Bag);
            if (instance == null)
                return InventoryResult.Fail(InventoryFailure.ItemNotFound, "Item instance was not found in the bag.");
            if (!instance.Definition.IsEquipment)
                return InventoryResult.Fail(InventoryFailure.WrongItemType, "Only equipment can be equipped.");
            if (!slotDefinitions.TryGetValue(slotId, out var slot))
                return InventoryResult.Fail(InventoryFailure.InvalidSlot, "Equipment slot is not configured.");
            if (!slot.Accepts(instance.Definition))
                return InventoryResult.Fail(InventoryFailure.SlotTagMismatch, "Item tags do not match this slot.");
            if (!equipped.TryGetValue(slotId, out var slotItems))
                equipped[slotId] = slotItems = new List<ItemInstance>();
            if (slotItems.Count >= slot.maxCount)
                return InventoryResult.Fail(InventoryFailure.AlreadyEquipped, "Equipment slot is full.");

            items[InventoryContainer.Bag].Remove(instance);
            slotItems.Add(instance);
            instance.SetEquipped(true);
            ItemEquipped?.Invoke(instance, slotId);
            return InventoryResult.Ok();
        }

        public InventoryResult TryUnequip(string instanceId, string slotId, InventoryContainer destination = InventoryContainer.Bag)
        {
            if (!equipped.TryGetValue(slotId, out var slotItems))
                return InventoryResult.Fail(InventoryFailure.ItemNotFound, "Equipment slot is empty.");
            var instance = slotItems.FirstOrDefault(item => item.InstanceId == instanceId);
            if (instance == null)
                return InventoryResult.Fail(InventoryFailure.ItemNotFound, "Item instance was not found in this slot.");

            var addResult = TryAdd(instance, destination);
            if (!addResult.Success) return addResult;
            slotItems.Remove(instance);
            instance.SetEquipped(false);
            ItemUnequipped?.Invoke(instance, slotId);
            return InventoryResult.Ok();
        }

        public ItemInstance Find(string instanceId, InventoryContainer container)
        {
            return items.TryGetValue(container, out var list)
                ? list.FirstOrDefault(item => item.InstanceId == instanceId)
                : null;
        }

        public IEnumerable<ItemInstance> FindByDefinition(ItemDefinitionSO definition)
        {
            return items.Values.SelectMany(list => list).Where(item => item.Definition == definition);
        }

        public bool CanAdd(ItemDefinitionSO definition, int quantity, InventoryContainer container = InventoryContainer.Bag)
        {
            return CanAdd(definition, quantity, container, string.Empty, false);
        }

        public int GetCapacity(InventoryContainer container)
        {
            return capacities.TryGetValue(container, out var capacity) ? capacity : 0;
        }

        public int GetUsedCapacity(InventoryContainer container)
        {
            return items.TryGetValue(container, out var list) ? list.Count : 0;
        }

        private bool CanAdd(ItemDefinitionSO definition, int quantity, InventoryContainer container,
            string rolledStateKey, bool isBound)
        {
            if (definition == null || quantity < 1 || container == InventoryContainer.Equipment || !items.ContainsKey(container))
                return false;

            var probe = new ItemInstance(definition, 1, rolledStateKey, isBound);
            int remaining = quantity;
            foreach (var existing in items[container].Where(item => item.CanStackWith(probe)))
            {
                remaining -= definition.maxStack - existing.Quantity;
                if (remaining <= 0) return true;
            }

            int requiredStacks = (remaining + definition.maxStack - 1) / definition.maxStack;
            return items[container].Count + requiredStacks <= capacities[container];
        }

        private InventoryResult Added(ItemInstance instance)
        {
            ItemAdded?.Invoke(instance);
            return InventoryResult.Ok();
        }
    }
}