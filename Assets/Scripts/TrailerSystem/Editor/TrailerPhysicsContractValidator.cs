#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem.Editor
{
    /// <summary>
    /// Validates the authored prefab contract required by the runtime kingpin
    /// attachment system. It deliberately warns rather than auto-editing art.
    /// </summary>
    public static class TrailerPhysicsContractValidator
    {
        [MenuItem("Ultimate Truck Empire/Trailer System/Validate Trailer Physics")]
        public static void Validate()
        {
            var catalog = TrailerCatalogRuntime.Catalog;
            if (catalog == null)
            {
                Debug.LogError("[TrailerPhysics] Trailer catalog could not be loaded from Resources.");
                return;
            }

            int errors = 0;
            int warnings = 0;

            foreach (var definition in catalog.trailers)
            {
                if (definition == null) continue;
                if (definition.prefab == null)
                {
                    warnings++;
                    Debug.LogWarning($"[TrailerPhysics] {definition.id}: no prefab assigned; physics contract skipped.");
                    continue;
                }

                var root = definition.prefab.transform;
                Transform kingpin = root.Find("Sockets/Kingpin") ?? root.Find("Kingpin");
                if (kingpin == null)
                {
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (child != root && string.Equals(child.name, "Kingpin", System.StringComparison.OrdinalIgnoreCase))
                        {
                            kingpin = child;
                            break;
                        }
                    }
                }

                if (kingpin == null)
                {
                    errors++;
                    Debug.LogError($"[TrailerPhysics] {definition.id}: missing authored Kingpin socket.");
                }

                var colliders = definition.prefab.GetComponentsInChildren<Collider>(true);
                if (colliders.Length == 0)
                {
                    errors++;
                    Debug.LogError($"[TrailerPhysics] {definition.id}: no colliders found on prefab.");
                }
                else
                {
                    int solid = 0;
                    for (int i = 0; i < colliders.Length; i++)
                        if (colliders[i] != null && !colliders[i].isTrigger) solid++;

                    if (solid == 0)
                    {
                        errors++;
                        Debug.LogError($"[TrailerPhysics] {definition.id}: all prefab colliders are triggers.");
                    }
                }

                if (root.GetComponentInChildren<LoadedTrailer>(true) == null)
                {
                    warnings++;
                    Debug.LogWarning($"[TrailerPhysics] {definition.id}: LoadedTrailer component missing.");
                }

                if (root.GetComponentInChildren<TrailerCargoModule>(true) == null)
                {
                    warnings++;
                    Debug.LogWarning($"[TrailerPhysics] {definition.id}: TrailerCargoModule component missing.");
                }
            }

            Debug.Log($"[TrailerPhysics] Validation complete. Errors: {errors}, Warnings: {warnings}.");
        }
    }
}
#endif
