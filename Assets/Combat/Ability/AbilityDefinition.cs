using System;
using UnityEngine;
using TTH.Combat.Effects;
using TTH.Combat.Attributes;

namespace TTH.Combat.Ability
{
    public enum AbilityTarget
    {
        Defender,
        Attacker
    }

    public enum AbilityCastTarget
    {
        Self,
        AlliesInRange,
        SelfAndAlliesInRange
    }

    public enum AbilityScaleStat
    {
        HP, MP, ATK, DEF, SPD, DEX, VIT, WIS
    }

    // NEW: OnCast action kind
    public enum AbilityOnCastActionKind
    {
        ApplyEffect,
        ResourceDelta
    }

    public enum AbilityResourceType
    {
        HP,
        MP
        // sau này thêm Shield/Energy… không cần sửa runner nếu Resources có API tương ứng
    }

    [Serializable]
    public struct AbilityStepScaling
    {
        public bool enabled;
        public AbilityScaleStat stat;

        [Tooltip("Ngưỡng stat bắt đầu tính.")]
        public float threshold;

        [Tooltip("Mỗi bao nhiêu stat thì +1 step.")]
        public float perStep;

        [Tooltip("Mỗi step cộng thêm bao nhiêu.")]
        public float stepValue;

        public float Evaluate(Func<AbilityScaleStat, float> statGetter)
        {
            if (!enabled || statGetter == null || perStep <= 0f) return 0f;

            float s = statGetter(stat);
            float over = s - threshold;
            if (over <= 0f) return 0f;

            int steps = Mathf.FloorToInt(over / perStep);
            return steps * stepValue;
        }
    }

    [Serializable]
    public struct AbilityParamFloat
    {
        public float baseValue;
        public AbilityStepScaling step;

        public bool clamp;
        public float min;
        public float max;

        public float Evaluate(Func<AbilityScaleStat, float> statGetter)
        {
            float v = baseValue + step.Evaluate(statGetter);
            if (clamp) v = Mathf.Clamp(v, min, max);
            return v;
        }
    }

    [Serializable]
    public struct AbilityParamInt
    {
        public int baseValue;
        public AbilityStepScaling step;

        public bool clamp;
        public int min;
        public int max;

        public int Evaluate(Func<AbilityScaleStat, float> statGetter)
        {
            float v = baseValue + step.Evaluate(statGetter);
            int i = Mathf.FloorToInt(v);
            if (clamp) i = Mathf.Clamp(i, min, max);
            return i;
        }
    }

    [Serializable]
    public sealed class AbilityOnHitEffect
    {
        public AbilityTarget target = AbilityTarget.Defender;

        [Tooltip("EffectDefinitionSO sẽ được apply qua EffectSystemLite (stacking policy nằm trong def).")]
        public EffectDefinitionSO effect;

        [Min(0f)] public float chance = 1f;
    }

    [Serializable]
    public sealed class AbilityOnCastEffect
    {
        [Header("Targeting")]
        public AbilityCastTarget target = AbilityCastTarget.Self;

        [Tooltip("Bán kính chọn ally (nếu target có allies).")]
        public AbilityParamFloat radius;

        [Tooltip("Số ally tối đa (KHÔNG tính self). 0 = không giới hạn.")]
        public AbilityParamInt maxAllies;

        [Header("Action")]
        public AbilityOnCastActionKind action = AbilityOnCastActionKind.ApplyEffect;

        [Tooltip("Dùng cho ApplyEffect")]
        public EffectDefinitionSO effect;

        [Tooltip("Dùng cho ResourceDelta")]
        public AbilityResourceType resource = AbilityResourceType.HP;

        [Tooltip("Dùng cho ResourceDelta. Có thể âm để trừ.")]
        public AbilityParamFloat amount;

        [Min(0f)] public float chance = 1f;
    }

    [CreateAssetMenu(menuName = "TTH/Combat/Ability Definition", fileName = "Ability_")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string abilityId = "ability_id";

        [Header("Core Params (Snapshot at cast)")]
        public AbilityParamFloat cooldown;
        public AbilityParamFloat damage;   // Archer projectile / wizard spell base damage (payload)
        public AbilityParamFloat radius;   // AoE radius (NOT ally selection radius)
        public AbilityParamFloat duration; // DoT/beam duration (future)

        [Header("On-Cast Actions")]
        public AbilityOnCastEffect[] onCast;

        [Header("On-Hit Effects")]
        public AbilityOnHitEffect[] onHit;

        public static float GetStatFromAttributes(AttributeSystem attrs, AbilityScaleStat s)
        {
            if (attrs == null) return 0f;

            return s switch
            {
                AbilityScaleStat.HP => attrs.Get(AttributeId.HP),
                AbilityScaleStat.MP => attrs.Get(AttributeId.MP),
                AbilityScaleStat.ATK => attrs.Get(AttributeId.ATK),
                AbilityScaleStat.DEF => attrs.Get(AttributeId.DEF),
                AbilityScaleStat.SPD => attrs.Get(AttributeId.SPD),
                AbilityScaleStat.DEX => attrs.Get(AttributeId.DEX),
                AbilityScaleStat.VIT => attrs.Get(AttributeId.VIT),
                AbilityScaleStat.WIS => attrs.Get(AttributeId.WIS),
                _ => 0f
            };
        }
    }
}
