using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Ability;
using TTH.Combat.Attributes;
using TTH.Combat.Runtime;
using TTH.Combat.Status;

public sealed class CharacterSystemTest : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO characterStats;
    [SerializeField] private EnemyStatsSO enemyStats;
    [SerializeField] private AbilityDefinition activeTenDamageAbility;
    [SerializeField] private AbilityDefinition healTenAbility;

    private AttributeSystem characterAttributes;
    private ResourcePool characterResources;
    private CombatEntity characterEntity;
    private CombatEntity enemyEntity;
    private CombatSystem combatSystem;
    private AbilityRunner abilityRunner;
    private float enemyHealth;
    private readonly List<string> eventLog = new();

    private void Awake()
        {
            BuildRuntime();
        }

    private void BuildRuntime()
        {
            if (characterStats == null || enemyStats == null)
            {
                AddLog("Assign Character Stats and Enemy Stats in the Inspector.");
                return;
            }

            var characterStatBlock = new StatBlock();
            characterStatBlock.SetBase(AttributeId.ATK, characterStats.baseAttack);
            characterStatBlock.SetBase(AttributeId.DEF, characterStats.baseDefense);
            characterStatBlock.SetBase(AttributeId.DEX, characterStats.dexterity);
            characterStatBlock.SetBase(AttributeId.HP, characterStats.MaxHealth);
            characterStatBlock.SetBase(AttributeId.MP, characterStats.MaxMana);
            characterAttributes = new AttributeSystem(characterStatBlock, null);
            characterResources = new ResourcePool(characterStats.MaxHealth, characterStats.MaxMana);
            characterEntity = new CombatEntity(1, characterAttributes, new StatusSystem(), null, characterResources);

            var enemyStatBlock = new StatBlock();
            enemyStatBlock.SetBase(AttributeId.ATK, enemyStats.attack);
            enemyStatBlock.SetBase(AttributeId.DEF, enemyStats.defense);
            enemyStatBlock.SetBase(AttributeId.DEX, enemyStats.dexterity);
            enemyStatBlock.SetBase(AttributeId.HP, enemyStats.maxHealth);
            enemyStatBlock.SetBase(AttributeId.MP, enemyStats.maxMana);
            var enemyAttributes = new AttributeSystem(enemyStatBlock, null);
            var enemyResources = new ResourcePool(enemyStats.maxHealth, enemyStats.maxMana);
            enemyEntity = new CombatEntity(2, enemyAttributes, new StatusSystem(), null, enemyResources);

            combatSystem = new CombatSystem(
                new CombatEvents(),
                new AbilityEffectProc(null, null));
            abilityRunner = new AbilityRunner(characterEntity);
            enemyHealth = enemyResources.CurrentHP;
            eventLog.Clear();
            AddLog("Runtime initialized.");
        }

    private void CastTenDamageAbility()
        {
            if (activeTenDamageAbility == null)
            {
                AddLog("Assign Ability_ActiveTenDamage_MVP in the Inspector.");
                return;
            }

            if (characterEntity == null || enemyEntity == null || abilityRunner == null)
            {
                AddLog("Assign Character Stats and Enemy Stats, then press Reset.");
                return;
            }

            if (!abilityRunner.CanUse(activeTenDamageAbility))
            {
                AddLog($"Cannot cast. Current MP: {characterResources.CurrentMP:0}.");
                return;
            }

            var enemyHPBefore = enemyEntity.Resources.CurrentHP;
            var manaBefore = characterResources.CurrentMP;
            var intent = abilityRunner.UseAndCreateIntent(activeTenDamageAbility, Vector3.zero);
            if (intent == null)
            {
                AddLog("Ability cast failed.");
                return;
            }

            var result = combatSystem.HandleHit(new HitEvent(characterEntity, enemyEntity, Vector2.zero, intent));
            enemyHealth = enemyEntity.Resources.CurrentHP;
            var actualDamage = enemyHPBefore - enemyHealth;
            AddLog($"Cast {activeTenDamageAbility.abilityId}: damage {actualDamage:0}, MP {manaBefore:0} -> {characterResources.CurrentMP:0}, enemy HP {enemyHPBefore:0} -> {enemyHealth:0}.");
            Debug.Log($"[CharacterSystemTest] Ability result: {result.finalDamage:0} damage.");
        }

    private void CastHealTenAbility()
    {
        if (healTenAbility == null)
        {
            AddLog("Assign Ability_Heal10 in the Inspector.");
            return;
        }

        if (characterEntity == null || abilityRunner == null)
        {
            AddLog("Assign Character Stats and Enemy Stats, then press Reset.");
            return;
        }

        if (!abilityRunner.CanUse(healTenAbility))
        {
            AddLog($"Cannot cast heal. Current MP: {characterResources.CurrentMP:0}.");
            return;
        }

        var hpBefore = characterResources.CurrentHP;
        var manaBefore = characterResources.CurrentMP;
        var intent = abilityRunner.UseAndCreateIntent(healTenAbility, Vector3.zero);
        if (intent == null)
        {
            AddLog("Heal cast failed.");
            return;
        }

        combatSystem.HandleHit(new HitEvent(characterEntity, enemyEntity, Vector2.zero, intent));
        AddLog($"Cast {healTenAbility.abilityId}: healed {characterResources.CurrentHP - hpBefore:0}, MP {manaBefore:0} -> {characterResources.CurrentMP:0}.");
    }

    private void TakeCharacterDamage()
    {
        if (characterResources == null || characterAttributes == null)
        {
            AddLog("Assign Character Stats, then press Reset Runtime.");
            return;
        }

        var hpBefore = characterResources.CurrentHP;
        characterResources.ApplyHPDelta(-50f);
        characterResources.ClampToMax(characterAttributes);
        AddLog($"Character took {hpBefore - characterResources.CurrentHP:0} damage. HP {hpBefore:0} -> {characterResources.CurrentHP:0}.");
    }

    private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20f, 20f, 520f, 520f));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Ability Test");
            GUILayout.Label(characterStats != null && characterResources != null
                ? $"Character HP {characterResources.CurrentHP:0}/{characterStats.MaxHealth:0} | MP {characterResources.CurrentMP:0}/{characterStats.MaxMana:0}"
                : "Character Stats is not assigned.");
            GUILayout.Label(enemyStats != null
                ? $"Enemy Max HP {enemyStats.maxHealth:0} | DEF {enemyStats.defense:0}"
                : "Enemy Stats is not assigned.");
            GUILayout.Label(activeTenDamageAbility != null
                ? $"Ability {activeTenDamageAbility.abilityId} | Cost {activeTenDamageAbility.manaCost.baseValue:0} MP | Damage {activeTenDamageAbility.damage.baseValue:0}"
                : "Ability_ActiveTenDamage_MVP is not assigned.");
            GUILayout.Label(healTenAbility != null
                ? $"Heal {healTenAbility.abilityId} | Amount {GetHealAmount():0}"
                : "Ability_Heal10 is not assigned.");

            GUILayout.Space(8f);
            GUILayout.Label($"Current MP: {(characterResources != null ? characterResources.CurrentMP : 0f):0}");
            GUILayout.Label($"Enemy HP: {(enemyEntity != null ? enemyEntity.Resources.CurrentHP : 0f):0}");

            if (GUILayout.Button("Cast Ability", GUILayout.Height(40f)))
                CastTenDamageAbility();
            if (GUILayout.Button("Cast Heal Ability", GUILayout.Height(40f)))
                CastHealTenAbility();
            if (GUILayout.Button("Take 50 Damage", GUILayout.Height(40f)))
                TakeCharacterDamage();
            if (GUILayout.Button("Reset Runtime", GUILayout.Height(32f)))
                BuildRuntime();

            GUILayout.Space(8f);
            GUILayout.Label("Event Log");
            foreach (var message in eventLog)
                GUILayout.Label(message);
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

    private void AddLog(string message)
    {
        eventLog.Insert(0, message);
        if (eventLog.Count > 6)
            eventLog.RemoveAt(eventLog.Count - 1);
    }

    private float GetHealAmount()
    {
        if (healTenAbility == null || healTenAbility.onCast == null) return 0f;
        for (int i = 0; i < healTenAbility.onCast.Length; i++)
        {
            var effect = healTenAbility.onCast[i];
            if (effect != null && effect.action == AbilityOnCastActionKind.ResourceDelta && effect.resource == AbilityResourceType.HP)
            {
                var multiplier = 1f;
                if (healTenAbility.supportLinks != null)
                {
                    for (int supportIndex = 0; supportIndex < healTenAbility.supportLinks.Length; supportIndex++)
                    {
                        var support = healTenAbility.supportLinks[supportIndex];
                        if (support != null && support.Matches(healTenAbility))
                            multiplier += support.GetModifierValue(AbilitySupportModifierType.HealMultiplier);
                    }
                }

                return effect.amount.baseValue * Mathf.Max(0f, multiplier);
            }
        }
        return 0f;
    }
}