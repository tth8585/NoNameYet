using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TTH.Combat.Tests.Editor
{
    /// <summary>
    /// Quick menu to create AttributeSystem test scene
    /// Right-click in Project and use TTH/Tests/Create AttributeSystem Test Scene
    /// </summary>
    public class AttributeSystemTestSceneSetup
    {
        [MenuItem("TTH/Tests/Create AttributeSystem Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create test runner GameObject
            var testObj = new GameObject("AttributeSystemTest");
            testObj.AddComponent<AttributeSystemTestRunner>();

            // Save scene
            string scenePath = "Assets/_Dev/Tests/AttributeSystemTest/AttributeSystemTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[AttributeSystemTest] Scene created at {scenePath}. Open it and press Play to test!");
            Debug.Log("[AttributeSystemTest] Test methods: TestAddModifier(), TestMultiplyModifier(), TestOverride(), TestCapHitting(), TestStackingPolicy()");
        }
    }
}
