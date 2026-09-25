using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TTH.Combat.Attributes;
using TTH.Game.Inventory;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryView : UIView
{
	[SerializeField] private PlayerRuntime playerRuntime;
	[SerializeField] private Slider hpBar;
	[SerializeField] private Slider mpBar;
	[SerializeField] private TMP_Text hpText;
	[SerializeField] private TMP_Text mpText;
	[SerializeField] private TMP_Text atkText;
	[SerializeField] private TMP_Text defText;
	[SerializeField] private TMP_Text dexText;
	[SerializeField] private TMP_Text spdText;
	[SerializeField] private TMP_Text vitText;
	[SerializeField] private TMP_Text wisText;
	[SerializeField] private Transform unequippedPanel;
	[SerializeField] private TMP_Text capacityText;
	[SerializeField] private GameObject itemContextMenu;
	[SerializeField] private Button equipButton;
	[SerializeField] private Button consumeButton;
	[SerializeField] private Button unequipButton;
	[SerializeField] private Button deleteButton;
	[SerializeField] private Button removeButton;

	private bool subscribed;
	private readonly List<InventoryItemSlot> itemSlots = new();
	private readonly List<InventoryItemSlot> equippedSlots = new();
	private GameObject contextMenu;
	private InventoryItemSlot selectedSlot;
	private ItemInstance selectedItem;
	private string selectedEquipmentSlot;

	private void Awake()
	{
		if (playerRuntime == null)
			playerRuntime = FindFirstObjectByType<PlayerRuntime>();

		if (unequippedPanel == null)
			unequippedPanel = FindChild("UnEquipped Panel");
		if (capacityText == null)
			capacityText = CreateCapacityText();
		PrepareItemSlots();
		PrepareEquippedSlots();
		InitializeContextMenu();
	}

	public override void OnShown()
	{
		Subscribe();
		Refresh();
	}

	public override void OnHide()
	{
		Unsubscribe();
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	private void Update()
	{
		if (contextMenu == null || !contextMenu.activeSelf || selectedSlot == null || Mouse.current == null)
			return;

		var mouse = Mouse.current;
		if (!mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.wasPressedThisFrame)
			return;

		Vector2 pointerPosition = mouse.position.ReadValue();
		if (IsPointerInside(contextMenu.transform, pointerPosition) ||
			IsPointerInside(selectedSlot.transform, pointerPosition))
			return;

		CloseContextMenu();
	}

	private static bool IsPointerInside(Transform target, Vector2 pointerPosition)
	{
		var rectTransform = target as RectTransform;
		if (rectTransform == null)
			return false;

		var canvas = rectTransform.GetComponentInParent<Canvas>();
		Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
			? canvas.worldCamera
			: null;
		return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition, eventCamera);
	}

	private void Subscribe()
	{
		if (subscribed || playerRuntime == null || playerRuntime.Resources == null)
			return;

		playerRuntime.Resources.OnHPChanged += HandleResourceChanged;
		playerRuntime.Resources.OnMPChanged += HandleResourceChanged;
		if (playerRuntime.Attributes != null)
			playerRuntime.Attributes.OnDirty += HandleAttributeDirty;
			if (playerRuntime.Inventory != null)
			{
				playerRuntime.Inventory.ItemAdded += HandleInventoryChanged;
				playerRuntime.Inventory.ItemChanged += HandleInventoryChanged;
				playerRuntime.Inventory.ItemRemoved += HandleInventoryChanged;
				playerRuntime.Inventory.ItemEquipped += HandleInventoryChanged;
				playerRuntime.Inventory.ItemUnequipped += HandleInventoryChanged;
			}
		subscribed = true;
	}

	private void Unsubscribe()
	{
		if (!subscribed || playerRuntime == null)
			return;

		if (playerRuntime.Resources != null)
		{
			playerRuntime.Resources.OnHPChanged -= HandleResourceChanged;
			playerRuntime.Resources.OnMPChanged -= HandleResourceChanged;
		}

		if (playerRuntime.Attributes != null)
			playerRuntime.Attributes.OnDirty -= HandleAttributeDirty;
		if (playerRuntime.Inventory != null)
		{
			playerRuntime.Inventory.ItemAdded -= HandleInventoryChanged;
			playerRuntime.Inventory.ItemChanged -= HandleInventoryChanged;
			playerRuntime.Inventory.ItemRemoved -= HandleInventoryChanged;
			playerRuntime.Inventory.ItemEquipped -= HandleInventoryChanged;
			playerRuntime.Inventory.ItemUnequipped -= HandleInventoryChanged;
		}
		subscribed = false;
	}

	private void HandleResourceChanged(float before, float after)
	{
		Refresh();
	}

	private void HandleAttributeDirty(AttributeId attribute)
	{
		Refresh();
	}

	private void HandleInventoryChanged(ItemInstance item)
	{
		RefreshInventory();
		RefreshEquippedItems();
	}

	private void HandleInventoryChanged(ItemInstance item, string slotId)
	{
		RefreshInventory();
	}

	private void Refresh()
	{
		if (playerRuntime == null || playerRuntime.Attributes == null || playerRuntime.Resources == null)
			return;

		var attributes = playerRuntime.Attributes;
		var resources = playerRuntime.Resources;
		var maxHP = Mathf.Max(0f, attributes.Get(AttributeId.HP));
		var maxMP = Mathf.Max(0f, attributes.Get(AttributeId.MP));

		SetBar(hpBar, resources.CurrentHP, maxHP);
		SetBar(mpBar, resources.CurrentMP, maxMP);
		SetText(hpText, $"HP {resources.CurrentHP:0}/{maxHP:0}");
		SetText(mpText, $"MP {resources.CurrentMP:0}/{maxMP:0}");
		SetText(atkText, $"ATK {attributes.Get(AttributeId.ATK):0}");
		SetText(defText, $"DEF {attributes.Get(AttributeId.DEF):0}");
		SetText(dexText, $"DEX {attributes.Get(AttributeId.DEX):0}");
		SetText(spdText, $"SPD {attributes.Get(AttributeId.SPD):0}");
		SetText(vitText, $"VIT {attributes.Get(AttributeId.VIT):0}");
		SetText(wisText, $"WIS {attributes.Get(AttributeId.WIS):0}");
		RefreshInventory();
	}

	private void RefreshInventory()
	{
		if (playerRuntime?.Inventory == null)
			return;

		var items = playerRuntime.Inventory.GetItems(InventoryContainer.Bag);
		int capacity = playerRuntime.Inventory.GetCapacity(InventoryContainer.Bag);
		int used = playerRuntime.Inventory.GetUsedCapacity(InventoryContainer.Bag);
		SetText(capacityText, $"Bag {used}/{capacity}");

		for (int i = 0; i < itemSlots.Count; i++)
			itemSlots[i].Bind(i < items.Count ? items[i] : null);
		RefreshEquippedItems();
	}

	private void PrepareEquippedSlots()
	{
		var equippedPanel = FindChild("Equipped Items Panel");
		if (equippedPanel == null)
			return;

		foreach (var child in equippedPanel.GetComponentsInChildren<Transform>(true))
		{
			if (!child.name.StartsWith("Equipped Item Slot"))
				continue;

			var slot = child.GetComponent<InventoryItemSlot>();
			if (slot == null)
				slot = child.gameObject.AddComponent<InventoryItemSlot>();
			slot.Initialize(ShowContextMenu);
			equippedSlots.Add(slot);
		}
	}

	private void RefreshEquippedItems()
	{
		if (playerRuntime?.Inventory == null)
			return;

		int index = 0;
		foreach (var slotId in playerRuntime.Inventory.GetEquipmentSlotIds())
		{
			foreach (var item in playerRuntime.Inventory.GetEquipped(slotId))
			{
				if (index < equippedSlots.Count)
					equippedSlots[index++].Bind(item);
			}
		}

		while (index < equippedSlots.Count)
			equippedSlots[index++].Clear();
	}

	private void PrepareItemSlots()
	{
		if (unequippedPanel == null)
			return;

		var transforms = unequippedPanel.GetComponentsInChildren<Transform>(true);
		InventoryItemSlot template = null;
		foreach (var child in transforms)
		{
			if (!child.name.StartsWith("UnEquipped Item Slot"))
				continue;

			var slot = child.GetComponent<InventoryItemSlot>();
			if (slot == null)
				slot = child.gameObject.AddComponent<InventoryItemSlot>();
			slot.Initialize(ShowContextMenu);
			itemSlots.Add(slot);
			template ??= slot;
		}

		int capacity = playerRuntime?.Inventory?.GetCapacity(InventoryContainer.Bag) ?? 20;
		while (template != null && itemSlots.Count < capacity)
		{
			var clone = Instantiate(template.gameObject, template.transform.parent);
			clone.name = $"UnEquipped Item Slot ({itemSlots.Count + 1})";
			var slot = clone.GetComponent<InventoryItemSlot>();
			slot.Initialize(ShowContextMenu);
			itemSlots.Add(slot);
		}
	}

	private Transform FindChild(string childName)
	{
		foreach (var child in GetComponentsInChildren<Transform>(true))
		{
			if (child.name == childName)
				return child;
		}
		return null;
	}

	private TMP_Text CreateCapacityText()
	{
		var textObject = new GameObject("Bag Capacity", typeof(RectTransform));
		textObject.transform.SetParent(transform, false);
		var text = textObject.AddComponent<TextMeshProUGUI>();
		text.alignment = TextAlignmentOptions.TopRight;
		text.fontSize = 22f;
		text.color = Color.white;
		text.raycastTarget = false;
		var rect = text.rectTransform;
		rect.anchorMin = new Vector2(0.6f, 0.94f);
		rect.anchorMax = new Vector2(0.96f, 0.99f);
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
		return text;
	}

	private void InitializeContextMenu()
	{
		if (itemContextMenu != null)
			contextMenu = itemContextMenu;

		if (contextMenu == null)
			contextMenu = FindChild("Item Context Menu")?.gameObject;
		if (contextMenu == null)
			return;

		if (equipButton == null)
			equipButton = FindButton(contextMenu.transform, "Equip Btn");
		if (consumeButton == null)
			consumeButton = FindButton(contextMenu.transform, "Consume Btn");
		if (unequipButton == null)
			unequipButton = FindButton(contextMenu.transform, "Unequip Btn");
		if (deleteButton == null)
			deleteButton = FindButton(contextMenu.transform, "Delete Btn");
		if (removeButton == null)
			removeButton = FindButton(contextMenu.transform, "Remove Btn");

		BindButton(equipButton, EquipSelected);
		BindButton(consumeButton, ConsumeSelected);
		BindButton(unequipButton, UnequipSelected);
		BindButton(deleteButton, DeleteSelected);
		BindButton(removeButton, RemoveSelected);

		var layout = contextMenu.GetComponent<VerticalLayoutGroup>();
		if (layout == null)
			layout = contextMenu.AddComponent<VerticalLayoutGroup>();
		layout.childAlignment = TextAnchor.MiddleCenter;
		layout.childControlWidth = false;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;

		var sizeFitter = contextMenu.GetComponent<ContentSizeFitter>();
		if (sizeFitter == null)
			sizeFitter = contextMenu.AddComponent<ContentSizeFitter>();
		sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		CloseContextMenu();
	}

	private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
	{
		if (button == null)
			return;

		button.onClick.RemoveListener(action);
		button.onClick.AddListener(action);
	}

	private static Button FindButton(Transform root, string objectName)
	{
		foreach (var button in root.GetComponentsInChildren<Button>(true))
		{
			if (button.name == objectName)
				return button;
		}
		return null;
	}

	private void ShowContextMenu(InventoryItemSlot slot)
	{
		if (slot == null || slot.Item?.Definition == null || contextMenu == null)
			return;

		selectedSlot = slot;
		selectedItem = slot.Item;
		selectedEquipmentSlot = FindEquippedSlot(slot);

		SetButtonActive(equipButton, false);
		SetButtonActive(consumeButton, false);
		SetButtonActive(unequipButton, false);
		SetButtonActive(deleteButton, false);
		SetButtonActive(removeButton, false);

		var visibleButtons = new List<Button>();
		if (!string.IsNullOrEmpty(selectedEquipmentSlot))
		{
			AddVisibleButton(visibleButtons, unequipButton);
		}
		else
		{
			bool isConsumable = selectedItem.Definition.itemType == ItemType.Consumable;
			// TODO: Replace this fallback with item-class-specific actions once item classes are defined.
			AddVisibleButton(visibleButtons, isConsumable ? consumeButton : equipButton);
			AddVisibleButton(visibleButtons, selectedItem.Quantity == 1 ? deleteButton : removeButton);
		}

		for (int i = 0; i < visibleButtons.Count; i++)
			visibleButtons[i].transform.SetSiblingIndex(i);

		if (Mouse.current != null)
			contextMenu.transform.position = Mouse.current.position.ReadValue();
		contextMenu.SetActive(visibleButtons.Count > 0);
		if (contextMenu.transform is RectTransform menuRect)
			LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
	}

	private static void AddVisibleButton(List<Button> visibleButtons, Button button)
	{
		if (button == null)
			return;

		button.gameObject.SetActive(true);
		visibleButtons.Add(button);
	}

	private static void SetButtonActive(Button button, bool active)
	{
		if (button != null)
			button.gameObject.SetActive(active);
	}

	private string FindEquippedSlot(InventoryItemSlot slot)
	{
		if (!equippedSlots.Contains(slot) || playerRuntime?.Inventory == null)
			return string.Empty;

		foreach (var slotId in playerRuntime.Inventory.GetEquipmentSlotIds())
		{
			foreach (var item in playerRuntime.Inventory.GetEquipped(slotId))
			{
				if (item == slot.Item)
					return slotId;
			}
		}
		return string.Empty;
	}

	private void EquipSelected()
	{
		if (selectedItem == null)
			return;
		var result = playerRuntime.EquipItem(selectedItem.InstanceId);
		if (!result.Success)
			Debug.LogWarning(result.Message);
		CloseContextMenu();
	}

	private void ConsumeSelected()
	{
		if (selectedItem == null)
			return;
		var result = playerRuntime.ConsumeItem(selectedItem.InstanceId);
		if (!result.Success)
			Debug.LogWarning(result.Message);
		CloseContextMenu();
	}

	private void UnequipSelected()
	{
		if (selectedItem == null || string.IsNullOrEmpty(selectedEquipmentSlot))
			return;

		var result = playerRuntime.UnequipItem(selectedItem.InstanceId, selectedEquipmentSlot);
		if (!result.Success)
			Debug.LogWarning(result.Message);
		CloseContextMenu();
	}
	
	private void RemoveSelected()
	{
		if (selectedItem == null || !string.IsNullOrEmpty(selectedEquipmentSlot))
			return;

		var result = playerRuntime.RemoveItem(selectedItem.InstanceId);
		if (!result.Success)
			Debug.LogWarning(result.Message);
		CloseContextMenu();
	}

	private void DeleteSelected()
	{
		if (selectedItem == null || !string.IsNullOrEmpty(selectedEquipmentSlot))
			return;

		var result = playerRuntime.DeleteItem(selectedItem.InstanceId);
		if (!result.Success)
			Debug.LogWarning(result.Message);
		CloseContextMenu();
	}

	private void CloseContextMenu()
	{
		selectedSlot = null;
		selectedItem = null;
		selectedEquipmentSlot = null;
		SetButtonActive(equipButton, false);
		SetButtonActive(consumeButton, false);
		SetButtonActive(unequipButton, false);
		SetButtonActive(deleteButton, false);
		SetButtonActive(removeButton, false);
		if (contextMenu != null)
			contextMenu.SetActive(false);
	}

	private static void SetBar(Slider bar, float current, float max)
	{
		if (bar == null)
			return;

		bar.minValue = 0f;
		bar.maxValue = Mathf.Max(1f, max);
		bar.SetValueWithoutNotify(Mathf.Clamp(current, 0f, bar.maxValue));
	}

	private static void SetText(TMP_Text text, string value)
	{
		if (text != null)
			text.text = value;
	}
}
