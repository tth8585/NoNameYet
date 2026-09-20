using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CharacterSystemTestSceneSetup
{
    private const string StatsAssetPath = "Assets/_Game/Sandbox_Test/CharacterStats_MVP.asset";
    private const string EnemyStatsAssetPath = "Assets/_Game/Sandbox_Test/EnemyStats_MVP.asset";

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

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var testObject = new GameObject("CharacterSystemTest");
        var test = testObject.AddComponent<CharacterSystemTest>();
        var stats = AssetDatabase.LoadAssetAtPath<CharacterStatsSO>(StatsAssetPath);
        var enemyStats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(EnemyStatsAssetPath);
        var serializedTest = new SerializedObject(test);
        serializedTest.FindProperty("characterStats").objectReferenceValue = stats;
        serializedTest.FindProperty("enemyStats").objectReferenceValue = enemyStats;
        serializedTest.ApplyModifiedPropertiesWithoutUndo();

        var scenePath = "Assets/_Game/Sandbox_Test/CharacterSystemTest.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Selection.activeGameObject = testObject;
        Debug.Log($"[CharacterSystemTest] Scene created at {scenePath}.");
    }
}