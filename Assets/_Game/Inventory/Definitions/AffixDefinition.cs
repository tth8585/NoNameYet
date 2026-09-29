using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Ability;

namespace TTH.Game.Inventory
{
    public enum AffixSlotType
    {
        Prefix,
        Suffix
    }

    public enum AffixEffectType
    {
        Attribute,
        Gameplay
    }

    public enum GameplayModifierType
    {
        none
    }

    [Serializable]
    public sealed class AffixTierDefinition
    {
        public string tierId;
        public float value;
        [Min(0f)] public float weight = 1f;
    }

    [Serializable]
    public sealed class RandomAffixDefinition
    {
        public string affixId;
        public string affixFamilyId;
        public string displayName;
        public AffixSlotType slotType;
        public bool isPercent;
        public AffixEffectType effectType;
        public AttributeId attribute;
        public ModifierOp operation = ModifierOp.Add;
        public GameplayModifierType gameplayModifier;
        public AffixTierDefinition[] tiers;

        public bool IsValid => !string.IsNullOrEmpty(affixId) &&
                               !string.IsNullOrEmpty(affixFamilyId) &&
                               tiers != null && tiers.Length > 0;
    }

    [Serializable]
    public sealed class RolledAffix
    {
        public string affixId;
        public string affixFamilyId;
        public string displayName;
        public AffixSlotType slotType;
        public string tierId;
        public float value;
        public bool isPercent;
        public AffixEffectType effectType;
        public AttributeId attribute;
        public ModifierOp operation;
        public GameplayModifierType gameplayModifier;

        public string FormatValue()
        {
            return isPercent ? $"+{value:0.#}%" : $"+{value:0.#}";
        }
    }

    public sealed class ItemRollResult
    {
        public bool Success { get; }
        public string Error { get; }
        public ItemInstance Item { get; }

        private ItemRollResult(bool success, string error, ItemInstance item)
        {
            Success = success;
            Error = error;
            Item = item;
        }

        public static ItemRollResult Succeeded(ItemInstance item) => new(true, string.Empty, item);
        public static ItemRollResult Failed(string error) => new(false, error, null);
    }

    public sealed class ItemRollService
    {
        public ItemRollResult Roll(ItemDefinitionSO definition, int? seed = null)
        {
            if (definition == null)
                return ItemRollResult.Failed("Item definition is missing.");
            if (definition.randomAffixRules == null)
                return ItemRollResult.Failed("Random affix rules are missing.");
            if (!definition.randomAffixRules.IsValid(out string ruleError))
                return ItemRollResult.Failed(ruleError);

            int rollSeed = seed ?? Guid.NewGuid().GetHashCode();
            var random = new System.Random(rollSeed);
            var rolls = new List<RolledAffix>();

            if (!TryRollSlotCounts(definition.randomAffixRules, random, out int prefixCount, out int suffixCount, out string countError))
                return ItemRollResult.Failed(countError);

            int totalCount = prefixCount + suffixCount;

            if (!RollSlot(definition, AffixSlotType.Prefix, prefixCount, random, rolls, out string prefixError))
                return ItemRollResult.Failed(prefixError);
            if (!RollSlot(definition, AffixSlotType.Suffix, suffixCount, random, rolls, out string suffixError))
                return ItemRollResult.Failed(suffixError);
            if (!TryRollAbilitySlotCount(definition, random, out int abilitySlotCount, out string loadoutError))
                return ItemRollResult.Failed(loadoutError);

            var abilityLoadout = new AbilityLoadout { slotCount = abilitySlotCount };
            var item = new ItemInstance(definition, 1, BuildStateKey(rolls, abilitySlotCount), false, rolls, rollSeed,
                GetRarityForAffixCount(totalCount), abilityLoadout);
            return ItemRollResult.Succeeded(item);
        }

        private static bool TryRollAbilitySlotCount(ItemDefinitionSO definition, System.Random random,
            out int slotCount, out string error)
        {
            var rules = definition.abilitySlotRules;
            int minimum = rules == null ? 1 : rules.minSlotCount;
            int maximum = rules == null ? 1 : rules.maxSlotCount;
            if (rules != null && !rules.IsValid(out error))
            {
                slotCount = 0;
                return false;
            }

            float totalWeight = 0f;
            for (int count = minimum; count <= maximum; count++)
            {
                float weight = GetAbilitySlotWeight(rules, count);
                if (weight < 0f || float.IsNaN(weight) || float.IsInfinity(weight))
                {
                    slotCount = 0;
                    error = "Ability slot weights must be finite and non-negative.";
                    return false;
                }
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                slotCount = 0;
                error = "At least one configured ability slot count must have a positive weight.";
                return false;
            }

            double roll = random.NextDouble() * totalWeight;
            slotCount = maximum;
            for (int count = minimum; count <= maximum; count++)
            {
                roll -= GetAbilitySlotWeight(rules, count);
                if (roll < 0d)
                {
                    slotCount = count;
                    break;
                }
            }

            error = string.Empty;
            return true;
        }

