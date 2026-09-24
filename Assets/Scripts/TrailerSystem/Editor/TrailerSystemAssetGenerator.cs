#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    public static class TrailerSystemAssetGenerator
    {
        private const string Root = "Assets/TrailerSystem";
        private const string TrailerData = Root + "/Data/Trailers";
        private const string CargoData = Root + "/Data/Cargo";
        private const string SkinData = Root + "/Data/Skins";
        private const string ResourcesRoot = "Assets/Resources";
        private const string CatalogPath = ResourcesRoot + "/TrailerSystem/Catalog/TrailerCatalog.asset";

        [MenuItem("Ultimate Truck Empire/Trailer System/Create Starter Data")]
        public static void CreateStarterData()
        {
            EnsureFolder(Root);
            EnsureFolder(Root + "/Data");
            EnsureFolder(ResourcesRoot);
            EnsureFolder(ResourcesRoot + "/TrailerSystem");
            EnsureFolder(ResourcesRoot + "/TrailerSystem/Catalog");
            EnsureFolder(TrailerData);
            EnsureFolder(CargoData);
            EnsureFolder(SkinData);
            EnsureFolder(Root + "/Prefabs");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Textures");
            EnsureFolder(Root + "/Textures/Trailers");
            EnsureFolder(Root + "/Textures/Skins");
            EnsureFolder(Root + "/Cargo");
            EnsureFolder(Root + "/Cargo/Prefabs");
            EnsureFolder(Root + "/Cargo/Textures");

            var skins = CreateSkins();
            var trailers = new List<TrailerDefinition>
            {
                CreateTrailer("dry-van", "Dry Van", TrailerCategory.DryVan, 30f, 7.0f, 110000f, skins[0]),
                CreateTrailer("refrigerated", "Refrigerated", TrailerCategory.Refrigerated, 28f, 8.0f, 165000f, skins[1]),
                CreateTrailer("flatbed", "Flatbed", TrailerCategory.Flatbed, 32f, 6.5f, 125000f, skins[2]),
                CreateTrailer("heavy-flatbed", "Heavy Flatbed", TrailerCategory.HeavyFlatbed, 34f, 7.0f, 155000f, skins[2]),
                CreateTrailer("lowboy-rgn", "Lowboy / RGN", TrailerCategory.Lowboy, 42f, 9.0f, 210000f, skins[2]),
                CreateTrailer("container-chassis", "Container Chassis", TrailerCategory.ContainerChassis, 30f, 5.5f, 135000f, skins[2]),
                CreateTrailer("grain-hopper", "Grain Hopper", TrailerCategory.GrainHopper, 30f, 7.5f, 150000f, skins[3]),
                CreateTrailer("cement-tanker", "Cement Tanker", TrailerCategory.CementTanker, 30f, 8.0f, 185000f, skins[4]),
                CreateTrailer("fuel-tanker", "Fuel Tanker", TrailerCategory.FuelTanker, 30f, 9.0f, 180000f, skins[5])
            };

            var cargo = new List<CargoDefinition>
            {
                CreateCargo("electronics", "Electronics", CargoCategory.Electronics, 6, 18, trailers[0], true, false, true),
                CreateCargo("furniture", "Furniture", CargoCategory.Furniture, 6, 20, trailers[0], true, false, false),
                CreateCargo("retail-goods", "Retail Goods", CargoCategory.Retail, 8, 24, trailers[0], false, false, false),
                CreateCargo("frozen-food", "Frozen Food", CargoCategory.FrozenFood, 8, 24, trailers[1], false, true, false),
                CreateCargo("produce", "Produce", CargoCategory.Produce, 8, 24, trailers[1], true, true, false),
                CreateCargo("steel-coils", "Steel Coils", CargoCategory.Steel, 12, 32, trailers[2], false, false, false),
                CreateCargo("pipes", "Steel Pipes", CargoCategory.Pipes, 10, 30, trailers[2], false, false, false),
                CreateCargo("timber", "Timber", CargoCategory.Timber, 8, 28, trailers[2], false, false, false),
                CreateCargo("construction-materials", "Construction Materials", CargoCategory.Construction, 10, 32, trailers[2], false, false, false),
                CreateCargo("excavator", "Excavator", CargoCategory.Machinery, 18, 42, trailers[4], false, false, true),
                CreateCargo("bulldozer", "Bulldozer", CargoCategory.Machinery, 16, 40, trailers[4], false, false, true),
                CreateCargo("tractor", "Agricultural Tractor", CargoCategory.Agricultural, 8, 24, trailers[3], false, false, false),
                CreateCargo("wheat", "Wheat", CargoCategory.Grain, 10, 30, trailers[6], false, false, false),
                CreateCargo("corn", "Corn", CargoCategory.Grain, 10, 30, trailers[6], false, false, false),
                CreateCargo("animal-feed", "Animal Feed", CargoCategory.AnimalFeed, 8, 28, trailers[6], false, false, false),
                CreateCargo("cement-powder", "Cement Powder", CargoCategory.Cement, 10, 30, trailers[7], false, false, false),
                CreateCargo("industrial-fuel", "Industrial Fuel", CargoCategory.Fuel, 8, 30, trailers[8], false, false, false)
            };

            // Shared compatibility references. This keeps one trailer mesh reusable across many cargo types.
            for (int i = 0; i < trailers.Count; i++)
            {
                var compatible = new List<CargoDefinition>();
                for (int c = 0; c < cargo.Count; c++)
                    if (cargo[c].compatibleTrailers != null && System.Array.IndexOf(cargo[c].compatibleTrailers, trailers[i]) >= 0)
                        compatible.Add(cargo[c]);
                trailers[i].compatibleCargo = compatible.ToArray();
                EditorUtility.SetDirty(trailers[i]);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<TrailerCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TrailerCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.trailers = trailers;
            catalog.cargo = cargo;
            catalog.skins = skins;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
            Debug.Log($"Trailer System starter data created: {trailers.Count} trailers, {cargo.Count} cargo definitions, {skins.Count} skins.");
        }

        private static TrailerDefinition CreateTrailer(string id, string displayName, TrailerCategory category, float capacity, float emptyWeight, float price, TrailerSkinDefinition skin)
        {
            string path = TrailerData + "/" + id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TrailerDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.id = id;
            asset.displayName = displayName;
            asset.category = category;
            asset.manufacturer = "UTE Trailers";
            asset.payloadCapacityTons = capacity;
            asset.emptyWeightTons = emptyWeight;
            asset.purchasePrice = price;
            asset.defaultSkin = skin;
            asset.availableSkins = new[] { skin };
            asset.targetTriangles = 15000;
            asset.materialSlotBudget = 3;
            asset.maxTextureResolution = 1024;
            asset.lodCount = 3;
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CargoDefinition CreateCargo(string id, string displayName, CargoCategory category, float minWeight, float maxWeight,
            TrailerDefinition trailer, bool fragile, bool temp, bool highValue)
        {
            string path = CargoData + "/" + id + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<CargoDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CargoDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.id = id;
            asset.displayName = displayName;
            asset.category = category;
            asset.minWeightTons = minWeight;
            asset.maxWeightTons = maxWeight;
            asset.fragile = fragile;
            asset.temperatureSensitive = temp;
            asset.highValue = highValue;
            asset.compatibleTrailers = new[] { trailer };
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<TrailerSkinDefinition> CreateSkins()
        {
            string[] ids = { "plain-white", "reefer-white", "fleet-blue", "agri-green", "cement-silver", "dark-grey" };
            var list = new List<TrailerSkinDefinition>();
            for (int i = 0; i < ids.Length; i++)
            {
                string path = SkinData + "/" + ids[i] + ".asset";
                var skin = AssetDatabase.LoadAssetAtPath<TrailerSkinDefinition>(path);
                if (skin == null)
                {
                    skin = ScriptableObject.CreateInstance<TrailerSkinDefinition>();
                    AssetDatabase.CreateAsset(skin, path);
                }
                skin.id = ids[i];
                skin.displayName = ids[i].Replace("-", " ");
                skin.fallbackTint = Color.white;
                skin.textureOverrides = new TrailerTextureOverride[0];
                EditorUtility.SetDirty(skin);
                list.Add(skin);
            }
            return list;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
