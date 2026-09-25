using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TTH.Game.Inventory
{
    [Serializable]
    public sealed class InventorySaveData
    {
        public int version = 2;
        public List<ItemInstanceSaveData> items = new();
    }

    public static class InventorySaveSystem
    {
        public const string UnsupportedVersionError = "Inventory save version is outdated.";

        public static string GetDefaultPath(string fileName = "inventory.json")
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public static string Save(InventoryRuntime inventory)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            var data = new InventorySaveData();
            foreach (InventoryContainer container in Enum.GetValues(typeof(InventoryContainer)))
            {
                foreach (var item in inventory.GetItems(container))
                    data.items.Add(item.CreateSaveData(container));
            }

            foreach (var slot in inventory.GetEquipmentSlotIds())
            {
                foreach (var item in inventory.GetEquipped(slot))
                    data.items.Add(item.CreateSaveData(InventoryContainer.Equipment, slot));
            }

            return JsonUtility.ToJson(data, true);
        }

        public static void SaveToFile(InventoryRuntime inventory, string path = null)
        {
            File.WriteAllText(path ?? GetDefaultPath(), Save(inventory));
        }

        public static bool LoadFromFile(string path, InventoryRuntime inventory,
            Func<string, ItemDefinitionSO> resolveDefinition, out string error)
        {
            error = string.Empty;
            if (!File.Exists(path))
            {
                error = $"Inventory save file was not found: {path}";
                return false;
            }

            return Load(File.ReadAllText(path), inventory, resolveDefinition, out error);
        }

        public static bool Load(string json, InventoryRuntime inventory,
            Func<string, ItemDefinitionSO> resolveDefinition, out string error)
        {
            error = string.Empty;
            if (inventory == null)
            {
                error = "Inventory runtime is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Inventory save data is empty.";
                return false;
            }

            if (resolveDefinition == null)
            {
                error = "Item definition resolver is missing.";
                return false;
            }

            InventorySaveData data;
            try
            {
                data = JsonUtility.FromJson<InventorySaveData>(json);
            }
            catch (Exception exception)
            {
                error = $"Inventory save data is invalid: {exception.Message}";
                return false;
            }

            if (data == null || data.items == null)
            {
                error = "Inventory save data has no item list.";
                return false;
            }

            if (data.version < 2)
            {
                error = UnsupportedVersionError;
                return false;
            }

            var restored = new List<RestoredItem>();
            foreach (var savedItem in data.items)
            {
                if (savedItem == null || string.IsNullOrEmpty(savedItem.itemId) || savedItem.quantity < 1)
                {
                    error = "Inventory save data contains an invalid item.";
                    return false;
                }

                var definition = resolveDefinition(savedItem.itemId);
                if (definition == null)
                {
                    error = $"Item definition not found: {savedItem.itemId}";
                    return false;
                }

                var item = new ItemInstance(
                    savedItem.instanceId,
                    definition,
                    savedItem.quantity,
                    savedItem.level,
                    savedItem.rolledStateKey,
                    savedItem.durability,
                    savedItem.isEquipped,
                    savedItem.isBound,
                    savedItem.acquiredAt,
                    savedItem.rollSeed,
                    savedItem.randomAffixes,
                    savedItem.rolledRarity);
                restored.Add(new RestoredItem(item, savedItem.container, savedItem.slotId));
            }

            inventory.Clear();
            foreach (var restoredItem in restored)
            {
                InventoryResult result = restoredItem.Container == InventoryContainer.Equipment
                    ? inventory.RestoreEquipped(restoredItem.Item, restoredItem.SlotId)
                    : inventory.Restore(restoredItem.Item, restoredItem.Container);

                if (!result.Success)
                {
                    error = result.Message;
                    inventory.Clear();
                    return false;
                }
            }

            return true;
        }

        private readonly struct RestoredItem
        {
            public readonly ItemInstance Item;
            public readonly InventoryContainer Container;
            public readonly string SlotId;

            public RestoredItem(ItemInstance item, InventoryContainer container, string slotId)
            {
                Item = item;
                Container = container;
                SlotId = slotId;
            }
        }
    }
}