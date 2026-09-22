#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    public static class TrailerPrefabContractSetup
    {
        [MenuItem("Ultimate Truck Empire/Trailer System/Apply Selected Prefab To Definition")]
        public static void ApplySelectedToDefinition()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[TrailerSystem] Select a trailer prefab asset first.");
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(selected);
            if (!PrefabUtility.IsPartOfPrefabAsset(selected) || string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogWarning("[TrailerSystem] Selection must be a prefab asset.");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:TrailerDefinition");
            TrailerDefinition match = null;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TrailerDefinition definition = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(path);
                if (definition != null && definition.prefab == selected)
                {
                    match = definition;
                    break;
                }
            }

            if (match == null)
            {
                Debug.LogWarning("[TrailerSystem] No TrailerDefinition references the selected prefab.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform cargo = Find(root.transform, "CargoSocket");
                Transform kingpin = Find(root.transform, "Kingpin");
                if (cargo == null || kingpin == null)
                {
                    Debug.LogError("[TrailerSystem] Required sockets are missing. Run Setup Selected Trailer Prefab first.");
                    return;
                }

                Undo.RecordObject(match, "Assign trailer prefab sockets");
                match.cargoSocket = cargo;
                match.kingpinSocket = kingpin;

                Transform sockets = Find(root.transform, "Sockets");
                if (sockets != null)
                {
                    Transform[] wheels = new Transform[4];
                    string[] names = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
                    for (int i = 0; i < names.Length; i++)
                        wheels[i] = FindDirectChild(sockets, names[i]);

                    match.wheelSockets = wheels;
                }

                EditorUtility.SetDirty(match);
                AssetDatabase.SaveAssets();
                Debug.Log("[TrailerSystem] Assigned prefab sockets to TrailerDefinition '" + match.id + "'.", match);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = Find(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }
    }
}
#endif
