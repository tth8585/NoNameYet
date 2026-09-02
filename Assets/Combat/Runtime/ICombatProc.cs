namespace TTH.Combat.Runtime
{
    /// <summary>
    /// Extension point for Phase B: effects/procs can mutate DamageContext (e.g. apply statuses, add temporary mods, etc.)
    /// </summary>
    public interface ICombatProc
    {
        void OnHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender);
        void AfterHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender);
    }
}
