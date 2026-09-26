#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.Editor
{
    /// <summary>
    /// One-time importer for the purchased Versatile Studio Demo City pack.
    /// Search is filename-based so the Asset Store package may live anywhere
    /// under Assets/ without hard-coding a vendor folder.
    /// </summary>
    public static class VersatileStudioWorldSetup
    {
        private const string Destination = "Assets/Resources/VersatileStudioWorld";

        private static readonly string[] Prefabs =
        {
            "demo_city_by_versatile_studio", "factory_building_big", "factory_building_big_bgr",
            "factory_building_small", "factory_building_small_bgr", "factory_chimney-stalk",
            "office_building_1", "office_building_1_bgr", "office_building_2", "office_building_2_bgr",
            "office_building_3", "office_building_3_bgr", "office_building_4", "office_building_4_bgr",
            "mid_house_1", "mid_house_1_2", "mid_house_2", "mid_house_3", "mid_house_4", "mid_house_4_2",
            "mid_house_5", "small_house_1", "small_house_2", "small_house_3",
            "tree_1", "tree_2", "tree_3", "lamp_pole_dual", "lamp_pole_single", "lamp_pole_small",
            "highway_support", "bench", "building_lamp", "concrete_block", "hedge_with_base",
            "basement_big", "basement_mid", "basement_small"
        };

        private static readonly string[] Materials =
        {
            "asphalt_2-5_tracks", "asphalt_2_tracks", "asphalt_4_tracks", "asphalt_extra",
            "asphalt_highway", "asphalt_square", "blue_light", "building_bases", "building_facades",
            "building_interior", "building_windows_wet", "highrise_facades", "highway_wall",
            "orange_light", "pink_light", "props_main", "road_sideway_fences", "sideway_tile",
            "vegetation_main", "vegetation_non_tp", "white_light"
        };

        [MenuItem("Ultimate Truck Empire/World/Import Versatile Studio Demo City Kit")]
        public static void Import()
        {
            Directory.CreateDirectory(Destination);
            AssetDatabase.Refresh();

            int copied = 0;
            foreach (var prefab in Prefabs)
                copied += CopyFirstAsset(prefab, "t:Prefab");
            foreach (var material in Materials)
                copied += CopyFirstAsset(material, "t:Material");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Truck Empire] Demo City kit import complete. Copied {copied} assets into {Destination}.");
        }

        private static int CopyFirstAsset(string fileName, string filter)
        {
            string[] guids = AssetDatabase.FindAssets(fileName + " " + filter);
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[Truck Empire] Demo City asset not found: {fileName}");
                return 0;
            }

            string source = AssetDatabase.GUIDToAssetPath(guids[0]);
            string extension = Path.GetExtension(source);
            string destination = Destination + "/" + fileName + extension;

            if (string.Equals(source, destination, System.StringComparison.OrdinalIgnoreCase))
                return 0;

            if (File.Exists(destination))
                AssetDatabase.DeleteAsset(destination);

            if (!AssetDatabase.CopyAsset(source, destination))
            {
                Debug.LogWarning($"[Truck Empire] Could not copy {source} -> {destination}");
                return 0;
            }

            return 1;
        }
    }
}
#endif
