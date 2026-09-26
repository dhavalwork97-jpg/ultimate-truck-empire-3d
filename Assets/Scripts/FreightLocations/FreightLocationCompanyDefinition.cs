using System;
using UnityEngine;

namespace UltimateTruckEmpire.FreightLocations
{
    [CreateAssetMenu(menuName = "Ultimate Truck Empire/Freight Location/Company Definition", fileName = "CompanyDefinition")]
    public sealed class FreightLocationCompanyDefinition : ScriptableObject
    {
        public string companyName;
        public string city;
        public GameObject prefabReference;
        public string[] cargoTypes = Array.Empty<string>();
        public bool pickupEnabled = true;
        public bool deliveryEnabled = true;
    }
}
