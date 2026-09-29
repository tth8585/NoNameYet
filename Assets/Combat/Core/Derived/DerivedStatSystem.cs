using TTH.Combat.Attributes;
using TTH.Combat.Status;
using UnityEngine;

namespace TTH.Combat.Derived
{
    /// <summary>
    /// Computes derived stats from primary attributes + statuses.
    /// Caches per (AttributeSystem.Version, StatusSystem.Version).
    /// </summary>
    public sealed class DerivedStatSystem
    {
        private readonly AttributeSystem _attr;
        private readonly StatusSystem _status;
        private readonly DerivedStatsConfigSO _cfg;

        private int _cachedAttrVer = -1;
        private int _cachedStatusVer = -1;

        private float _fireRate;
        private float _damageMultiplier;
        private float _moveSpeed;
        private float _hpRegen;
        private float _mpRegen;

        public DerivedStatSystem(AttributeSystem attr, StatusSystem status, DerivedStatsConfigSO cfg)
        {
            _attr = attr;
            _status = status;
            _cfg = cfg;
        }

        public float Get(DerivedStatId id)
        {
            RecalcIfNeeded();

            return id switch
            {
                DerivedStatId.DamageMultiplier => _damageMultiplier,
                DerivedStatId.FireRate => _fireRate,
                DerivedStatId.MoveSpeed => _moveSpeed,
                DerivedStatId.HPRegen => _hpRegen,
                DerivedStatId.MPRegen => _mpRegen,
                _ => 0f
            };
        }

        private void RecalcIfNeeded()
        {
            int av = _attr.Version;
            int sv = _status.Version;

            if (av == _cachedAttrVer && sv == _cachedStatusVer)
                return;

            _cachedAttrVer = av;
            _cachedStatusVer = sv;

            // --- Base derived from primary ---
            float dex = _attr.Get(AttributeId.DEX);
            float spd = _attr.Get(AttributeId.SPD);
            float vit = _attr.Get(AttributeId.VIT);
            float wis = _attr.Get(AttributeId.WIS);
            float atk = _attr.Get(AttributeId.ATK);

            _damageMultiplier = Mathf.Max(0f, _cfg.DamageMultiplier_Base + atk * _cfg.DamageMultiplier_PerAtk);
            float fireRateBase = _cfg.FR_Base + dex * _cfg.FR_PerDex;
            float moveSpeedBase = _cfg.MS_Base + spd * _cfg.MS_PerSpd;

            float hpRegenBase = _cfg.HPRegen_Base + vit * _cfg.HPRegen_PerVit;
            float mpRegenBase = _cfg.MPRegen_Base + wis * _cfg.MPRegen_PerWis;

            // --- Apply statuses (post-derived) ---
            _fireRate = fireRateBase * (_status.Has(StatusId.Berserk) ? _cfg.BerserkMul : 1f);
            _moveSpeed = moveSpeedBase * (_status.Has(StatusId.Speedy) ? _cfg.SpeedyMul : 1f);

            if (_status.Has(StatusId.Healing))
                hpRegenBase += _cfg.Healing_HPRegenAdd;

            _hpRegen = hpRegenBase;
            _mpRegen = mpRegenBase;
        }

    }
}
