using System.Collections.Generic;

namespace TTH.Combat.Runtime
{
    public sealed class CompositeCombatProc : ICombatProc
    {
        private readonly List<ICombatProc> procs = new();

        public CompositeCombatProc(params ICombatProc[] initialProcs)
        {
            if (initialProcs == null) return;
            foreach (var proc in initialProcs)
            {
                if (proc != null) procs.Add(proc);
            }
        }

        public void Add(ICombatProc proc)
        {
            if (proc != null) procs.Add(proc);
        }

        public void OnHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
        {
            foreach (var proc in procs) proc.OnHit(ref ctx, attacker, defender);
        }

        public void AfterHit(ref DamageContext ctx, CombatEntity attacker, CombatEntity defender)
        {
            foreach (var proc in procs) proc.AfterHit(ref ctx, attacker, defender);
        }

        public void OnTurnStart(CombatEntity actor)
        {
            foreach (var proc in procs) proc.OnTurnStart(actor);
        }

        public void OnTurnEnd(CombatEntity actor)
        {
            foreach (var proc in procs) proc.OnTurnEnd(actor);
        }
    }
}