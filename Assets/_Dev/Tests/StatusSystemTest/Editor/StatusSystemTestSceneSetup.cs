using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TTH.Combat.Tests.Editor
{
    /// <summary>
    /// Quick menu to create StatusSystem test scene
    /// Right-click in Project and use TTH/Tests/Create StatusSystem Test Scene
    /// </summary>
    public class StatusSystemTestSceneSetup
    {
        [MenuItem("TTH/Tests/Create StatusSystem Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create test runner GameObject
            var testObj = new GameObject("StatusSystemTest");
            testObj.AddComponent<StatusSystemTestRunner>();

            // Save scene
            string scenePath = "Assets/_Dev/Tests/StatusSystemTest/StatusSystemTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[StatusSystemTest] Scene created at {scenePath}. Open it and press Play to test!");
            Debug.Log("[StatusSystemTest] Test methods: TestAddStatus(), TestAddMultiple(), TestRefreshDuration(), TestRemove(), TestClearAll()");
        }
    }
}
