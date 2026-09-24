using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public enum CargoCategory
    {
        Retail, Food, FrozenFood, Produce, Electronics, Furniture,
        Steel, Timber, Pipes, Construction, Machinery, Agricultural,
        Grain, AnimalFeed, Cement, Aggregate, Waste, Containerized, Fuel
    }

    [CreateAssetMenu(fileName = "CargoDefinition", menuName = "Ultimate Truck Empire/Trailer/Cargo Definition")]
    public sealed class CargoDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "cargo-id";
        public string displayName = "New Cargo";
        public CargoCategory category;

        [Header("Payload")]
        [Min(0.1f)] public float minWeightTons = 5f;
        [Min(0.1f)] public float maxWeightTons = 25f;
        [Min(0.01f)] public float volumeM3 = 20f;
        [Min(0f)] public float rewardMultiplier = 1f;

        [Header("Gameplay")]
        public bool fragile;
        public bool temperatureSensitive;
        public bool highValue;
        public bool requiresCoveredTrailer;
        public bool requiresBulkTrailer;
        public bool requiresHeavyHaul;

        [Header("Presentation")]
        public GameObject cargoPrefab;
        public Texture2D cargoTexture;
        public Vector3 localPosition;
        public Vector3 localRotation;
        public Vector3 localScale = Vector3.one;

        [Header("Compatibility")]
        public TrailerDefinition[] compatibleTrailers;

        public float RollWeight(System.Random random)
        {
            if (random == null) return minWeightTons;
            return Mathf.Lerp(minWeightTons, maxWeightTons, (float)random.NextDouble());
        }

        public bool IsCompatibleWith(TrailerDefinition trailer)
        {
            if (trailer == null || compatibleTrailers == null) return false;
            for (int i = 0; i < compatibleTrailers.Length; i++)
                if (compatibleTrailers[i] == trailer) return true;
            return false;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name.ToLowerInvariant().Replace(" ", "-");
            minWeightTons = Mathf.Max(0.1f, minWeightTons);
            maxWeightTons = Mathf.Max(minWeightTons, maxWeightTons);
            volumeM3 = Mathf.Max(0.01f, volumeM3);
            rewardMultiplier = Mathf.Max(0f, rewardMultiplier);
        }
    }
}