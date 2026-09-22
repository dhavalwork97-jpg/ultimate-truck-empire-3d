using System;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.TrailerSystem
{
    public enum TrailerCategory
    {
        DryVan, Refrigerated, Flatbed, HeavyFlatbed, Lowboy, ContainerChassis,
        GrainHopper, CementTanker, DumpTrailer, AgriculturalBulk
    }

    [CreateAssetMenu(fileName = "TrailerDefinition", menuName = "Ultimate Truck Empire/Trailer/Trailer Definition")]
    public sealed class TrailerDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "trailer-id";
        public string displayName = "New Trailer";
        public TrailerCategory category;
        public string manufacturer = "UTE Trailers";
        public int requiredCompanyLevel = 1;

        [Header("Runtime")]
        public GameObject prefab;
        [Min(1f)] public float payloadCapacityTons = 30f;
        [Min(0f)] public float emptyWeightTons = 7f;
        [Min(0f)] public float purchasePrice = 100000f;
        [Min(0f)] public float resaleMultiplier = 0.65f;

        [Header("Compatibility")]
        public CargoDefinition[] compatibleCargo;
        public TrailerDefinition[] replacementCompatibleBases;

        [Header("Presentation")]
        public TrailerSkinDefinition defaultSkin;
        public TrailerSkinDefinition[] availableSkins;

        [Header("Mobile Budget")]
        [Range(1, 100000)] public int targetTriangles = 15000;
        [Range(1, 8)] public int materialSlotBudget = 3;
        [Range(1, 4096)] public int maxTextureResolution = 1024;
        [Range(1, 4)] public int lodCount = 3;

        [Header("Attachment Points")]
        public Transform cargoSocket;
        public Transform kingpinSocket;
        public Transform[] wheelSockets;

        public bool CanCarry(CargoDefinition cargo)
        {
            if (cargo == null || compatibleCargo == null) return false;
            for (int i = 0; i < compatibleCargo.Length; i++)
                if (compatibleCargo[i] == cargo) return true;
            return false;
        }

        public float ClampPayload(float requestedTons)
        {
            return Mathf.Clamp(requestedTons, 0f, payloadCapacityTons);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name.ToLowerInvariant().Replace(" ", "-");
            requiredCompanyLevel = Mathf.Max(1, requiredCompanyLevel);
            payloadCapacityTons = Mathf.Max(1f, payloadCapacityTons);
            emptyWeightTons = Mathf.Max(0f, emptyWeightTons);
            resaleMultiplier = Mathf.Clamp01(resaleMultiplier);
            materialSlotBudget = Mathf.Max(1, materialSlotBudget);
            maxTextureResolution = Mathf.Clamp(maxTextureResolution, 256, 4096);
            lodCount = Mathf.Clamp(lodCount, 1, 4);
        }
    }
}