using TTH.Combat.Attributes;
using TTH.Combat.Derived;
using TTH.Combat.Status;

namespace TTH.Combat.Runtime
{
    /// <summary>
    /// Simple runtime bundle so CombatSystem can read stats/status and write resources.
    /// You can expand later (faction/team, tags, etc.) without touching core pipeline.
    /// </summary>
    public sealed class CombatEntity
    {
        public int Id { get; }
        public AttributeSystem Attributes { get; }
        public StatusSystem Statuses { get; }
        public DerivedStatSystem Derived { get; }
        public ResourcePool Resources { get; }

        public CombatEntity(int id, AttributeSystem attributes, StatusSystem statuses, DerivedStatSystem derived, ResourcePool resources)
        {
            Id = id;
            Attributes = attributes;
            Statuses = statuses;
            Derived = derived;
            Resources = resources;
        }
    }
}
