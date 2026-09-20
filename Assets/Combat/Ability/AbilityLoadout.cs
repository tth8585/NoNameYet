using System;
using UnityEngine;

namespace TTH.Combat.Ability
{
    [Serializable]
    public sealed class AbilityLoadout
    {
        public AbilityDefinition activeAbility;
        public SupportAbilityDefinitionSO[] supportLinks = Array.Empty<SupportAbilityDefinitionSO>();

        public AbilityLoadout()
        {
        }

        public AbilityLoadout(AbilityDefinition activeAbility, SupportAbilityDefinitionSO[] supportLinks)
        {
            this.activeAbility = activeAbility;
            this.supportLinks = supportLinks ?? Array.Empty<SupportAbilityDefinitionSO>();
        }

        public SupportAbilityDefinitionSO[] GetMatchingSupports()
        {
            if (activeAbility == null || supportLinks == null || supportLinks.Length == 0)
                return Array.Empty<SupportAbilityDefinitionSO>();

            var matches = new System.Collections.Generic.List<SupportAbilityDefinitionSO>();
            for (int i = 0; i < supportLinks.Length; i++)
            {
                var support = supportLinks[i];
                if (support != null && support.Matches(activeAbility) && !matches.Contains(support))
                    matches.Add(support);
            }

            return matches.ToArray();
        }
    }
}
