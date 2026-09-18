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

        private void Update()
        {
            if (vehicles.Count >= targetVehicles || Time.time < nextSpawn) return;
            nextSpawn = Time.time + 1.5f;
            SpawnVehicle();
        }

        private void SpawnVehicle()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Traffic Vehicle";
            go.transform.position = new Vector3(Random.Range(-80f, 80f), .65f, Random.Range(-6f, 76f));
            go.transform.localScale = new Vector3(1.7f, 1.1f, 3.5f);
            Destroy(go.GetComponent<Collider>());
            vehicles.Add(go);
        }
    }
}
