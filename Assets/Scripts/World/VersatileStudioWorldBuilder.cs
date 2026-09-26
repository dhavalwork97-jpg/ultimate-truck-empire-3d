using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Reusable visual world kit backed by the Versatile Studio Demo City asset pack.
    /// Gameplay systems remain independent of this visual layer.
    /// </summary>
    public static class VersatileStudioWorldBuilder
    {
        private const string ResourceRoot = "VersatileStudioWorld/";
        private static readonly string[] CityPrefabs =
        {
            "demo_city_by_versatile_studio",
            "office_building_1", "office_building_2", "office_building_3", "office_building_4",
            "mid_house_1", "mid_house_1_2", "mid_house_2", "mid_house_3", "mid_house_4", "mid_house_4_2", "mid_house_5",
            "small_house_1", "small_house_2", "small_house_3",
            "factory_building_big", "factory_building_small",
            "tree_1", "tree_2", "tree_3",
            "lamp_pole_dual", "lamp_pole_single", "lamp_pole_small",
            "highway_support", "bench", "concrete_block", "building_lamp",
            "factory_chimney-stalk", "hedge_with_base"
        };

        private static readonly string[] MaterialNames =
        {
            "asphalt_2-5_tracks", "asphalt_2_tracks", "asphalt_4_tracks", "asphalt_extra",
            "asphalt_highway", "asphalt_square", "highway_wall", "road_sideway_fences",
            "sideway_tile", "building_bases", "building_facades", "building_interior",
            "building_windows_wet", "highrise_facades", "props_main", "vegetation_main", "vegetation_non_tp"
        };

        public static bool AssetsAvailable()
        {
            foreach (var name in CityPrefabs)
                if (Resources.Load<GameObject>(ResourceRoot + name) != null)
                    return true;
            return false;
        }

        public static bool Build(Transform parent)
        {
            if (!AssetsAvailable()) return false;

            var root = new GameObject("Demo City — Versatile Studio World Kit").transform;
            root.SetParent(parent, false);

            var baseCity = Load("demo_city_by_versatile_studio");
            if (baseCity != null)
            {
                var instance = UnityEngine.Object.Instantiate(baseCity, root);
                instance.name = "Demo City Base";
            }

            // Reuse the supplied kit across multiple city districts. The layout is
            // deliberately deterministic so gameplay/save data remains stable.
            PlaceDistrict(root, "Ahmedabad District", new Vector3(0f, 0f, 0f), 101, 1.0f);
            PlaceDistrict(root, "Gandhinagar District", new Vector3(-360f, 0f, 220f), 202, 0.82f);
            PlaceDistrict(root, "Vadodara District", new Vector3(390f, 0f, 210f), 303, 0.9f);
            PlaceDistrict(root, "Surat District", new Vector3(430f, 0f, -300f), 404, 0.86f);
            PlaceDistrict(root, "Rajkot District", new Vector3(-420f, 0f, -290f), 505, 0.78f);

            BuildHighwayFurniture(root);
            return true;
        }

        private static void PlaceDistrict(Transform root, string districtName, Vector3 origin, int seed, float scale)
        {
            var district = new GameObject(districtName).transform;
            district.SetParent(root, false);

            string[] buildings =
            {
                "office_building_1", "office_building_2", "office_building_3", "office_building_4",
                "mid_house_1", "mid_house_2", "mid_house_3", "mid_house_4", "mid_house_5",
                "small_house_1", "small_house_2", "small_house_3",
                "factory_building_big", "factory_building_small"
            };

            var rng = new System.Random(seed);
            for (int i = 0; i < 24; i++)
            {
                var prefab = Load(buildings[rng.Next(buildings.Length)]);
                if (prefab == null) continue;

                float x = ((i % 6) - 2.5f) * 55f + rng.Next(-12, 13);
                float z = ((i / 6) - 1.5f) * 52f + rng.Next(-10, 11);
                var go = UnityEngine.Object.Instantiate(prefab, district);
                go.name = prefab.name + "_KitInstance";
                go.transform.localPosition = new Vector3(x, 0f, z);
                go.transform.localRotation = Quaternion.Euler(0f, rng.Next(4) * 90f, 0f);
                go.transform.localScale = Vector3.one * scale * (0.92f + (float)rng.NextDouble() * 0.16f);
            }

            for (int i = 0; i < 18; i++)
            {
                var prefab = Load("tree_" + (1 + rng.Next(3)));
                if (prefab == null) continue;
                var go = UnityEngine.Object.Instantiate(prefab, district);
                go.name = prefab.name + "_KitInstance";
                go.transform.localPosition = new Vector3(rng.Next(-180, 181), 0f, rng.Next(-150, 151));
                go.transform.localScale = Vector3.one * (0.8f + (float)rng.NextDouble() * 0.45f) * scale;
            }

            for (int i = 0; i < 8; i++)
            {
                string lamp = i % 2 == 0 ? "lamp_pole_single" : "lamp_pole_dual";
                var prefab = Load(lamp);
                if (prefab == null) continue;
                var go = UnityEngine.Object.Instantiate(prefab, district);
                go.name = lamp + "_KitInstance";
                go.transform.localPosition = new Vector3(-165f + i * 47f, 0f, i % 2 == 0 ? -18f : 18f);
                go.transform.localRotation = Quaternion.Euler(0f, i % 2 == 0 ? 0f : 180f, 0f);
                go.transform.localScale = Vector3.one * scale;
            }
        }

        private static void BuildHighwayFurniture(Transform root)
        {
            var highway = new GameObject("Demo City Highway + Bridge Kit").transform;
            highway.SetParent(root, false);

            for (int i = -4; i <= 4; i++)
            {
                var prefab = Load("highway_support");
                if (prefab == null) break;
                var go = UnityEngine.Object.Instantiate(prefab, highway);
                go.name = "HighwaySupport_" + i;
                go.transform.localPosition = new Vector3(i * 90f, 0f, -105f);
            }
        }

        private static GameObject Load(string name)
        {
            return Resources.Load<GameObject>(ResourceRoot + name);
        }
    }
}
