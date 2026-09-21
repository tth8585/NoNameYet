using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TTH.Game.Inventory
{
    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Random Affix Pool", fileName = "AffixPool_")]
    public sealed class RandomAffixPoolSO : ScriptableObject
    {
        [Header("Prefix Pool")]
        public AffixDefinitionSO[] prefixAffixAssets;

        [Header("Suffix Pool")]
        public AffixDefinitionSO[] suffixAffixAssets;

        public IEnumerable<RandomAffixDefinition> GetCandidates(AffixSlotType slotType)
        {
            var assets = slotType == AffixSlotType.Prefix ? prefixAffixAssets : suffixAffixAssets;
            return assets == null
                ? Enumerable.Empty<RandomAffixDefinition>()
                : assets.Where(asset => asset != null).Select(asset => asset.ToRuntimeDefinition());
        }

        private void OnValidate()
        {
            ValidateFamilies(GetCandidates(AffixSlotType.Prefix), AffixSlotType.Prefix);
            ValidateFamilies(GetCandidates(AffixSlotType.Suffix), AffixSlotType.Suffix);
        }

        private void ValidateFamilies(IEnumerable<RandomAffixDefinition> affixes, AffixSlotType slotType)
        {
            var families = new HashSet<string>();
            foreach (var affix in affixes)
            {
                if (affix == null || string.IsNullOrEmpty(affix.affixFamilyId)) continue;
                if (!families.Add(affix.affixFamilyId))
                    Debug.LogWarning($"[{name}] Duplicate {slotType} affix family: {affix.affixFamilyId}", this);
            }
        }
    }
}