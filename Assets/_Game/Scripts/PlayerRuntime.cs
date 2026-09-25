using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Runtime;
using TTH.Combat.Status;
using TTH.Game.Inventory;

public class PlayerRuntime : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO characterStats;
    [SerializeField] private ItemDefinitionSO defaultRustySword;
    [SerializeField] private ItemDefinitionDatabaseSO itemDatabase;
    [SerializeField] private string inventorySaveFileName = "inventory.json";

    public AttributeSystem Attributes => characterAttributes;
    public CombatEntity Entity => characterEntity;
    public ResourcePool Resources => characterResources;
    public StatusSystem Statuses => characterStatus;
    public InventoryRuntime Inventory => inventory;

    private AttributeSystem characterAttributes;
    private CombatEntity characterEntity;
    private ResourcePool characterResources;
    private StatusSystem characterStatus;
    private InventoryRuntime inventory;
    private EquipmentModifierBinder equipmentBinder;
    private AffixModifierBridge affixBinder;

    private StatBlock characterStatBlock; 

    private void Awake()
    {
        BuildRuntime();
    }

    private void BuildRuntime()
    {
        if (characterStats == null)
        {
            Debug.LogError("Assign Character Stats in the Inspector.");
            return;
        }

        characterStatBlock = new StatBlock();
        
        characterStatBlock.SetBase(AttributeId.ATK, characterStats.attack);
        characterStatBlock.SetBase(AttributeId.DEF, characterStats.defense);
        characterStatBlock.SetBase(AttributeId.DEX, characterStats.dexterity);
        characterStatBlock.SetBase(AttributeId.SPD, characterStats.speed);
        characterStatBlock.SetBase(AttributeId.VIT, characterStats.vitality);
        characterStatBlock.SetBase(AttributeId.WIS, characterStats.wisdom);
        characterStatBlock.SetBase(AttributeId.HP, characterStats.hitPoints);
        characterStatBlock.SetBase(AttributeId.MP, characterStats.magicPoints);

        characterAttributes = new AttributeSystem(characterStatBlock, null);
        characterResources = new ResourcePool(characterStats.hitPoints, characterStats.magicPoints);
        characterStatus = new StatusSystem();
        characterEntity = new CombatEntity(1, characterAttributes, characterStatus, null, characterResources);
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        var weaponSlot = new EquipmentSlotDefinition
        {
            slotId = "Weapon",
            acceptedTags = new[] { "Weapon" },
            maxCount = 1
        };
        inventory = new InventoryRuntime(new[] { weaponSlot });
        equipmentBinder = new EquipmentModifierBinder(characterAttributes);
        affixBinder = new AffixModifierBridge(characterAttributes);
        inventory.ItemEquipped += HandleItemEquipped;
        inventory.ItemUnequipped += HandleItemUnequipped;

        string savePath = InventorySaveSystem.GetDefaultPath(inventorySaveFileName);
        if (File.Exists(savePath))
        {
            if (itemDatabase == null)
            {
                Debug.LogError("Item definition database is required to load inventory.", this);
                return;
            }

            if (InventorySaveSystem.LoadFromFile(savePath, inventory, itemDatabase.Find, out string loadError))
            {
                BindLoadedEquipment();
                return;
            }

            if (loadError == InventorySaveSystem.UnsupportedVersionError)
            {
                Debug.LogWarning("Legacy inventory save detected. Creating the updated default inventory.", this);
                File.Delete(savePath);
                CreateDefaultInventory();
                InventorySaveSystem.SaveToFile(inventory, savePath);
                return;
            }

            Debug.LogError($"Could not load inventory save: {loadError}", this);
            return;
        }

        CreateDefaultInventory();
        InventorySaveSystem.SaveToFile(inventory, savePath);
    }

    public InventoryResult EquipItem(string instanceId)
    {
        var item = inventory?.Find(instanceId, InventoryContainer.Bag);
        if (item == null)
            return InventoryResult.Fail(InventoryFailure.ItemNotFound, "Item instance was not found.");

        var result = inventory.TryEquip(instanceId, item.Definition.equipmentSlotId);
        if (result.Success)
            SaveInventory();
        return result;
    }

    public InventoryResult ConsumeItem(string instanceId)
    {
        var item = inventory?.Find(instanceId, InventoryContainer.Bag);
        if (item?.Definition == null || item.Definition.itemType != ItemType.Consumable)
            return InventoryResult.Fail(InventoryFailure.WrongItemType, "This item cannot be consumed.");

        switch (item.Definition.consumeResource)
        {
            case ConsumableResource.HP:
                characterResources.ApplyHPDelta(item.Definition.consumeAmount);
                break;
            case ConsumableResource.MP:
                characterResources.ApplyMPDelta(item.Definition.consumeAmount);
                break;
            default:
                return InventoryResult.Fail(InventoryFailure.WrongItemType, "This item has no consume effect.");
        }

        var result = inventory.TryRemove(instanceId, 1);
        if (result.Success)
            SaveInventory();
        return result;
    }

    public InventoryResult UnequipItem(string instanceId, string slotId)
    {
        var result = inventory == null
            ? InventoryResult.Fail(InventoryFailure.InvalidContainer, "Inventory runtime is missing.")
            : inventory.TryUnequip(instanceId, slotId);
        if (result.Success)
            SaveInventory();
        return result;
    }
    
    public InventoryResult RemoveItem(string instanceId)
    {
        var result = inventory == null
            ? InventoryResult.Fail(InventoryFailure.InvalidContainer, "Inventory runtime is missing.")
            : inventory.TryRemove(instanceId, 1);
        if (result.Success)
            SaveInventory();
        return result;
    }
    
    public InventoryResult DeleteItem(string instanceId)
    {
        var item = inventory?.Find(instanceId, InventoryContainer.Bag);
        if (item == null)
            return InventoryResult.Fail(InventoryFailure.ItemNotFound, "Item instance was not found.");
        
        var result = inventory.TryRemove(instanceId, item.Quantity);
        if (result.Success)
            SaveInventory();
        return result;
    }

    private void HandleItemEquipped(ItemInstance item, string slotId)
    {
        equipmentBinder.Equip(item);
        affixBinder.Equip(item);
    }

    private void HandleItemUnequipped(ItemInstance item, string slotId)
    {
        equipmentBinder.Unequip(item);
        affixBinder.Unequip(item);
    }

    private void BindLoadedEquipment()
    {
        foreach (var slotId in inventory.GetEquipmentSlotIds())
        {
            foreach (var item in inventory.GetEquipped(slotId))
            {
                equipmentBinder.Equip(item);
                affixBinder.Equip(item);
            }
        }
    }

    private void SaveInventory()
    {
        InventorySaveSystem.SaveToFile(inventory, InventorySaveSystem.GetDefaultPath(inventorySaveFileName));
    }

    [ContextMenu("Debug/Add Rusty Sword To Bag")]
    private void AddRustySwordToBagForTesting()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before adding a test Rusty Sword.", this);
            return;
        }

        if (inventory == null || defaultRustySword == null)
        {
            Debug.LogWarning("Player inventory or default Rusty Sword is not initialized.", this);
            return;
        }

        InventoryResult result = inventory.TryAdd(CreateDefaultRustySword());
        if (!result.Success)
        {
            Debug.LogWarning($"Could not add test Rusty Sword: {result.Message}", this);
            return;
        }

        SaveInventory();
        Debug.Log("Added a Rusty Sword to the Bag and saved the inventory.", this);
    }

    private void CreateDefaultInventory()
    {
        if (defaultRustySword == null)
        {
            Debug.LogWarning("Assign the default Rusty Sword item definition.", this);
            return;
        }

        var result = inventory.TryAdd(CreateDefaultRustySword());
        if (!result.Success)
            Debug.LogError($"Could not add default Rusty Sword: {result.Message}", this);
    }

    private ItemInstance CreateDefaultRustySword()
    {
        var dexAffix = new RolledAffix
        {
            affixId = "dex",
            affixFamilyId = "dex",
            displayName = "Dexterity",
            slotType = AffixSlotType.Suffix,
            tierId = "T2",
            value = 10f,
            isPercent = false,
            effectType = AffixEffectType.Attribute,
            attribute = AttributeId.DEX,
            operation = ModifierOp.Add,
            gameplayModifier = GameplayModifierType.none
        };

        var rustySword = new ItemInstance(
            defaultRustySword,
            1,
            "dex_T2",
            false,
            new List<RolledAffix> { dexAffix },
            0,
            ItemRarity.Common);
        return rustySword;
    }
}
