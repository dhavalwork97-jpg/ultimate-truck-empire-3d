using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public static class TrailerCompatibility
    {
        public static bool CanLoad(TrailerDefinition trailer, CargoDefinition cargo, float weightTons)
        {
            if (trailer == null || cargo == null) return false;
            if (!trailer.CanCarry(cargo) || !cargo.IsCompatibleWith(trailer)) return false;
            return weightTons >= 0f && weightTons <= trailer.payloadCapacityTons;
        }

        public static float GetAllowedWeight(TrailerDefinition trailer, CargoDefinition cargo, float requestedWeight)
        {
            if (!CanLoad(trailer, cargo, 0f)) return 0f;
            return Mathf.Clamp(requestedWeight, cargo.minWeightTons, Mathf.Min(cargo.maxWeightTons, trailer.payloadCapacityTons));
        }
    }
}