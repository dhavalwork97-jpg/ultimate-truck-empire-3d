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
            cities.Add(new CityNode("Ahmedabad", new Vector3(-55,0,0), true, true, true));
            cities.Add(new CityNode("Gandhinagar", new Vector3(-25,0,70), true, false, true));
            cities.Add(new CityNode("Vadodara", new Vector3(55,0,0), true, true, true));
            cities.Add(new CityNode("Surat", new Vector3(95,0,-70), true, false, true));
            cities.Add(new CityNode("Rajkot", new Vector3(-110,0,-70), true, false, true));
            cities.Add(new CityNode("Udaipur", new Vector3(-120,0,80), true, false, true));
        }

        public CityNode Find(string city) => cities.Find(c => string.Equals(c.name, city, StringComparison.OrdinalIgnoreCase));
    }
}
