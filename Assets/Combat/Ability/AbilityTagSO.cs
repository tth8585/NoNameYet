using UnityEngine;

namespace TTH.Combat.Ability
{
    [CreateAssetMenu(menuName = "TTH/Combat/Ability Tag", fileName = "AbilityTag_")]
    public sealed class AbilityTagSO : ScriptableObject
    {
        public string tagId = "tag_id";
    }
}
