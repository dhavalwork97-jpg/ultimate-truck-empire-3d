#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    /// <summary>
    /// Validates the eight launch trailer identities before production assets are accepted.
    /// This validator checks data contracts only; it does not create or alter art assets.
    /// </summary>
    public static class TrailerLaunchSetValidator
    {
        private struct RequiredTrailer
        {
            public readonly string Id;
            public readonly TrailerCategory Category;

            public RequiredTrailer(string id, TrailerCategory category)
            {
                Id = id;
                Category = category;
            }
        }

        private static readonly RequiredTrailer[] RequiredTrailers =
        {
            new RequiredTrailer("dry-van", TrailerCategory.DryVan),
            new RequiredTrailer("refrigerated", TrailerCategory.Refrigerated),
            new RequiredTrailer("flatbed", TrailerCategory.Flatbed),
            new RequiredTrailer("heavy-flatbed", TrailerCategory.HeavyFlatbed),
            new RequiredTrailer("lowboy-rgn", TrailerCategory.Lowboy),
            new RequiredTrailer("container-chassis", TrailerCategory.ContainerChassis),
            new RequiredTrailer("grain-hopper", TrailerCategory.GrainHopper),
            new RequiredTrailer("cement-tanker", TrailerCategory.CementTanker)
        };

        [MenuItem("Ultimate Truck Empire/Trailer System/Validate Launch Trailer Set")]
        public static void Validate()
        {
            TrailerCatalogAsset catalog = FindCatalog();
            if (catalog == null)
            {
                Debug.LogError("[TrailerSystem] Launch set validation failed: no TrailerCatalogAsset found.");
                return;
            }

            var definitionsById = new Dictionary<string, TrailerDefinition>(StringComparer.OrdinalIgnoreCase);
            int errors = 0;

            if (catalog.trailers == null)
            {
                Debug.LogError("[TrailerSystem] Launch set validation failed: catalog trailer list is null.", catalog);
                return;
            }

            for (int i = 0; i < catalog.trailers.Count; i++)
            {
                TrailerDefinition definition = catalog.trailers[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                    continue;

                if (definitionsById.ContainsKey(definition.id))
                {
                    Debug.LogError("[TrailerSystem] Duplicate trailer ID in catalog: " + definition.id, definition);
                    errors++;
                }
                else
                {
                    definitionsById.Add(definition.id, definition);
                }
            }

            for (int i = 0; i < RequiredTrailers.Length; i++)
            {
                RequiredTrailer required = RequiredTrailers[i];
                TrailerDefinition definition;

                if (!definitionsById.TryGetValue(required.Id, out definition))
                {
                    Debug.LogError("[TrailerSystem] Missing launch trailer definition: " + required.Id, catalog);
                    errors++;
                    continue;
                }

                if (definition.category != required.Category)
                {
                    Debug.LogError("[TrailerSystem] " + required.Id + " has category " +
                        definition.category + " but requires " + required.Category + ".", definition);
                    errors++;
                }

                if (definition.prefab == null)
                {
                    Debug.LogError("[TrailerSystem] " + required.Id + " has no production prefab assigned.", definition);
                    errors++;
                }

                if (definition.payloadCapacityTons <= 0f)
                {
                    Debug.LogError("[TrailerSystem] " + required.Id + " must have a positive payload capacity.", definition);
                    errors++;
                }

                if (definition.emptyWeightTons < 0f)
                {
                    Debug.LogError("[TrailerSystem] " + required.Id + " has an invalid empty weight.", definition);
                    errors++;
                }

                if (definition.targetTriangles <= 0 ||
                    definition.materialSlotBudget <= 0 ||
                    definition.maxTextureResolution <= 0 ||
                    definition.lodCount <= 0)
                {
                    Debug.LogError("[TrailerSystem] " + required.Id +
                        " has incomplete mobile asset budget metadata.", definition);
                    errors++;
                }
            }

            if (errors == 0)
            {
                Debug.Log("[TrailerSystem] Launch trailer set validation passed: " +
                    RequiredTrailers.Length + " required trailer contracts verified.");
            }
            else
            {
                Debug.LogError("[TrailerSystem] Launch trailer set validation failed with " +
                    errors + " error(s).");
            }
        }

        private static TrailerCatalogAsset FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:TrailerCatalogAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TrailerCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<TrailerCatalogAsset>(path);
                if (catalog != null)
                    return catalog;
            }

            return null;
        }
    }
}
#endif
