using System;
using TTH.Combat.Attributes;

namespace TTH.Combat.Runtime
{
    /// <summary>
    /// Holds mutable runtime resources (CurrentHP/CurrentMP).
    /// MaxHP/MaxMP are read from AttributeSystem (HP/MP attributes represent MAX).
    /// </summary>
    public sealed class ResourcePool
    {
        public event Action<float, float> OnHPChanged; // (before, after)
        public event Action<float, float> OnMPChanged;

        public float CurrentHP { get; private set; }
        public float CurrentMP { get; private set; }

        public bool IsDead => CurrentHP <= 0f;

        public ResourcePool(float initialHP, float initialMP)
        {
            CurrentHP = Math.Max(0f, initialHP);
            CurrentMP = Math.Max(0f, initialMP);
        }

        public void SetHP(float value)
        {
            float before = CurrentHP;
            CurrentHP = Math.Max(0f, value);
            if (before != CurrentHP) OnHPChanged?.Invoke(before, CurrentHP);
        }

        public void SetMP(float value)
        {
            float before = CurrentMP;
            CurrentMP = Math.Max(0f, value);
            if (before != CurrentMP) OnMPChanged?.Invoke(before, CurrentMP);
        }

        public void ApplyHPDelta(float delta)
        {
            if (delta == 0f) return;
            float before = CurrentHP;
            CurrentHP = Math.Max(0f, CurrentHP + delta);
            if (before != CurrentHP) OnHPChanged?.Invoke(before, CurrentHP);
        }

        public void ApplyMPDelta(float delta)
        {
            if (delta == 0f) return;
            float before = CurrentMP;
            CurrentMP = Math.Max(0f, CurrentMP + delta);
            if (before != CurrentMP) OnMPChanged?.Invoke(before, CurrentMP);
        }

        /// <summary>
        /// Clamp current values to current MAX values from AttributeSystem.
        /// Call this when MaxHP/MaxMP might change (equip/unequip/buff expired).
        /// </summary>
        public void ClampToMax(AttributeSystem attr)
        {
            if (attr == null) return;

            float maxHP = Math.Max(0f, attr.Get(AttributeId.HP));
            float maxMP = Math.Max(0f, attr.Get(AttributeId.MP));

            if (CurrentHP > maxHP) SetHP(maxHP);
            if (CurrentMP > maxMP) SetMP(maxMP);
        }


        /// <summary>
        /// Initialize current resources to MAX (e.g., spawn / respawn).
        /// </summary>
        public void FillToMax(AttributeSystem attr)
        {
            if (attr == null) return;
            SetHP(Math.Max(0f, attr.Get(AttributeId.HP)));
            SetMP(Math.Max(0f, attr.Get(AttributeId.MP)));
        }
    }
}
