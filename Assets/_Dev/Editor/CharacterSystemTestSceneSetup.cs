using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CharacterSystemTestSceneSetup
{
    private const string StatsAssetPath = "Assets/_Game/Sandbox_Test/CharacterStats_MVP.asset";
    private const string EnemyStatsAssetPath = "Assets/_Game/Sandbox_Test/EnemyStats_MVP.asset";
    private const string RelicAssetPath = "Assets/_Game/Sandbox_Test/Relic_Revenge_MVP.asset";
    private const string RegenRelicAssetPath = "Assets/_Game/Sandbox_Test/Relic_TurnHeal_MVP.asset";
    private const string VenomousRelicAssetPath = "Assets/_Game/Sandbox_Test/Relic_VenomousRetaliation_MVP.asset";
    private const string RevengePassiveAssetPath = "Assets/_Game/Sandbox_Test/Passive_Revenge_MVP.asset";
    private const string TurnHealPassiveAssetPath = "Assets/_Game/Sandbox_Test/Passive_TurnHeal_MVP.asset";
    private const string VenomousPassiveAssetPath = "Assets/_Game/Sandbox_Test/Passive_VenomousRetaliation_MVP.asset";

    private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        return asset;
    }

    [MenuItem("TTH/Tests/Create Default Enemy Stats Asset")]
    public static void CreateDefaultEnemyStatsAsset()
    {
        var asset = GetOrCreateAsset<EnemyStatsSO>(EnemyStatsAssetPath);
        asset.maxHealth = 200f;
        asset.maxMana = 0f;
        asset.attack = 20f;
        asset.defense = 0f;
        asset.dexterity = 10f;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
    }

    [MenuItem("TTH/Tests/Migrate Relics To Passive Definitions")]
    public static void MigrateRelicsToPassiveDefinitions()
    {
        var revengePassive = GetOrCreateAsset<NextDamageMultiplierPassiveSO>(RevengePassiveAssetPath);
        revengePassive.multiplier = 2f;

        var turnHealPassive = GetOrCreateAsset<HealOnTurnStartPassiveSO>(TurnHealPassiveAssetPath);
        turnHealPassive.healAmount = 5f;

        var revengeRelic = GetOrCreateAsset<RelicDefinitionSO>(RelicAssetPath);
        revengeRelic.relicId = "revenge_relic";
        revengeRelic.displayName = "Revenge Relic";
        revengeRelic.description = "After taking real damage, the next damage action deals x2.";
        revengeRelic.passiveAbility = revengePassive;

        var turnHealRelic = GetOrCreateAsset<RelicDefinitionSO>(RegenRelicAssetPath);
        turnHealRelic.relicId = "turn_heal_relic";
        turnHealRelic.displayName = "Vital Spring Relic";
        turnHealRelic.description = "At the start of each turn, restore 5 HP.";
        turnHealRelic.passiveAbility = turnHealPassive;

        var venomousPassive = GetOrCreateAsset<VenomousRetaliationPassiveSO>(VenomousPassiveAssetPath);
        venomousPassive.poisonDamagePerTurn = 5f;
        venomousPassive.poisonDurationTurns = 1;

        var venomousRelic = GetOrCreateAsset<RelicDefinitionSO>(VenomousRelicAssetPath);
        venomousRelic.relicId = "venomous_retaliation_relic";
        venomousRelic.displayName = "Venomous Retaliation";
        venomousRelic.description = "After taking real damage, poison the attacker for 5 damage next turn.";
        venomousRelic.passiveAbility = venomousPassive;

        EditorUtility.SetDirty(revengePassive);
        EditorUtility.SetDirty(turnHealPassive);
        EditorUtility.SetDirty(revengeRelic);
        EditorUtility.SetDirty(turnHealRelic);
        EditorUtility.SetDirty(venomousPassive);
        EditorUtility.SetDirty(venomousRelic);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterSystemTest] Relic passive migration complete.");
    }

    [MenuItem("TTH/Tests/Create Default Revenge Relic Asset")]
    public static void CreateDefaultRevengeRelicAsset()
    {
        var asset = GetOrCreateAsset<RelicDefinitionSO>(RelicAssetPath);
        var passive = GetOrCreateAsset<NextDamageMultiplierPassiveSO>(RevengePassiveAssetPath);
        passive.multiplier = 2f;
        asset.relicId = "revenge_relic";
        asset.displayName = "Revenge Relic";
        asset.description = "After taking real damage, the next damage action deals x2.";
        asset.passiveAbility = passive;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        Selection.activeObject = asset;
        Debug.Log($"[CharacterSystemTest] Revenge relic asset ready at {RelicAssetPath}.");
    }

    [MenuItem("TTH/Tests/Create Default Turn Heal Relic Asset")]
    public static void CreateDefaultTurnHealRelicAsset()
    {
        var asset = GetOrCreateAsset<RelicDefinitionSO>(RegenRelicAssetPath);
        var passive = GetOrCreateAsset<HealOnTurnStartPassiveSO>(TurnHealPassiveAssetPath);
        passive.healAmount = 5f;
        asset.relicId = "turn_heal_relic";
        asset.displayName = "Vital Spring Relic";
        asset.description = "At the start of each turn, restore 5 HP.";
        asset.passiveAbility = passive;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        Selection.activeObject = asset;
        Debug.Log($"[CharacterSystemTest] Turn heal relic asset ready at {RegenRelicAssetPath}.");
    }

    [MenuItem("TTH/Tests/Create Default Character Stats Asset")]
    public static void CreateDefaultCharacterStatsAsset()
    {
        var asset = AssetDatabase.LoadAssetAtPath<CharacterStatsSO>(StatsAssetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CharacterStatsSO>();
            AssetDatabase.CreateAsset(asset, StatsAssetPath);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = asset;
        Debug.Log($"[CharacterSystemTest] Character stats asset ready at {StatsAssetPath}.");
    }

    [MenuItem("TTH/Tests/Create Character System MVP Test Scene")]
    public static void CreateTestScene()
    {
        CreateDefaultCharacterStatsAsset();
        CreateDefaultEnemyStatsAsset();
        CreateDefaultRevengeRelicAsset();
        CreateDefaultTurnHealRelicAsset();
        MigrateRelicsToPassiveDefinitions();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var testObject = new GameObject("CharacterSystemTest");
        var test = testObject.AddComponent<CharacterSystemTest>();
        var stats = AssetDatabase.LoadAssetAtPath<CharacterStatsSO>(StatsAssetPath);
        var enemyStats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(EnemyStatsAssetPath);
        var relic = AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(RelicAssetPath);
        var regenRelic = AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(RegenRelicAssetPath);
        var venomousRelic = AssetDatabase.LoadAssetAtPath<RelicDefinitionSO>(VenomousRelicAssetPath);
        var serializedTest = new SerializedObject(test);
        serializedTest.FindProperty("characterStats").objectReferenceValue = stats;
        serializedTest.FindProperty("enemyStats").objectReferenceValue = enemyStats;
        var equippedRelics = serializedTest.FindProperty("equippedRelics");
        equippedRelics.arraySize = 2;
        equippedRelics.GetArrayElementAtIndex(0).objectReferenceValue = relic;
        equippedRelics.GetArrayElementAtIndex(1).objectReferenceValue = regenRelic;
        var enemyRelics = serializedTest.FindProperty("enemyRelics");
        enemyRelics.arraySize = 1;
        enemyRelics.GetArrayElementAtIndex(0).objectReferenceValue = venomousRelic;
        serializedTest.ApplyModifiedPropertiesWithoutUndo();
        var scenePath = "Assets/_Game/Sandbox_Test/CharacterSystemTest.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Selection.activeGameObject = testObject;
        Debug.Log($"[CharacterSystemTest] Scene created at {scenePath}.");
    }
}