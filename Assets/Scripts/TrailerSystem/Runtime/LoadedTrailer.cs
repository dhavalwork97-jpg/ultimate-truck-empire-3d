using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public sealed class LoadedTrailer : MonoBehaviour
    {
        [SerializeField] private TrailerDefinition trailer;
        [SerializeField] private CargoDefinition cargo;
        [SerializeField] private TrailerSkinDefinition skin;
        [SerializeField] private float cargoWeightTons;
        [SerializeField] private TrailerCargoModule cargoModule;

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

            cargoModule = GetComponent<TrailerCargoModule>() ?? gameObject.AddComponent<TrailerCargoModule>();
            cargoModule.Initialize(trailer);
            if (!cargoModule.Load(cargo))
            {
                cargo = null;
                cargoWeightTons = 0f;
                return false;
            }

            var applier = GetComponent<TrailerSkinApplier>() ?? gameObject.AddComponent<TrailerSkinApplier>();
            applier.Initialize(trailer, skin);
            return true;
        }

        public void Unload()
        {
            if (cargoModule != null)
                cargoModule.Unload();
            cargo = null;
            cargoWeightTons = 0f;
        }
    }
}