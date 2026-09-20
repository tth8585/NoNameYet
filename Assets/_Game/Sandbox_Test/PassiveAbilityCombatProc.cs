using System.Collections.Generic;
using TTH.Combat.Runtime;

public sealed class CharacterTestAttack
{
    public float BaseDamage { get; }

    public CharacterTestAttack(float baseDamage)
    {
        BaseDamage = baseDamage;
    }
}

public sealed class PassiveAbilityCombatProc : ICombatProc
{
    private readonly Dictionary<int, IReadOnlyList<RelicRuntime>> relicsByEntityId = new();
    private readonly Dictionary<int, PoisonState> poisonByEntityId = new();
    private CombatSystem combatSystem;

    private sealed class PoisonState
    {
        public float DamagePerTurn;
        public int RemainingTurns;
    }

    public void BindCombatSystem(CombatSystem system)
    {
        combatSystem = system;
    }

    public void Register(CombatEntity entity, IReadOnlyList<RelicRuntime> relics)
    {
        if (entity == null) return;
        relicsByEntityId[entity.Id] = relics;
    }

    public void OnHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        if (ctx.source is CharacterTestAttack testAttack)
        {
            ctx.baseDamage = testAttack.BaseDamage;
            ctx.hasBaseDamage = true;
        }

        if (!relicsByEntityId.TryGetValue(attacker.Id, out var relics)) return;
        foreach (var relic in relics) relic.OnBeforeHit(ref ctx, attacker, defender);
    }

    public void AfterHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        if (!ctx.didDamage) return;
        if (relicsByEntityId.TryGetValue(defender.Id, out var defenderRelics))
        {
            foreach (var relic in defenderRelics)
            {
                var reaction = relic.OnAfterHit(ctx, attacker, defender);
                if (reaction.Type == PassiveReactionType.ApplyPoison)
                    ApplyPoison(reaction);
            }
        }

        if (ctx.didKill && relicsByEntityId.TryGetValue(attacker.Id, out var attackerRelics))
        {
            foreach (var relic in attackerRelics) relic.OnKill(ctx, attacker, defender);
        }
    }

    public void OnTurnStart(CombatEntity actor)
    {
        TickPoison(actor);
        if (!relicsByEntityId.TryGetValue(actor.Id, out var relics)) return;

        var totalHealing = 0f;
        foreach (var relic in relics) totalHealing += relic.OnTurnStart(actor);
        if (totalHealing <= 0f || actor.Resources == null) return;

        actor.Resources.ApplyHPDelta(totalHealing);
        actor.Resources.ClampToMax(actor.Attributes);
    }

    private void ApplyPoison(PassiveReaction reaction)
    {
        if (reaction.Target == null || reaction.DamagePerTurn <= 0f) return;
        poisonByEntityId[reaction.Target.Id] = new PoisonState
        {
            DamagePerTurn = reaction.DamagePerTurn,
            RemainingTurns = reaction.DurationTurns
        };
    }

    private void TickPoison(CombatEntity actor)
    {
        if (combatSystem == null || actor == null || !poisonByEntityId.TryGetValue(actor.Id, out var poison)) return;

        combatSystem.HandleHit(new HitEvent(
            actor,
            actor,
            UnityEngine.Vector2.zero,
            new CharacterTestAttack(poison.DamagePerTurn)), DamageKind.Dot);

        poison.RemainingTurns--;
        if (poison.RemainingTurns <= 0) poisonByEntityId.Remove(actor.Id);
    }

    public void OnTurnEnd(CombatEntity actor)
    {
        if (!relicsByEntityId.TryGetValue(actor.Id, out var relics)) return;
        foreach (var relic in relics) relic.OnTurnEnd(actor);
    }
}