using System.Collections.Generic;
using System.Linq;

namespace TTH.Combat.Attributes
{
    public static class AttributeResolver
    {
        /// <summary>
        /// AttributeResolver v1.2 (ROTMG-like)
        ///
        /// Core responsibility:
        /// - Resolve final attribute value from base + modifiers
        /// - Apply stacking rules
        /// - Apply class cap ONLY for Progression source
        ///
        /// NOTE:
        /// - Override logic is handled outside (AttributeSystem level)
        /// - This class is pure & deterministic
        /// </summary>

        #region Public API

        public static float Resolve(
            AttributeId id,
            float baseValue,
            IReadOnlyList<AttributeModifier> modifiers,
            ClassStatCaps caps)
        {
            // Fast path: no modifiers → only base + cap
            if (modifiers == null || modifiers.Count == 0)
                return ClampByCap(id, baseValue, caps);

            var validMods = FilterValidModifiers(id, modifiers);
            if (validMods.Count == 0)
                return ClampByCap(id, baseValue, caps);

            // Split by source type (ROTMG v1.2 rule)
            var progressionMods = validMods
                .Where(m => m.SourceType == AttributeSourceType.Progression);

            var bonusMods = validMods
                .Where(m => m.SourceType == AttributeSourceType.Bonus);

            // 1) Progression pipeline (affected by class cap)
            float progressionValue = ResolveProgression(
                id,
                baseValue,
                progressionMods,
                caps
            );

            // 2) Bonus pipeline (NO cap)
            float bonusValue = ResolveBonus(bonusMods);

            // 3) Final value
            float finalValue = progressionValue + bonusValue;
            return ClampMinZero(finalValue);
        }

        #endregion

        #region Core Logic
        /// <summary>
        /// Filter modifiers:
        /// - Correct attribute
        /// - Not expired
        /// - Sorted by Order for deterministic result
        /// </summary>
        private static List<AttributeModifier> FilterValidModifiers(
            AttributeId id,
            IReadOnlyList<AttributeModifier> modifiers)
        {
            return modifiers
                .Where(m => m.Attribute == id && !m.IsExpired)
                .OrderBy(m => m.Order)
                .ToList();
        }

        /// <summary>
        /// Progression pipeline (Base + Progression mods)
        /// This part is affected by ClassStatCaps.
        ///
        /// Formula:
        /// ClampByCap(
        ///     (Base + Sum(Add)) * (1 + Sum(Multiply))
        /// )
        /// </summary>
        private static float ResolveProgression(
            AttributeId id,
            float baseValue,
            IEnumerable<AttributeModifier> mods,
            ClassStatCaps caps)
        {
            float value = baseValue;

            // Additive progression (e.g. level, potion)
            value += AggregateByStacking(mods.Where(m => m.Op == ModifierOp.Add));

            // Multiplicative progression
            float mulSum = AggregateByStacking(mods.Where(m => m.Op == ModifierOp.Multiply));
            value *= (1f + mulSum);

            // Cap applies ONLY here
            return ClampByCap(id, value, caps);
        }

        /// <summary>
        /// Bonus pipeline (gear, buff, debuff)
        /// This part is NEVER capped.
        ///
        /// Formula:
        /// (Sum(Add)) * (1 + Sum(Multiply))
        /// </summary>
        private static float ResolveBonus(IEnumerable<AttributeModifier> mods)
        {
            float value = 0f;

            value += AggregateByStacking(mods.Where(m => m.Op == ModifierOp.Add));

            float mulSum = AggregateByStacking(mods.Where(m => m.Op == ModifierOp.Multiply));
            value *= (1f + mulSum);

            return value;
        }
        #endregion

        #region Helpers

        /// <summary>
        /// Final safety clamp:
        /// Attribute value should never be negative in runtime.
        /// </summary>
        private static float ClampMinZero(float value)
        {
            return value < 0f ? 0f : value;
        }

        /// <summary>
        /// Aggregate modifiers after applying stacking policy.
        /// </summary>
        private static float AggregateByStacking(IEnumerable<AttributeModifier> mods)
        {
            var filtered = ApplyStackingFilter(mods);
            float sum = 0f;
            foreach (var m in filtered) sum += m.Value;
            return sum;
        }

        /// <summary>
        /// Apply stacking rules (HighestOnly, LowestOnly, StackAll...)
        /// Order is preserved at the end for determinism.
        /// </summary>
        private static IEnumerable<AttributeModifier> ApplyStackingFilter(IEnumerable<AttributeModifier> mods)
        {
            var list = mods.ToList();
            if (list.Count <= 1) return list;

            var result = new List<AttributeModifier>();

            foreach (var g in list.GroupBy(m => m.Stacking))
            {
                switch (g.Key)
                {
                    case StackingPolicy.HighestOnly:
                        result.Add(g.OrderByDescending(x => x.Value).First());
                        break;
                    case StackingPolicy.LowestOnly:
                        result.Add(g.OrderBy(x => x.Value).First());
                        break;
                    default:
                        result.AddRange(g);
                        break;
                }
            }

            return result.OrderBy(m => m.Order);
        }

        /// <summary>
        /// Clamp value by class cap if available.
        /// Used ONLY for Progression stats.
        /// </summary>
        private static float ClampByCap(AttributeId id, float value, ClassStatCaps caps)
        {
            if (value < 0f) value = 0f;

            if (caps != null && caps.TryGetCap(id, out var cap))
            {
                if (value > cap) value = cap;
            }

            return value;
        }

        #endregion
    }
}
