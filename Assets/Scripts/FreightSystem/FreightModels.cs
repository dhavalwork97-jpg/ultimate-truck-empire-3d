using System;
using UnityEngine;
using UltimateTruckEmpire.Economy;

namespace UltimateTruckEmpire.Freight
{
    [CreateAssetMenu(menuName = "Ultimate Truck Empire/Freight/Cargo Definition", fileName = "CargoDefinition")]
    public sealed class CargoDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public float weightTons = 10f;
        public LogisticsTrailerClass requiredTrailer = LogisticsTrailerClass.DryVan;
        public float rewardPerKm = 95f;
        public int baseXp = 100;
        public bool fragile;
    }

    [Serializable]
    public sealed class FreightJob
    {
        public string id;
        public string cargoId;
        public string originCity;
        public string destinationCity;
        public float distanceKm;
        public float weightTons;
        public LogisticsTrailerClass trailerClass;
        public float reward;
        public int xp;
        public int difficulty;
        public float deadlineHours;
        public bool accepted;
        public bool cargoLoaded;

        public FreightJob Clone()
        {
            return (FreightJob)MemberwiseClone();
        }
    }

    [CreateAssetMenu(menuName = "Ultimate Truck Empire/Freight/Warehouse Definition", fileName = "WarehouseDefinition")]
    public sealed class WarehouseDefinition : ScriptableObject
    {
        public string id;
        public string city;
        public string displayName;
        public string cargoType;
        public bool pickupEnabled = true;
        public bool deliveryEnabled = true;
    }
}
