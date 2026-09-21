using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TTH.Game.Inventory;

namespace TTH.Game.Inventory.Tests
{
    public sealed class InventoryAffixTestRunner : MonoBehaviour
    {
        [SerializeField] public ItemDefinitionSO rustySword;

        private readonly ItemRollService rollService = new();
        private InventoryRuntime inventory;
        private ItemInstance createdItem;
        private string status = "Press Create Item to roll a Rusty Sword.";
        private Text output;

        private void Awake()
        {
            inventory = new InventoryRuntime();
            CreateUi();
        }

        private void CreateUi()
        {
            var canvasObject = new GameObject("InventoryAffixTestCanvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = CreateUiObject("Panel", canvas.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 520f);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);

            var title = CreateText("Title", panel.transform, "Inventory Affix Test", 26, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f));

            var buttonObject = CreateUiObject("CreateItemButton", panel.transform);
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(0.25f, 0.72f), new Vector2(0.75f, 0.84f));
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.48f, 0.8f, 1f);
            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(CreateItem);
            var buttonText = CreateText("Label", buttonObject.transform, "Create Item", 22, TextAnchor.MiddleCenter);
            SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one);

            output = CreateText("Output", panel.transform, status, 18, TextAnchor.UpperLeft);
            SetRect(output.rectTransform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.67f));
            output.horizontalOverflow = HorizontalWrapMode.Wrap;
            output.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            return result;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
        {
            var textObject = CreateUiObject(name, parent);
            var text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void CreateItem()
        {
            if (rustySword == null)
            {
                status = "ERROR: Item_rusty_sword asset is not assigned.";
                createdItem = null;
                RefreshOutput();
                return;
            }

            ItemRollResult roll = rollService.Roll(rustySword);
            if (!roll.Success)
            {
                status = $"ROLL FAILED: {roll.Error}";
                createdItem = null;
                RefreshOutput();
                return;
            }

            InventoryResult addResult = inventory.TryAdd(roll.Item);
            if (!addResult.Success)
            {
                status = $"INVENTORY FAILED: {addResult.Message}";
                createdItem = null;
                RefreshOutput();
                return;
            }

            createdItem = roll.Item;
            status = "Created and added Item_rusty_sword to Bag.";
            RefreshOutput();
        }

        private void RefreshOutput()
        {
            if (output == null) return;

            var text = new StringBuilder(status);
            if (createdItem != null)
            {
                text.AppendLine();
                text.AppendLine();
                text.AppendLine($"Item: {createdItem.Definition.itemId}");
                text.AppendLine($"Rarity: {createdItem.RolledRarity}");
                text.AppendLine($"Instance: {createdItem.InstanceId}");
                text.AppendLine($"Roll seed: {createdItem.RollSeed}");
                text.AppendLine("Rolled affixes:");

                foreach (var affix in createdItem.RandomAffixes)
                    text.AppendLine($"- {affix.slotType}: {affix.displayName} | {affix.tierId} | {affix.FormatValue()} | family={affix.affixFamilyId}");
            }

            output.text = text.ToString();
        }
    }
}