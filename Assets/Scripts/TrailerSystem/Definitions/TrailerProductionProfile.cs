using System;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    [CreateAssetMenu(fileName = "TrailerProductionProfile", menuName = "Ultimate Truck Empire/Trailer/Production Profile")]
    public sealed class TrailerProductionProfile : ScriptableObject
    {
        [Serializable]
        public struct AxlePreset
        {
            public string name;
            public Vector3 localPosition;
        }

        [Header("Production")]
        public TrailerDefinition trailer;
        [Min(1)] public int axleCount = 3;
        [Min(0f)] public float maxGrossWeightTons = 40f;
        [Min(0f)] public float rolloverThreshold = 1.2f;
        [Min(0f)] public float brakeForce = 1f;
        public AxlePreset[] axles;

        [Header("Mobile")]
        [Min(1)] public int lod0TriangleBudget = 25000;
        [Min(1)] public int lod1TriangleBudget = 12000;
        [Min(1)] public int lod2TriangleBudget = 5000;
        [Min(1)] public int lod3TriangleBudget = 1500;

        private void OnValidate()
        {
            axleCount = Mathf.Max(1, axleCount);
            maxGrossWeightTons = Mathf.Max(0f, maxGrossWeightTons);
            rolloverThreshold = Mathf.Max(0f, rolloverThreshold);
            brakeForce = Mathf.Max(0f, brakeForce);
            lod0TriangleBudget = Mathf.Max(1, lod0TriangleBudget);
            lod1TriangleBudget = Mathf.Max(1, lod1TriangleBudget);
            lod2TriangleBudget = Mathf.Max(1, lod2TriangleBudget);
            lod3TriangleBudget = Mathf.Max(1, lod3TriangleBudget);
        }
    }
}