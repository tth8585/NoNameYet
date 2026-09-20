using System.Collections.Generic;
using UnityEngine;
using TTH.Combat.Ability;
using TTH.Combat.Attributes;
using TTH.Combat.Effects;
using TTH.Combat.Runtime;
using TTH.Combat.Status;

public sealed class CharacterSystemTest : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO characterStats;
    [SerializeField] private EnemyStatsSO enemyStats;
    [SerializeField] private AbilityDefinition healAbility;
    [SerializeField] private RelicDefinitionSO[] equippedRelics;
    [SerializeField] private RelicDefinitionSO[] enemyRelics;

    private AttributeSystem attributes;
    private ResourcePool resources;
    private CombatEntity playerEntity;
    private CombatEntity enemyEntity;
    private CombatSystem combatSystem;
    private AbilityRunner abilityRunner;
    private float enemyHealth;
    private readonly List<RelicRuntime> relicRuntimes = new();
    private readonly List<RelicRuntime> enemyRelicRuntimes = new();
    private int turnNumber;
    private readonly List<string> eventLog = new();
    private Vector2 scrollPosition;
    private GUIStyle titleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle bodyStyle;
    private GUIStyle statusStyle;

    private float MaxHealth => characterStats != null ? characterStats.MaxHealth : 0f;
    private float MaxMana => characterStats != null ? characterStats.MaxMana : 0f;
    private float ActionSpeed => characterStats != null ? characterStats.ActionSpeed : 0f;

    private void Awake()
    {
        BuildCharacter();
    }

    private void BuildCharacter()
    {
        if (characterStats == null)
        {
            Debug.LogError("[CharacterSystemTest] Character Stats asset is not assigned.");
            return;
        }

        if (enemyStats == null)
        {
            Debug.LogError("[CharacterSystemTest] Enemy Stats asset is not assigned.");
            return;
        }

        var stats = new StatBlock();
        stats.SetBase(AttributeId.ATK, characterStats.baseAttack);
        stats.SetBase(AttributeId.DEF, characterStats.baseDefense);
        stats.SetBase(AttributeId.DEX, characterStats.dexterity);
        stats.SetBase(AttributeId.HP, MaxHealth);
        stats.SetBase(AttributeId.MP, MaxMana);
        attributes = new AttributeSystem(stats, null);
        resources = new ResourcePool(MaxHealth, MaxMana);
        playerEntity = new CombatEntity(1, attributes, new StatusSystem(), null, resources);

        var enemyStatBlock = new StatBlock();
        enemyStatBlock.SetBase(AttributeId.HP, enemyStats.maxHealth);
        enemyStatBlock.SetBase(AttributeId.MP, enemyStats.maxMana);
        enemyStatBlock.SetBase(AttributeId.ATK, enemyStats.attack);
        enemyStatBlock.SetBase(AttributeId.DEF, enemyStats.defense);
        enemyStatBlock.SetBase(AttributeId.DEX, enemyStats.dexterity);
        var enemyAttributes = new AttributeSystem(enemyStatBlock, null);
        var enemyResources = new ResourcePool(enemyStats.maxHealth, enemyStats.maxMana);
        enemyEntity = new CombatEntity(2, enemyAttributes, new StatusSystem(), null, enemyResources);

        enemyHealth = enemyStats.maxHealth;
        BuildRelicRuntimes(equippedRelics, relicRuntimes);
        BuildRelicRuntimes(enemyRelics, enemyRelicRuntimes);
        var passiveProc = new PassiveAbilityCombatProc();
        passiveProc.Register(playerEntity, relicRuntimes);
        passiveProc.Register(enemyEntity, enemyRelicRuntimes);
        var abilityProc = new AbilityEffectProc(new EffectSystemLite(), _ => null);
        combatSystem = new CombatSystem(new CombatEvents(), new CompositeCombatProc(passiveProc, abilityProc));
        passiveProc.BindCombatSystem(combatSystem);
        abilityRunner = new AbilityRunner(playerEntity);
        turnNumber = 1;
        eventLog.Clear();
        AddLog($"Character initialized with {relicRuntimes.Count} equipped relic(s).");
        AddLog("DEX affects action speed only; ATK and DEF remain independent.");
    }

    private void ReceiveDamage(float incomingDamage)
    {
        var before = resources.CurrentHP;
        combatSystem.HandleHit(new HitEvent(
            enemyEntity,
            playerEntity,
            Vector2.zero,
            new CharacterTestAttack(incomingDamage)));
        var actualDamage = before - resources.CurrentHP;
        if (actualDamage > 0f)
        {
            AddLog($"Received {actualDamage:0} damage. Relic is READY.");
        }
        else
        {
            AddLog("Received 0 damage. Relic did not trigger.");
        }
    }

    private void DealDamage(float multiplier, string actionName)
    {
        var baseDamage = attributes.Get(AttributeId.ATK) * multiplier;
        var result = combatSystem.HandleHit(new HitEvent(
            playerEntity,
            enemyEntity,
            Vector2.zero,
            new CharacterTestAttack(baseDamage)));
        enemyHealth = enemyEntity.Resources.CurrentHP;
        AddLog($"{actionName}: {result.finalDamage:0} damage" + (result.baseDamage > baseDamage ? " (passive consumed)." : "."));
    }

    private void Heal()
    {
        var before = resources.CurrentHP;
        resources.ApplyHPDelta(20f);
        resources.ClampToMax(attributes);
        AddLog($"Healed {resources.CurrentHP - before:0}. Healing does not consume the relic.");
    }

    private void CastHealAbility()
    {
        if (healAbility == null)
        {
            AddLog("Assign a Heal Ability asset first.");
            return;
        }

        if (!abilityRunner.CanUse(healAbility))
        {
            AddLog("Heal ability is on cooldown.");
            return;
        }

        var before = resources.CurrentHP;
        var intent = abilityRunner.UseAndCreateIntent(healAbility, Vector3.zero);
        var result = combatSystem.HandleHit(new HitEvent(
            playerEntity,
            enemyEntity,
            Vector2.zero,
            intent));

        AddLog($"{healAbility.abilityId}: healed {resources.CurrentHP - before:0} HP; combat resolved {result.finalDamage:0} damage.");
    }

    private void BeginTurn()
    {
        turnNumber++;
        var before = resources.CurrentHP;
        var enemyHealthBefore = enemyEntity.Resources.CurrentHP;
        combatSystem.HandleTurnStart(enemyEntity);
        combatSystem.HandleTurnStart(playerEntity);
        enemyHealth = enemyEntity.Resources.CurrentHP;
        AddLog($"Turn {turnNumber}: player healed {resources.CurrentHP - before:0} HP; enemy lost {enemyHealthBefore - enemyHealth:0} HP.");
    }

    private void RunAssertions()
    {
        var passed = 0;
        var failed = 0;
        Check("STR -> Max HP", Mathf.Approximately(attributes.Get(AttributeId.HP), characterStats.MaxHealth), ref passed, ref failed);
        Check("INT -> Max MP", Mathf.Approximately(attributes.Get(AttributeId.MP), characterStats.MaxMana), ref passed, ref failed);
        Check("DEX -> Action Speed", Mathf.Approximately(ActionSpeed, characterStats.dexterity), ref passed, ref failed);
        Check("ATK independent from DEX", Mathf.Approximately(attributes.Get(AttributeId.ATK), characterStats.baseAttack), ref passed, ref failed);
        Check("DEF independent from DEX", Mathf.Approximately(attributes.Get(AttributeId.DEF), characterStats.baseDefense), ref passed, ref failed);
        Check("Two player relics are equipped", relicRuntimes.Count == 2, ref passed, ref failed);
        Check("Enemy has Venomous Retaliation", enemyRelicRuntimes.Count == 1, ref passed, ref failed);
        Check("Relics start inactive", AllRelicsInactive(), ref passed, ref failed);
        ReceiveDamage(15f);
        Check("Taking real damage readies Revenge", relicRuntimes[0].IsPassiveReady, ref passed, ref failed);
        var healthAfterDamage = resources.CurrentHP;
        var enemyHealthBeforePoison = enemyHealth;
        DealDamage(1f, "Retaliation test attack");
        Check("Revenge doubles the retaliation test attack", Mathf.Approximately(enemyHealthBeforePoison - enemyHealth, characterStats.baseAttack * 2f), ref passed, ref failed);
        Check("Revenge is consumed once", AllRelicsInactive(), ref passed, ref failed);
        BeginTurn();
        Check("Turn heal offsets venom damage", Mathf.Approximately(resources.CurrentHP, healthAfterDamage), ref passed, ref failed);
        Check("Enemy stats receive player damage", enemyHealthBeforePoison - enemyHealth > 0f, ref passed, ref failed);
        DealDamage(1f, "Assertion attack");
        AddLog($"Assertions: {passed} passed, {failed} failed.");
    }

    private static void Check(string label, bool condition, ref int passed, ref int failed)
    {
        if (condition)
        {
            passed++;
            Debug.Log($"[CharacterSystemTest] PASS: {label}");
        }
        else
        {
            failed++;
            Debug.LogError($"[CharacterSystemTest] FAIL: {label}");
        }
    }

    private static void BuildRelicRuntimes(IReadOnlyList<RelicDefinitionSO> definitions, List<RelicRuntime> runtimes)
    {
        runtimes.Clear();
        if (definitions == null) return;

        foreach (var relic in definitions)
        {
            if (relic != null) runtimes.Add(new RelicRuntime(relic));
        }
    }

    private void AddLog(string message)
    {
        eventLog.Insert(0, message);
        if (eventLog.Count > 8) eventLog.RemoveAt(eventLog.Count - 1);
    }

    private bool AllRelicsInactive()
    {
        foreach (var relicRuntime in relicRuntimes)
        {
            if (relicRuntime.IsPassiveReady) return false;
        }

        return true;
    }

    private void OnGUI()
    {
        if (attributes == null || resources == null) BuildCharacter();
        if (attributes == null || resources == null) return;
        EnsureStyles();
        var panelWidth = Mathf.Min(Screen.width - 40f, 900f);
        GUILayout.BeginArea(new Rect(20f, 20f, panelWidth, Screen.height - 40f));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(panelWidth), GUILayout.Height(Screen.height - 40f));
        GUILayout.BeginVertical("box");
        GUILayout.Label("CHARACTER SYSTEM MVP TEST", titleStyle);
        GUILayout.Label("One character / three primary stats / multiple relics", bodyStyle);
        GUILayout.EndVertical();

        DrawStats();
        DrawRelic();
        DrawControls();
        DrawLog();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawStats()
    {
        GUILayout.Label("Character Stats", sectionStyle);
        GUILayout.BeginVertical("box");
        GUILayout.Label($"STR  {characterStats.strength:0}   ->   Max HP  {attributes.Get(AttributeId.HP):0}", bodyStyle);
        GUILayout.Label($"INT  {characterStats.intelligence:0}   ->   Max MP  {attributes.Get(AttributeId.MP):0}", bodyStyle);
        GUILayout.Label($"DEX  {characterStats.dexterity:0}   ->   Action Speed  {ActionSpeed:0}", bodyStyle);
        GUILayout.Space(4);
        GUILayout.Label($"ATK  {attributes.Get(AttributeId.ATK):0}   (independent from DEX)", bodyStyle);
        GUILayout.Label($"DEF  {attributes.Get(AttributeId.DEF):0}   (independent from DEX)", bodyStyle);
        GUILayout.Label($"Turn {turnNumber}      HP   {resources.CurrentHP:0} / {attributes.Get(AttributeId.HP):0}      MP   {resources.CurrentMP:0} / {attributes.Get(AttributeId.MP):0}", bodyStyle);
        GUILayout.Label($"Enemy HP   {enemyHealth:0} / {enemyStats.maxHealth:0}", bodyStyle);
        GUILayout.EndVertical();
    }

    private void DrawRelic()
    {
        GUILayout.Label($"Equipped Relics ({relicRuntimes.Count})", sectionStyle);
        GUILayout.BeginVertical("box");
        foreach (var relicRuntime in relicRuntimes)
        {
            var definition = relicRuntime.Definition;
            GUILayout.Label(definition.displayName, bodyStyle);
            GUILayout.Label(definition.description, bodyStyle);
            GUILayout.Label(relicRuntime.IsPassiveReady ? "PASSIVE: READY" : "PASSIVE: INACTIVE", statusStyle);
            GUILayout.Label(relicRuntime.HasActiveSkill ? "ACTIVE SKILL: AVAILABLE" : "ACTIVE SKILL: NONE", bodyStyle);
        }
        if (relicRuntimes.Count == 0) GUILayout.Label("No relics assigned.", bodyStyle);
        GUILayout.EndVertical();
    }

    private void DrawControls()
    {
        GUILayout.Label("Manual Checks", sectionStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Receive 15 Damage", GUILayout.Height(36))) ReceiveDamage(15f);
        if (GUILayout.Button("Basic Attack", GUILayout.Height(36))) DealDamage(1f, "Basic Attack");
        if (GUILayout.Button("Heavy Attack", GUILayout.Height(36))) DealDamage(2f, "Heavy Attack");
        if (GUILayout.Button("Next Turn", GUILayout.Height(36))) BeginTurn();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Heal 20", GUILayout.Height(32))) Heal();
        if (GUILayout.Button("Cast Heal Ability", GUILayout.Height(32))) CastHealAbility();
        if (GUILayout.Button("Run Assertions", GUILayout.Height(32))) RunAssertions();
        if (GUILayout.Button("Reset", GUILayout.Height(32))) BuildCharacter();
        GUILayout.EndHorizontal();
    }

    private void DrawLog()
    {
        GUILayout.Label("Event Log", sectionStyle);
        GUILayout.BeginVertical("box");
        foreach (var message in eventLog) GUILayout.Label(message, bodyStyle);
        GUILayout.EndVertical();
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
        statusStyle = new GUIStyle(bodyStyle) { fontStyle = FontStyle.Bold };
    }
}