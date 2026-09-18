using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public sealed class TrafficManager : MonoBehaviour
    {
        public static TrafficManager Instance { get; private set; }
        [SerializeField] private int targetVehicles = 10;
        private readonly List<GameObject> vehicles = new();
        private float nextSpawn;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // This used to drop untextured boxes at random coordinates, which put
            // them inside buildings and across roads. TrafficSpawner owns the
            // actual traffic, so when it is present this manager stands down and
            // simply tracks the fleet.
            if (FindFirstObjectByType<TrafficSpawner>() != null) enabled = false;
        }

        private void Update()
        {
            if (vehicles.Count >= targetVehicles || Time.time < nextSpawn) return;
            nextSpawn = Time.time + 1.5f;
            SpawnVehicle();
        }

        private void SpawnVehicle()
        {
            // Fallback path only: a proper vehicle, parked on a lane rather than
            // in the middle of a city block.
            var go = TrafficVisualFactory.Create(vehicles.Count, transform);
            go.name = "Traffic Vehicle";
            float x = Mathf.Lerp(-140f, 140f, (vehicles.Count * 0.17f) % 1f);
            go.transform.position = new Vector3(x, .55f, vehicles.Count % 2 == 0 ? 3.5f : -3.5f);
            vehicles.Add(go);
        }
    }
}
