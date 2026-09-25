using System.Collections.Generic;
using UnityEngine;

namespace TTH.Game.Inventory
{
    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Item Definition Database", fileName = "ItemDefinitionDatabase")]
    public sealed class ItemDefinitionDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<ItemDefinitionSO> definitions = new();

        private Dictionary<string, ItemDefinitionSO> byId;

        public ItemDefinitionSO Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            BuildIndex();
            byId.TryGetValue(itemId, out var definition);
            return definition;
        }

        private void BuildIndex()
        {
            if (byId != null)
                return;

            byId = new Dictionary<string, ItemDefinitionSO>();
            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.itemId))
                    continue;

                if (byId.ContainsKey(definition.itemId))
                {
                    Debug.LogError($"Duplicate itemId in database: {definition.itemId}", this);
                    continue;
                }

                byId.Add(definition.itemId, definition);
            }
        }
    }
}