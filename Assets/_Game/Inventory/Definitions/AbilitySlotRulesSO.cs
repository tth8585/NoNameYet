using UnityEngine;

namespace TTH.Game.Inventory
{
    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Ability Slot Rules", fileName = "AbilitySlotRules_")]
    public sealed class AbilitySlotRulesSO : ScriptableObject
    {
        [Min(1)] public int minSlotCount = 1;
        [Min(1)] public int maxSlotCount = 1;
        public AbilitySlotWeightProfileSO weightProfile;

        public bool IsValid(out string error)
        {
            if (minSlotCount < 1 || maxSlotCount < minSlotCount)
            {
                error = "Ability slot minimum must be at least one and cannot exceed its maximum.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}