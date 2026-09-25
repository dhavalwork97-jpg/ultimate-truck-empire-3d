using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Gameplay
{
    [Serializable]
    public sealed class CityNode
    {
        public string name;
        public Vector3 worldPosition;
        public bool hasDepot;
        public bool hasDealership;
        public bool hasService;
        public CityNode(string name, Vector3 position, bool depot, bool dealership, bool service) { this.name=name; worldPosition=position; hasDepot=depot; hasDealership=dealership; hasService=service; }
    }

    public sealed class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }
        public IReadOnlyList<CityNode> Cities => cities;
        private readonly List<CityNode> cities = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Build();
        }

        private void Build()
        {
            cities.Clear();
            foreach (var city in FreightWorldMap.Cities)
                cities.Add(new CityNode(city.name, city.worldPosition, city.hasPickup, city.name == "Ahmedabad" || city.name == "Vadodara", true));
        }

        public CityNode Find(string city) => cities.Find(c => string.Equals(c.name, city, StringComparison.OrdinalIgnoreCase));
    }
}
