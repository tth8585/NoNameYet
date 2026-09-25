using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TTH.Game.Inventory
{
    public sealed class InventoryItemSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private Image icon;
        [SerializeField]
        private TMP_Text quantityText;
   
        private Action<InventoryItemSlot> clicked;

        public ItemInstance Item { get; private set; }

        public void Initialize(Action<InventoryItemSlot> onClicked)
        {
            clicked = onClicked;

            var quantityTransform = quantityText?.transform;
            if (quantityTransform == null)
            {
                var quantityObject = new GameObject("Quantity", typeof(RectTransform));
                quantityObject.transform.SetParent(transform, false);
                quantityTransform = quantityObject.transform;
            }

            quantityText = quantityTransform.GetComponent<TMP_Text>();
            if (quantityText == null)
                quantityText = quantityTransform.gameObject.AddComponent<TextMeshProUGUI>();
            quantityText.alignment = TextAlignmentOptions.BottomRight;
            quantityText.fontSize = 22f;
            quantityText.color = Color.white;
            quantityText.raycastTarget = false;
            var rect = quantityText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);
        }

        public void Bind(ItemInstance item)
        {
            Item = item;
            bool hasItem = item != null && item.Definition != null;
            gameObject.SetActive(hasItem);
            if (!hasItem)
                return;

            icon.sprite = item.Definition.icon;
            icon.color = item.Definition.icon == null ? Color.white : Color.white;
            quantityText.text = item.Quantity > 1 ? item.Quantity.ToString() : string.Empty;
        }

        public void Clear()
        {
            Item = null;
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"Pointer Clicked on {Item?.Definition?.name ?? "Empty Slot"}");
            if (eventData.button == PointerEventData.InputButton.Right && Item != null)
                clicked?.Invoke(this);
        }
    }
}