using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Presentation layer backed by Versatile Studio's Demo City assets.
    /// The existing RoadNetwork/RoadBuilder remains authoritative for drivable
    /// surfaces, triggers and collision. Imported asset colliders are removed
    /// from runtime instances so the art pack cannot break gameplay.
    /// </summary>
    public static class VersatileStudioWorldBuilder
    {
        private const string DemoCityPath = "VersatileStudioWorld/demo_city_by_versatile_studio";

        private static readonly string[] BuildingPaths =
        {
            "VersatileStudioWorld/factory_building_big",
            "VersatileStudioWorld/factory_building_small",
            "VersatileStudioWorld/office_building_1",
            "VersatileStudioWorld/office_building_2",
            "VersatileStudioWorld/office_building_3",
            "VersatileStudioWorld/office_building_4",
            "VersatileStudioWorld/mid_house_1",
            "VersatileStudioWorld/mid_house_2",
            "VersatileStudioWorld/mid_house_3",
            "VersatileStudioWorld/mid_house_4",
            "VersatileStudioWorld/mid_house_5",
            "VersatileStudioWorld/small_house_1",
            "VersatileStudioWorld/small_house_2",
            "VersatileStudioWorld/small_house_3"
        };

        private static readonly string[] TreePaths =
        {
            "VersatileStudioWorld/tree_1",
            "VersatileStudioWorld/tree_2",
            "VersatileStudioWorld/tree_3"
        };

        private static readonly string[] LampPaths =
        {
            "VersatileStudioWorld/lamp_pole_dual_blue",
            "VersatileStudioWorld/lamp_pole_dual_white",
            "VersatileStudioWorld/lamp_pole_single_white",
            "VersatileStudioWorld/lamp_pole_single_orange",
            "VersatileStudioWorld/lamp_pole_small"
        };

        private static bool built;
        public static bool IsAvailable => Resources.Load<GameObject>(DemoCityPath) != null;

        public static void Build(Transform parent)
        {
            if (built) return;
            built = true;

            int loaded = 0;
            GameObject demoCity = Resources.Load<GameObject>(DemoCityPath);
            if (demoCity != null)
            {
                GameObject city = Object.Instantiate(demoCity, Vector3.zero, Quaternion.identity, parent);
                city.name = "Versatile Studio Demo City";
                MakePresentationOnly(city);
                loaded++;
            }

            // Dress the wider freight map with the same asset family so regional
            // gateways and yards do not fall back to the procedural placeholder look.
            loaded += BuildRegionalDistricts(parent);
            loaded += BuildTrees(parent);
            loaded += BuildLamps(parent);

            Debug.Log("[VersatileStudioWorldBuilder] Versatile Studio world layer loaded: " + loaded + " asset instances.");
        }

        private static int BuildRegionalDistricts(Transform parent)
        {
            int count = 0;
            Vector3[] anchors =
            {
                new Vector3(-145f, 0f, 95f), new Vector3(55f, 0f, 95f),
                new Vector3(-155f, 0f, -105f), new Vector3(115f, 0f, -95f),
                new Vector3(-205f, 0f, 145f), new Vector3(-55f, 0f, 220f),
                new Vector3(85f, 0f, 155f), new Vector3(205f, 0f, -125f),
                new Vector3(260f, 0f, -185f), new Vector3(-205f, 0f, -25f)
            };

            for (int i = 0; i < anchors.Length; i++)
            {
                GameObject prefab = Load(BuildingPaths[i % BuildingPaths.Length]);
                if (prefab == null) continue;

                GameObject instance = Object.Instantiate(prefab, anchors[i], Quaternion.Euler(0f, (i % 4) * 90f, 0f), parent);
                instance.name = "Versatile District " + i.ToString("00");
                float scale = i % 3 == 0 ? 1.05f : 0.92f;
                instance.transform.localScale = Vector3.one * scale;
                MakePresentationOnly(instance);
                count++;
            }

            return count;
        }

        private static int BuildTrees(Transform parent)
        {
            int count = 0;
            for (int i = 0; i < 42; i++)
            {
                int hash = Mathf.Abs(i * 1103515245 + 12345);
                float x = -270f + (hash % 520);
                float z = -230f + ((hash / 17) % 460);

                // Keep the imported tree layer away from the core road corridors.
                if (Mathf.Abs(z) < 12f || Mathf.Abs(z - 70f) < 12f || Mathf.Abs(z + 70f) < 12f)
                    continue;
                if (Mathf.Abs(x + 90f) < 12f && z > -60f && z < 125f)
                    continue;

                GameObject prefab = Load(TreePaths[i % TreePaths.Length]);
                if (prefab == null) continue;

                GameObject instance = Object.Instantiate(prefab,
                    new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, hash % 360, 0f),
                    parent);
                instance.name = "Versatile Tree " + i.ToString("00");
                instance.transform.localScale = Vector3.one * (0.85f + (hash % 35) * 0.01f);
                MakePresentationOnly(instance);
                count++;
            }

            return count;
        }

        private static int BuildLamps(Transform parent)
        {
            int count = 0;
            for (int i = 0; i < 22; i++)
            {
                float x = -220f + i * 20f;
                float z = (i % 2 == 0) ? 10.5f : -10.5f;
                GameObject prefab = Load(LampPaths[i % LampPaths.Length]);
                if (prefab == null) continue;

                GameObject instance = Object.Instantiate(prefab,
                    new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, i % 2 == 0 ? 180f : 0f, 0f),
                    parent);
                instance.name = "Versatile Street Lamp " + i.ToString("00");
                MakePresentationOnly(instance);
                count++;
            }

            return count;
        }

        private static GameObject Load(string path)
        {
            return Resources.Load<GameObject>(path);
        }

        private static void MakePresentationOnly(GameObject root)
        {
            if (root == null) return;

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Object.Destroy(colliders[i]);

            // Imported art prefabs should not own gameplay scripts. Renderer and
            // Animator components remain untouched because they are visual.
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null) continue;
                if (behaviours[i].GetType().Namespace == "UnityEngine")
                    continue;
                Object.Destroy(behaviours[i]);
            }
        }
    }
}
