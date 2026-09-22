#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    /// <summary>
    /// Creates lightweight, clearly marked placeholder trailer/cargo prefabs for integration testing.
    /// These are not final art assets and are intentionally generated from Unity primitives.
    /// </summary>
    public static class TrailerPrefabStarterGenerator
    {
        private const string PrefabRoot = "Assets/TrailerSystem/Prefabs";
        private const string CargoRoot = "Assets/TrailerSystem/Cargo/Prefabs";

        [MenuItem("Ultimate Truck Empire/Trailer System/Create Integration Prefabs")]
        public static void CreateIntegrationPrefabs()
        {
            EnsureFolder("Assets/TrailerSystem");
            EnsureFolder(PrefabRoot);
            EnsureFolder(CargoRoot);

            var catalog = Resources.Load<TrailerCatalogAsset>("TrailerSystem/Catalog/TrailerCatalog");
            if (catalog == null || catalog.trailers == null)
            {
                Debug.LogError("[TrailerSystem] Create Starter Data must be run before integration prefabs.");
                return;
            }

            int trailersCreated = 0;
            for (int i = 0; i < catalog.trailers.Count; i++)
            {
                var definition = catalog.trailers[i];
                if (definition == null || definition.prefab != null) continue;
                string path = PrefabRoot + "/" + definition.id + ".prefab";
                var root = BuildTrailer(definition);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);

                definition.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                EditorUtility.SetDirty(definition);
                trailersCreated++;
            }

            int cargoCreated = 0;
            if (catalog.cargo != null)
            {
                for (int i = 0; i < catalog.cargo.Count; i++)
                {
                    var cargo = catalog.cargo[i];
                    if (cargo == null || cargo.cargoPrefab != null) continue;
                    string path = CargoRoot + "/" + cargo.id + ".prefab";
                    var root = BuildCargo(cargo);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Object.DestroyImmediate(root);

                    cargo.cargoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    EditorUtility.SetDirty(cargo);
                    cargoCreated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TrailerSystem] Integration prefabs created. Trailers: " + trailersCreated +
                      ", cargo: " + cargoCreated +
                      ". Replace these clearly-marked placeholders with final authored art before shipping.");
        }

        private static GameObject BuildTrailer(TrailerDefinition definition)
        {
            var root = new GameObject(definition.displayName + " Trailer");
            root.AddComponent<LoadedTrailer>();
            root.AddComponent<TrailerCargoModule>();
            root.AddComponent<TrailerSkinApplier>();

            var sockets = new GameObject("Sockets");
            sockets.transform.SetParent(root.transform, false);

            var cargoSocket = NewSocket("CargoSocket", sockets.transform, new Vector3(0f, 1.2f, 0f));
            var kingpin = NewSocket("Kingpin", sockets.transform, new Vector3(0f, 0.55f, 2.15f));

            var wheels = new List<Transform>();
            float halfLength = definition.category == TrailerCategory.Lowboy ? 2.0f : 2.4f;
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -1.05f : 1.05f;
                float z = i < 2 ? -halfLength : halfLength;
                wheels.Add(NewSocket("WheelSocket_" + (i + 1), sockets.transform, new Vector3(x, 0.45f, z)));
            }

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "PLACEHOLDER_ART";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            body.transform.localScale = GetBodyScale(definition.category);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null) material = new Material(Shader.Find("Standard"));
            material.name = "Trailer Placeholder Material";
            body.GetComponent<Renderer>().sharedMaterial = material;

            var lodGroup = root.AddComponent<LODGroup>();
            var renderer = body.GetComponent<Renderer>();
            lodGroup.SetLODs(new[]
            {
                new LOD(0.60f, new[] { renderer }),
                new LOD(0.25f, new[] { renderer }),
                new LOD(0.05f, new[] { renderer })
            });
            lodGroup.RecalculateBounds();

            var loaded = root.GetComponent<LoadedTrailer>();
            var cargoModule = root.GetComponent<TrailerCargoModule>();
            SetPrivateField(cargoModule, "cargoSocket", cargoSocket);
            var skin = root.GetComponent<TrailerSkinApplier>();
            SetPrivateField(skin, "trailerDefinition", definition);

            return root;
        }

        private static GameObject BuildCargo(CargoDefinition definition)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "PLACEHOLDER_CARGO_" + definition.displayName;
            root.transform.localScale = definition.localScale == Vector3.zero ? Vector3.one : definition.localScale;
            Object.DestroyImmediate(root.GetComponent<Collider>());

            var renderer = root.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null) material = new Material(Shader.Find("Standard"));
            material.name = "Cargo Placeholder Material";
            renderer.sharedMaterial = material;
            return root;
        }

        private static Vector3 GetBodyScale(TrailerCategory category)
        {
            switch (category)
            {
                case TrailerCategory.Flatbed: return new Vector3(2.5f, 0.35f, 6.0f);
                case TrailerCategory.HeavyFlatbed: return new Vector3(2.6f, 0.45f, 6.2f);
                case TrailerCategory.Lowboy: return new Vector3(2.7f, 0.55f, 6.4f);
                case TrailerCategory.ContainerChassis: return new Vector3(2.55f, 0.4f, 6.1f);
                case TrailerCategory.GrainHopper: return new Vector3(2.5f, 1.9f, 6.0f);
                case TrailerCategory.CementTanker: return new Vector3(2.45f, 1.65f, 6.0f);
                case TrailerCategory.Refrigerated: return new Vector3(2.5f, 2.0f, 6.0f);
                default: return new Vector3(2.5f, 1.9f, 6.0f);
            }
        }

        private static Transform NewSocket(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(target, value);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