        private static float GetAbilitySlotWeight(AbilitySlotRulesSO rules, int slotCount)
        {
            if (rules != null && rules.weightProfile != null)
                return rules.weightProfile.GetWeightForSlotCount(slotCount);

            return Mathf.Pow(0.5f, slotCount - 1);
        }

        private static bool TryRollSlotCounts(RandomAffixRulesSO rules, System.Random random,
            out int prefixCount, out int suffixCount, out string error)
        {
            var validCounts = new SortedDictionary<int, List<Vector2Int>>();
            for (int prefix = rules.minPrefixAffixes; prefix <= rules.maxPrefixAffixes; prefix++)
            {
                for (int suffix = rules.minSuffixAffixes; suffix <= rules.maxSuffixAffixes; suffix++)
                {
                    int total = prefix + suffix;
                    if (total >= rules.minRandomAffixes && total <= rules.maxRandomAffixes &&
                        (total != 2 || (prefix == 1 && suffix == 1)))
                    {
                        if (!validCounts.TryGetValue(total, out var combinations))
                        {
                            combinations = new List<Vector2Int>();
                            validCounts.Add(total, combinations);
                        }
                        combinations.Add(new Vector2Int(prefix, suffix));
                    }
                }
            }

            float totalWeight = validCounts.Sum(pair => rules.GetCountWeight(pair.Key));
            if (totalWeight <= 0f)
            {
                prefixCount = 0;
                suffixCount = 0;
                error = "No weighted prefix/suffix count satisfies the configured affix rules.";
                return false;
            }

            double roll = random.NextDouble() * totalWeight;
            int selectedTotal = validCounts.Keys.Last();
            foreach (var pair in validCounts)
            {
                roll -= rules.GetCountWeight(pair.Key);
                if (roll < 0d)
                {
                    selectedTotal = pair.Key;
                    break;
                }
            }

            var validCombinations = validCounts[selectedTotal];
            Vector2Int selected = validCombinations[random.Next(validCombinations.Count)];
            prefixCount = selected.x;
            suffixCount = selected.y;
            error = string.Empty;
            return true;
        }

        private static ItemRarity GetRarityForAffixCount(int count)
        {
            switch (count)
            {
                case 1: return ItemRarity.Common;
                case 2: return ItemRarity.Magic;
                case 3: return ItemRarity.Rare;
                default: return ItemRarity.Legendary;
            }
        }

        private static bool RollSlot(ItemDefinitionSO definition, AffixSlotType slotType, int count,
            System.Random random, List<RolledAffix> result, out string error)
        {
            var candidates = definition.randomAffixPool == null
                ? new List<RandomAffixDefinition>()
                : definition.randomAffixPool.GetCandidates(slotType).ToList();
            var usedFamilies = new HashSet<string>(result.Select(affix => affix.affixFamilyId), StringComparer.Ordinal);

            for (int i = 0; i < count; i++)
            {
                var available = candidates.Where(affix => !usedFamilies.Contains(affix.affixFamilyId)).ToList();
                if (available.Count == 0)
                {
                    error = $"Not enough unique {slotType} affix families for {count} rolls.";
                    return false;
                }

                var selected = available[random.Next(available.Count)];
                var tier = SelectWeightedTier(selected.tiers, random);
                if (tier == null)
                {
                    error = $"Affix '{selected.affixId}' has no tier with positive weight.";
                    return false;
                }

                result.Add(new RolledAffix
                {
                    affixId = selected.affixId,
                    affixFamilyId = selected.affixFamilyId,
                    displayName = selected.displayName,
                    slotType = selected.slotType,
                    tierId = tier.tierId,
                    value = tier.value,
                    isPercent = selected.isPercent,
                    effectType = selected.effectType,
                    attribute = selected.attribute,
                    operation = selected.operation
                    ,gameplayModifier = selected.gameplayModifier
                });
                usedFamilies.Add(selected.affixFamilyId);
            }

            error = string.Empty;
            return true;
        }

        private static AffixTierDefinition SelectWeightedTier(AffixTierDefinition[] tiers, System.Random random)
        {
            float totalWeight = tiers.Where(tier => tier != null && tier.weight > 0f).Sum(tier => tier.weight);
            if (totalWeight <= 0f) return null;

            double roll = random.NextDouble() * totalWeight;
            foreach (var tier in tiers)
            {
                if (tier == null || tier.weight <= 0f) continue;
                roll -= tier.weight;
                if (roll < 0d) return tier;
            }
            return tiers.Last(tier => tier != null && tier.weight > 0f);
        }

        private static string BuildStateKey(IEnumerable<RolledAffix> rolls, int abilitySlotCount)
        {
            string affixState = string.Join("|", rolls.Select(roll => $"{roll.affixFamilyId}:{roll.tierId}:{roll.value:0.###}"));
            return $"{affixState}|abilitySlots:{abilitySlotCount}";
        }
    }
}