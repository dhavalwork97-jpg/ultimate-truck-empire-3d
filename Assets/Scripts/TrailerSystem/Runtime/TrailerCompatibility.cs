using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public static class TrailerCompatibility
    {
        public static bool CanLoad(TrailerDefinition trailer, CargoDefinition cargo, float weightTons)
        {
            if (trailer == null || cargo == null) return false;

            // Authored compatibility arrays remain authoritative when present. Imported
            // production trailers may enter the runtime before those arrays are wired,
            // so use safe gameplay rules as a deterministic fallback instead of making
            // every valid contract fail to load.
            bool trailerAllows = HasExplicitCargoRules(trailer)
                ? trailer.CanCarry(cargo)
                : InferCompatibility(trailer, cargo);

            bool cargoAllows = HasExplicitTrailerRules(cargo)
                ? cargo.IsCompatibleWith(trailer)
                : InferCompatibility(trailer, cargo);

            if (!trailerAllows || !cargoAllows) return false;
            return weightTons >= 0f && weightTons <= trailer.payloadCapacityTons;
        }

        public static float GetAllowedWeight(TrailerDefinition trailer, CargoDefinition cargo, float requestedWeight)
        {
            if (!CanLoad(trailer, cargo, 0f)) return 0f;
            return Mathf.Clamp(
                requestedWeight,
                cargo.minWeightTons,
                Mathf.Min(cargo.maxWeightTons, trailer.payloadCapacityTons));
        }

        private static bool HasExplicitCargoRules(TrailerDefinition trailer) =>
            trailer.compatibleCargo != null && trailer.compatibleCargo.Length > 0;

        private static bool HasExplicitTrailerRules(CargoDefinition cargo) =>
            cargo.compatibleTrailers != null && cargo.compatibleTrailers.Length > 0;

        private static bool InferCompatibility(TrailerDefinition trailer, CargoDefinition cargo)
        {
            if (trailer == null || cargo == null) return false;

            // Safety-critical cargo classes always require their dedicated trailer family.
            if (cargo.category == CargoCategory.Fuel)
                return trailer.category == TrailerCategory.FuelTanker;

            if (cargo.requiresHeavyHaul)
                return trailer.category == TrailerCategory.Lowboy ||
                       trailer.category == TrailerCategory.HeavyFlatbed;

            if (cargo.requiresBulkTrailer)
                return trailer.category == TrailerCategory.GrainHopper ||
                       trailer.category == TrailerCategory.CementTanker ||
                       trailer.category == TrailerCategory.DumpTrailer ||
                       trailer.category == TrailerCategory.AgriculturalBulk;

            if (cargo.requiresCoveredTrailer)
                return trailer.category == TrailerCategory.DryVan ||
                       trailer.category == TrailerCategory.Refrigerated;

            // A tanker is not a generic dry-freight trailer.
            if (trailer.category == TrailerCategory.FuelTanker ||
                trailer.category == TrailerCategory.CementTanker)
                return cargo.category == CargoCategory.Fuel ||
                       cargo.category == CargoCategory.Cement ||
                       cargo.category == CargoCategory.Aggregate;

            return true;
        }
    }
}
