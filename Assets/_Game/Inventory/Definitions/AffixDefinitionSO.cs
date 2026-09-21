using UnityEngine;
using TTH.Combat.Attributes;

namespace TTH.Game.Inventory
{
    [CreateAssetMenu(menuName = "TTH/Game/Inventory/Affix Definition", fileName = "Affix_")]
    public sealed class AffixDefinitionSO : ScriptableObject
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

        public RandomAffixDefinition ToRuntimeDefinition()
        {
            return new RandomAffixDefinition
            {
                affixId = affixId,
                affixFamilyId = affixFamilyId,
                displayName = displayName,
                slotType = slotType,
                isPercent = isPercent,
                effectType = effectType,
                attribute = attribute,
                operation = operation,
                gameplayModifier = gameplayModifier,
                tiers = tiers
            };
        }
    }
}