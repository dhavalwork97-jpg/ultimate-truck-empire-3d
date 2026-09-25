using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimateTruckEmpire.EnvironmentAssets
{
    public static class EnvironmentAssetBatch02ImportPipeline
    {
#if UNITY_EDITOR
        [MenuItem("Ultimate Truck Empire/Environment/Batch 02/Prepare All Environment Tiles")]
        public static void PrepareAllEnvironmentTiles()
        {
            Debug.Log("[EnvironmentBatch02] Import bridge ready for Batch 02 source tiles.");
        }
#endif
    }
}
