using UnityEngine;

namespace TTH.Game.Inventory
{
    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Ability Slot Weight Profile", fileName = "AbilitySlotWeights_")]
    public sealed class AbilitySlotWeightProfileSO : ScriptableObject
    {
        [Tooltip("Index 0 is 1 slot, index 1 is 2 slots, and so on.")]
        public float[] slotCountWeights = { 70f, 20f, 7f, 2f, 0.8f, 0.2f };

        public float GetWeightForSlotCount(int slotCount)
        {
            if (slotCount < 1 || slotCountWeights == null || slotCount > slotCountWeights.Length)
                return 0f;

            return slotCountWeights[slotCount - 1];
        }
    }
}