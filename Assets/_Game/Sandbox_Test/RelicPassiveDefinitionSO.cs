using UnityEngine;
using TTH.Combat.Runtime;

public abstract class PassiveAbilityRuntime
{
    public virtual bool IsReady => false;
    public virtual void OnBeforeHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender) { }
    public virtual PassiveReaction OnAfterHit(in DamageContext ctx, CombatEntity attacker, CombatEntity defender) => default;
    public virtual void OnKill(in DamageContext ctx, CombatEntity attacker, CombatEntity defender) { }
    public virtual float OnTurnStart(CombatEntity actor) => 0f;
    public virtual void OnTurnEnd(CombatEntity actor) { }
    public virtual void Reset() { }
}

public abstract class PassiveAbilityDefinitionSO : ScriptableObject
{
    public abstract PassiveAbilityRuntime CreateRuntime();
}

[CreateAssetMenu(menuName = "TTH/Game/Passive Abilities/Next Damage Multiplier", fileName = "Passive_NextDamageMultiplier_")]
public sealed class NextDamageMultiplierPassiveSO : PassiveAbilityDefinitionSO
{
    [Min(0f)] public float multiplier = 2f;

    public override PassiveAbilityRuntime CreateRuntime()
    {
        return new NextDamageMultiplierPassiveRuntime(multiplier);
    }
}

public sealed class NextDamageMultiplierPassiveRuntime : PassiveAbilityRuntime
{
    private readonly float multiplier;
    private bool isReady;
    public override bool IsReady => isReady;

    public NextDamageMultiplierPassiveRuntime(float multiplier)
    {
        this.multiplier = Mathf.Max(0f, multiplier);
    }

    public override PassiveReaction OnAfterHit(in DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        if (ctx.didDamage) isReady = true;
        return default;
    }

    public override void OnBeforeHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        if (!isReady || ctx.baseDamage <= 0f) return;
        isReady = false;
        ctx.baseDamage *= multiplier;
    }

    public override void Reset()
    {
        isReady = false;
    }
}

[CreateAssetMenu(menuName = "TTH/Game/Passive Abilities/Heal On Turn Start", fileName = "Passive_HealOnTurnStart_")]
public sealed class HealOnTurnStartPassiveSO : PassiveAbilityDefinitionSO
{
    [Min(0f)] public float healAmount = 5f;

    public override PassiveAbilityRuntime CreateRuntime()
    {
        return new HealOnTurnStartPassiveRuntime(healAmount);
    }
}

public sealed class HealOnTurnStartPassiveRuntime : PassiveAbilityRuntime
{
    private readonly float healAmount;

    public HealOnTurnStartPassiveRuntime(float healAmount)
    {
        this.healAmount = Mathf.Max(0f, healAmount);
    }

    public override float OnTurnStart(CombatEntity actor)
    {
        return healAmount;
    }
}

public enum PassiveReactionType
{
    None,
    ApplyPoison
}

public readonly struct PassiveReaction
{
    public readonly PassiveReactionType Type;
    public readonly CombatEntity Target;
    public readonly float DamagePerTurn;
    public readonly int DurationTurns;

    private PassiveReaction(PassiveReactionType type, CombatEntity target, float damagePerTurn, int durationTurns)
    {
        Type = type;
        Target = target;
        DamagePerTurn = damagePerTurn;
        DurationTurns = durationTurns;
    }

    public static PassiveReaction Poison(CombatEntity target, float damagePerTurn, int durationTurns)
    {
        return new PassiveReaction(PassiveReactionType.ApplyPoison, target, damagePerTurn, durationTurns);
    }
}

[CreateAssetMenu(menuName = "TTH/Game/Passive Abilities/Venomous Retaliation", fileName = "Passive_VenomousRetaliation_")]
public sealed class VenomousRetaliationPassiveSO : PassiveAbilityDefinitionSO
{
    [Min(0f)] public float poisonDamagePerTurn = 5f;
    [Min(1)] public int poisonDurationTurns = 1;

    public override PassiveAbilityRuntime CreateRuntime()
    {
        return new VenomousRetaliationPassiveRuntime(poisonDamagePerTurn, poisonDurationTurns);
    }
}

public sealed class VenomousRetaliationPassiveRuntime : PassiveAbilityRuntime
{
    private readonly float poisonDamagePerTurn;
    private readonly int poisonDurationTurns;

    public VenomousRetaliationPassiveRuntime(float poisonDamagePerTurn, int poisonDurationTurns)
    {
        this.poisonDamagePerTurn = Mathf.Max(0f, poisonDamagePerTurn);
        this.poisonDurationTurns = Mathf.Max(1, poisonDurationTurns);
    }

    public override PassiveReaction OnAfterHit(in DamageContext ctx, CombatEntity attacker, CombatEntity defender)
    {
        if (!ctx.didDamage || ctx.kind == DamageKind.Dot || attacker == null)
            return default;

        return PassiveReaction.Poison(attacker, poisonDamagePerTurn, poisonDurationTurns);
    }
}