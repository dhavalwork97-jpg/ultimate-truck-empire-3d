#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateTruckEmpire.EditorTools
{
    public static class BootstrapSceneBuilder
    {
        [MenuItem("Ultimate Truck Empire/Build Phase 1 World")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("WorldBootstrap");
            bootstrap.AddComponent<UltimateTruckEmpire.Bootstrap.WorldBootstrap>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/World_Gujarat.unity");
            AssetDatabase.Refresh();
            Debug.Log("Ultimate Truck Empire Phase 1 world created. Open World_Gujarat and press Play.");
        }
    }
}
#endif
