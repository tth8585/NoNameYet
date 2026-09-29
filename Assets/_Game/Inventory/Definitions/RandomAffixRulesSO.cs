using UnityEngine;

namespace TTH.Game.Inventory
{
    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Random Affix Rules", fileName = "RandomAffixRules_")]
    public sealed class RandomAffixRulesSO : ScriptableObject
    {
        [Min(1)] public int minRandomAffixes = 1;
        [Min(0)] public int maxRandomAffixes = 4;
        [Min(0)] public int minPrefixAffixes;
        [Min(0)] public int maxPrefixAffixes = 2;
        [Min(0)] public int minSuffixAffixes;
        [Min(0)] public int maxSuffixAffixes = 2;
        public AffixCountWeightProfileSO countWeightProfile;

        public float GetCountWeight(int count)
        {
            if (countWeightProfile != null)
                return countWeightProfile.GetWeightForCount(count);

            switch (count)
            {
                case 1: return 60f;
                case 2: return 25f;
                case 3: return 12f;
                default: return 3f;
            }
        }

        public bool IsValid(out string error)
        {
            if (minRandomAffixes > maxRandomAffixes)
            {
                error = "Minimum random affixes cannot exceed maximum.";
                return false;
            }

            if (minPrefixAffixes > maxPrefixAffixes || minSuffixAffixes > maxSuffixAffixes)
            {
                error = "Affix slot minimum cannot exceed maximum.";
                return false;
            }

            if (countWeightProfile != null && !countWeightProfile.HasValidWeights)
            {
                error = "Affix count weight profile is invalid.";
                return false;
            }

            if (maxPrefixAffixes + maxSuffixAffixes < minRandomAffixes ||
                minPrefixAffixes + minSuffixAffixes > maxRandomAffixes)
            {
                error = "Affix count rules cannot produce a valid total.";
                return false;
            }

            bool hasWeightedCount = false;
            for (int prefix = minPrefixAffixes; prefix <= maxPrefixAffixes; prefix++)
            {
                for (int suffix = minSuffixAffixes; suffix <= maxSuffixAffixes; suffix++)
                {
                    int total = prefix + suffix;
                    if (total >= minRandomAffixes && total <= maxRandomAffixes &&
                        (total != 2 || (prefix == 1 && suffix == 1)) && GetCountWeight(total) > 0f)
                        hasWeightedCount = true;
                }
            }

            if (!hasWeightedCount)
            {
                error = "No valid affix count has a positive weight.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}