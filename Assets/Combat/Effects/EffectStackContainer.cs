using System.Collections.Generic;

namespace TTH.Combat.Effects
{
    /// <summary>
    /// Per-entity container of active EffectInstances.
    /// </summary>
    public sealed class EffectStackContainer
    {
        private readonly List<EffectInstance> _active = new();
        public IReadOnlyList<EffectInstance> Active => _active;

        public int Count => _active.Count;

        internal List<EffectInstance> ActiveMutable => _active;
    }
}
