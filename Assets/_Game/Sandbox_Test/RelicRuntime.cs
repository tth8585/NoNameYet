using TTH.Combat.Runtime;

public sealed class RelicRuntime
{
    public RelicDefinitionSO Definition { get; }
    private readonly PassiveAbilityRuntime passiveRuntime;

    public bool IsPassiveReady => passiveRuntime != null && passiveRuntime.IsReady;
    public bool HasActiveSkill => Definition != null && Definition.activeAbility != null;

    public RelicRuntime(RelicDefinitionSO definition)
    {
        Definition = definition;
        passiveRuntime = definition != null && definition.passiveAbility != null
            ? definition.passiveAbility.CreateRuntime()
            : null;
    }

    public void OnBeforeHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        passiveRuntime?.OnBeforeHit(ref ctx, attacker, defender);
    }

    public PassiveReaction OnAfterHit(in DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        return passiveRuntime?.OnAfterHit(ctx, attacker, defender) ?? default;
    }

    public void OnKill(in DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        passiveRuntime?.OnKill(ctx, attacker, defender);
    }

    public float OnTurnStart(CombatEntity actor)
    {
        return passiveRuntime?.OnTurnStart(actor) ?? 0f;
    }

    public void OnTurnEnd(CombatEntity actor)
    {
        passiveRuntime?.OnTurnEnd(actor);
    }

    public void Reset()
    {
        passiveRuntime?.Reset();
    }
}