using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public sealed class LoadedTrailer : MonoBehaviour
    {
        [SerializeField] private TrailerDefinition trailer;
        [SerializeField] private CargoDefinition cargo;
        [SerializeField] private TrailerSkinDefinition skin;
        [SerializeField] private float cargoWeightTons;

        public TrailerDefinition Trailer => trailer;
        public CargoDefinition Cargo => cargo;
        public TrailerSkinDefinition Skin => skin;
        public float CargoWeightTons => cargoWeightTons;
        public float GrossWeightTons => (trailer != null ? trailer.emptyWeightTons : 0f) + cargoWeightTons;

        public bool Configure(TrailerDefinition trailerDefinition, CargoDefinition cargoDefinition,
            float weightTons, TrailerSkinDefinition skinDefinition = null)
        {
            if (!TrailerCompatibility.CanLoad(trailerDefinition, cargoDefinition, weightTons))
                return false;

            trailer = trailerDefinition;
            cargo = cargoDefinition;
            cargoWeightTons = TrailerCompatibility.GetAllowedWeight(trailer, cargo, weightTons);
            skin = skinDefinition != null ? skinDefinition : trailer.defaultSkin;

            var applier = GetComponent<TrailerSkinApplier>() ?? gameObject.AddComponent<TrailerSkinApplier>();
            applier.Initialize(trailer, skin);
            return true;
        }

        public void Unload()
        {
            cargo = null;
            cargoWeightTons = 0f;
        }
    }
}