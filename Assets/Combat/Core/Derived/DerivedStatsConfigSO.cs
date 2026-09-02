using UnityEngine;

namespace TTH.Combat.Derived
{
    [CreateAssetMenu(menuName = "TTH/Combat/Derived Stats Config", fileName = "DerivedStatsConfig")]
    public sealed class DerivedStatsConfigSO : ScriptableObject
    {
        [Header("FireRate (shots/sec)")]
        public float FR_Base = 1.5f;
        public float FR_PerDex = 0.05f;

        [Header("MoveSpeed (units/sec)")]
        public float MS_Base = 2.0f;
        public float MS_PerSpd = 0.06f;

        [Header("Regen (per sec)")]
        public float HPRegen_PerVit = 0.10f;
        public float MPRegen_PerWis = 0.05f;

        [Header("Status Multipliers")]
        public float BerserkMul = 1.5f;
        public float DazedMul = 0.5f;

        [Header("Healing Status (affects regen)")]
        [Tooltip("When StatusId.Healing is active, HPRegen is multiplied by this value.")]
        public float Healing_HPRegenMul = 1.0f;

        [Tooltip("Optional flat bonus regen when Healing is active (per sec).")]
        public float Healing_HPRegenAdd = 2.0f;

        [Header("Energized  Status (affects regen)")]
        [Tooltip("When StatusId.Energized  is active, MPRegen is multiplied by this value.")]
        public float Energized_MPRegenMul = 2.0f;

        [Tooltip("Optional flat bonus regen when Healing is active (per sec).")]
        public float Energized_MPRegenAdd = 1.0f;

        public float SpeedyMul = 1.3f;
        public float SlowMul = 0.5f;
    }
}
