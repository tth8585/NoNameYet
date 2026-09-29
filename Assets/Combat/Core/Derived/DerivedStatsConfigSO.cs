using UnityEngine;

namespace TTH.Combat.Derived
{
    [CreateAssetMenu(menuName = "TTH/Combat/Derived Stats Config", fileName = "DerivedStatsConfig")]
    public sealed class DerivedStatsConfigSO : ScriptableObject
    {
        [Header("Damage Multiplier")]
        [Min(0f)] public float DamageMultiplier_Base = 0.5f;
        [Min(0f)] public float DamageMultiplier_PerAtk = 0.02f;

        [Header("Attack Speed (attacks/sec)")]
        public float FR_Base = 1.5f;
        public float FR_PerDex = 6.5f / 75f;

        [Header("Move Speed (tiles/sec)")]
        public float MS_Base = 4f;
        public float MS_PerSpd = 5.6f / 75f;

        [Header("Regen (per sec)")]
        public float HPRegen_Base = 2f;
        public float HPRegen_PerVit = 0.2407f;
        public float MPRegen_Base = 0.5f;
        public float MPRegen_PerWis = 0.12f;

        [Header("Status Multipliers")]
        public float BerserkMul = 1.25f;
        public float SpeedyMul = 1.5f;

        [Header("Healing Status (affects regen)")]
        [Min(0f)] public float Healing_HPRegenAdd = 20f;
    }
}
