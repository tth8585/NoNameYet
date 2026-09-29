using UnityEngine;

namespace TTH.Game.Inventory
{
    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Affix Count Weight Profile", fileName = "AffixCountWeights_")]
    public sealed class AffixCountWeightProfileSO : ScriptableObject
    {
        [Min(0f)] public float oneAffixWeight = 60f;
        [Min(0f)] public float twoAffixWeight = 25f;
        [Min(0f)] public float threeAffixWeight = 12f;
        [Min(0f)] public float fourOrMoreAffixWeight = 3f;

        public float GetWeightForCount(int count)
        {
            switch (count)
            {
                case 1: return oneAffixWeight;
                case 2: return twoAffixWeight;
                case 3: return threeAffixWeight;
                default: return fourOrMoreAffixWeight;
            }
        }

        public bool HasValidWeights => IsValidWeight(oneAffixWeight) &&
                                       IsValidWeight(twoAffixWeight) &&
                                       IsValidWeight(threeAffixWeight) &&
                                       IsValidWeight(fourOrMoreAffixWeight) &&
                                       oneAffixWeight + twoAffixWeight + threeAffixWeight + fourOrMoreAffixWeight > 0f;

        private static bool IsValidWeight(float weight)
        {
            return weight >= 0f && !float.IsNaN(weight) && !float.IsInfinity(weight);
        }
    }
}