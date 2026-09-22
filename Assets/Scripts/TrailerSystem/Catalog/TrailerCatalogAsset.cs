using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    [CreateAssetMenu(fileName = "TrailerCatalog", menuName = "Ultimate Truck Empire/Trailer/Trailer Catalog")]
    public sealed class TrailerCatalogAsset : ScriptableObject
    {
        public List<TrailerDefinition> trailers = new List<TrailerDefinition>();
        public List<CargoDefinition> cargo = new List<CargoDefinition>();
        public List<TrailerSkinDefinition> skins = new List<TrailerSkinDefinition>();

        public TrailerDefinition FindTrailer(string id)
        {
            if (trailers == null) return null;
            for (int i = 0; i < trailers.Count; i++)
                if (trailers[i] != null && string.Equals(trailers[i].id, id, System.StringComparison.OrdinalIgnoreCase))
                    return trailers[i];
            return null;
        }

        public CargoDefinition FindCargo(string id)
        {
            if (cargo == null) return null;
            for (int i = 0; i < cargo.Count; i++)
                if (cargo[i] != null && string.Equals(cargo[i].id, id, System.StringComparison.OrdinalIgnoreCase))
                    return cargo[i];
            return null;
        }
    }
}