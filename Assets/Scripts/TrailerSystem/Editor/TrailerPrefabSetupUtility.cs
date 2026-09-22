#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    public static class TrailerPrefabSetupUtility
    {
        private const string Sockets = "Sockets";
        private const string CargoSocket = "CargoSocket";
        private const string Kingpin = "Kingpin";
        private static readonly string[] WheelNames =
        {
            "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"
        };

        [MenuItem("Ultimate Truck Empire/Trailer System/Setup Selected Trailer Prefab")]
        public static void SetupSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogWarning("[TrailerSystem] Select a trailer prefab asset or prefab root first.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(selected);
            if (!PrefabUtility.IsPartOfPrefabAsset(selected) || string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[TrailerSystem] Selection must be a prefab asset.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                EnsureComponent<LoadedTrailer>(root);
                EnsureComponent<TrailerCargoModule>(root);
                EnsureComponent<TrailerSkinApplier>(root);

                Transform sockets = FindDirectChild(root.transform, Sockets) ??
                                    CreateChild(root.transform, Sockets);
                EnsureChild(sockets, CargoSocket);
                EnsureChild(sockets, Kingpin);

                for (int i = 0; i < WheelNames.Length; i++)
                    EnsureChild(sockets, WheelNames[i]);

                if (root.GetComponentInChildren<LODGroup>(true) == null)
                    Debug.LogWarning("[TrailerSystem] " + root.name +
                        " has no LODGroup. Author LOD0/LOD1/LOD2 before shipping.", root);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SaveAssets();
                Debug.Log("[TrailerSystem] Trailer prefab contract components and socket placeholders configured: " + path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T EnsureComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = FindDirectChild(parent, name);
            return child != null ? child : CreateChild(parent, name);
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, "Create trailer socket");
            child.transform.SetParent(parent, false);
            return child.transform;
        }
    }
}
#endif
