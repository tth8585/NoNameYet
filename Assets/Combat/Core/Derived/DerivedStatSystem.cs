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

            float fireRateBase = _cfg.FR_Base + dex * _cfg.FR_PerDex;
            float moveSpeedBase = _cfg.MS_Base + spd * _cfg.MS_PerSpd;

            float hpRegenBase = vit * _cfg.HPRegen_PerVit;
            float mpRegenBase = wis * _cfg.MPRegen_PerWis;

            // --- Apply statuses (post-derived) ---
            // FireRate: Berserk, Dazed
            float fireMul = 1f;
            if (_status.Has(StatusId.Berserk)) fireMul *= _cfg.BerserkMul;
            if (_status.Has(StatusId.Dazed)) fireMul *= _cfg.DazedMul;

            _fireRate = fireRateBase * fireMul;

            // MoveSpeed: Paralyze override, else Speedy/Slow multipliers
            if (_status.Has(StatusId.Paralyze))
            {
                _moveSpeed = 0f;
            }
            else
            {
                float moveMul = 1f;
                if (_status.Has(StatusId.Speedy)) moveMul *= _cfg.SpeedyMul;
                if (_status.Has(StatusId.Slow)) moveMul *= _cfg.SlowMul;

                _moveSpeed = moveSpeedBase * moveMul;
            }

            // Regen: Healing status boosts regen
            if (_status.Has(StatusId.Healing))
            {
                hpRegenBase = hpRegenBase * Mathf.Max(0f, _cfg.Healing_HPRegenMul) + _cfg.Healing_HPRegenAdd;
            }

            if(_status.Has(StatusId.Energized))
            {
                mpRegenBase = mpRegenBase * Mathf.Max(0f, _cfg.Energized_MPRegenMul) + _cfg.Energized_MPRegenAdd;
            }

            // Regen (no statuses for now)
            _hpRegen = hpRegenBase;
            _mpRegen = mpRegenBase;
        }
    }
}
