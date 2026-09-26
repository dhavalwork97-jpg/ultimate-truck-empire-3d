#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.Editor
{
    /// <summary>
    /// Copies the purchased Asset Store prefabs into Resources so runtime builds
    /// can use them without hard-coded package paths.
    /// </summary>
    public static class VersatileStudioWorldSetup
    {
        private const string ResourceRoot = "Assets/Resources/VersatileStudioWorld";

        [MenuItem("Ultimate Truck Empire/World/Prepare Versatile Studio + Tenkoku Assets")]
        public static void Prepare()
        {
            Directory.CreateDirectory(ResourceRoot);

            string[] prefabNames =
            {
                "demo_city_by_versatile_studio",
                "factory_building_big", "factory_building_small",
                "office_building_1", "office_building_2", "office_building_3", "office_building_4",
                "mid_house_1", "mid_house_2", "mid_house_3", "mid_house_4", "mid_house_5",
                "small_house_1", "small_house_2", "small_house_3",
                "tree_1", "tree_2", "tree_3",
                "lamp_pole_dual_blue", "lamp_pole_dual_white",
                "lamp_pole_single_white", "lamp_pole_single_orange", "lamp_pole_small",
                "Tenkoku DynamicSky"
            };

            int copied = 0;
            for (int i = 0; i < prefabNames.Length; i++)
            {
                string source = FindPrefab(prefabNames[i]);
                if (string.IsNullOrEmpty(source))
                {
                    Debug.LogWarning("[VersatileStudioWorldSetup] Prefab not found after import: " + prefabNames[i]);
                    continue;
                }

                string destination = Path.Combine(ResourceRoot, prefabNames[i] + ".prefab").Replace("\\", "/");
                if (string.Equals(source, destination, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (AssetDatabase.CopyAsset(source, destination))
                {
                    copied++;
                    Debug.Log("[VersatileStudioWorldSetup] Prepared " + prefabNames[i] + " from " + source);
                }
                else if (File.Exists(destination))
                {
                    Debug.Log("[VersatileStudioWorldSetup] Already prepared: " + destination);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VersatileStudioWorldSetup] Preparation complete. Copied " + copied + " prefabs.");
        }

        private static string FindPrefab(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetFileNameWithoutExtension(path).Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return string.Empty;
        }
    }
}
#endif
