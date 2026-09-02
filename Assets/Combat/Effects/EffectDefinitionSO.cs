using System;
using UnityEngine;
using TTH.Combat.Attributes;
using TTH.Combat.Status;

namespace TTH.Combat.Effects
{
    public enum EffectStackPolicy
    {
        RefreshDuration = 0,     // refresh duration, keep best power (optional)
        StackCount = 1,          // add stacks up to maxStacks
        ReplaceIfStronger = 2,   // compare strength then replace
        UniqueByKey = 3          // one per uniqueKey (replace)
    }

    [Serializable]
    public struct StatusGrant
    {
        public StatusId id;
        public float durationSeconds; // <=0 means infinite
    }

    [CreateAssetMenu(menuName = "TTH/Combat/Effect Definition", fileName = "Effect_")]
    public sealed class EffectDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string effectId;
        public string[] tags;

        [Header("Duration")]
        public float durationSeconds = 5f; // <=0 means infinite

        [Header("Stacking")]
        public EffectStackPolicy policy = EffectStackPolicy.RefreshDuration;
        public int maxStacks = 1;                  // used by StackCount
        public string uniqueKey;                   // used by UniqueByKey
        public float strength = 1f;                // used by ReplaceIfStronger

        [Header("Payload: Attribute Modifiers")]
        public AttributeModifier[] modifiers;

        [Header("Payload: Status Flags")]
        public StatusGrant[] statuses;
    }
}
