using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TTH.Combat.Tests.Editor
{
    /// <summary>
    /// Quick menu to create CombatSystem test scene
    /// </summary>
    public class CombatSystemTestSceneSetup
    {
        [MenuItem("TTH/Tests/Create CombatSystem Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create test runner GameObject
            var testObj = new GameObject("CombatSystemTest");
            testObj.AddComponent<CombatSystemTestRunner>();

            // Save scene
            string scenePath = "Assets/_Dev/Tests/CombatSystemTest/CombatSystemTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[CombatSystemTest] Scene created at {scenePath}. Open it and press Play to test!");
            Debug.Log("[CombatSystemTest] Test methods: TestBasicHit(), TestMultipleHits(), TestHitDeadTarget(), TestHitNullAttacker(), TestDamageVariation()");
        }
    }
}
