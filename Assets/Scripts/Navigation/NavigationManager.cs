using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.World;

namespace UltimateTruckEmpire.Navigation
{
    public sealed class NavigationManager : MonoBehaviour
    {
        public static NavigationManager Instance { get; private set; }

        [SerializeField] private float rebuildInterval = 0.75f;
        [SerializeField] private float waypointReachDistance = 7f;

        private readonly List<Vector3> path = new List<Vector3>();
        private readonly List<NavNode> nodes = new List<NavNode>();
        private readonly List<List<int>> edges = new List<List<int>>();
        private TruckController truck;
        private DeliveryManager delivery;
        private Transform destination;
        private float rebuildTimer;
        private int nextIndex;

        public bool HasRoute => destination != null && path.Count > 0 && nextIndex < path.Count;
        public float DistanceRemainingKm { get; private set; }
        public string DestinationName { get; private set; } = "";
        public string Instruction { get; private set; } = "Waiting for a job";
        public Vector3 NextWaypoint => HasRoute ? path[nextIndex] : Vector3.zero;
        public IReadOnlyList<Vector3> RoutePoints => path;

        private sealed class NavNode
        {
            public Vector3 position;
            public string label;
            public NavNode(Vector3 p, string l) { position = p; label = l; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildRoadGraph();
        }

        private void Update()
        {
            if (truck == null) truck = FindFirstObjectByType<TruckController>();
            if (delivery == null) delivery = DeliveryManager.Instance;
            if (truck == null || delivery == null) return;

            Transform target = ResolveTarget(delivery);
            if (target != destination)
            {
                destination = target;
                RebuildRoute();
            }

            rebuildTimer -= Time.deltaTime;
            if (rebuildTimer <= 0f)
            {
                rebuildTimer = rebuildInterval;
                if (destination != null) UpdateProgressAndRebuildIfNeeded();
            }
        }

        private Transform ResolveTarget(DeliveryManager d)
        {
            DeliveryTrigger[] triggers = FindObjectsByType<DeliveryTrigger>(FindObjectsSortMode.None);
            DeliveryTrigger.TriggerType wanted = d.ContractAccepted && d.CargoLoaded
                ? DeliveryTrigger.TriggerType.Destination
                : DeliveryTrigger.TriggerType.Pickup;

            string preferred = wanted == DeliveryTrigger.TriggerType.Pickup ? d.Pickup : d.Destination;
            DeliveryTrigger fallback = null;
            for (int i = 0; i < triggers.Length; i++)
            {
                if (triggers[i] == null || triggers[i].Type != wanted) continue;
                if (fallback == null) fallback = triggers[i];
                if (string.Equals(triggers[i].LocationName, preferred, StringComparison.OrdinalIgnoreCase))
                    return triggers[i].transform;
            }
            return fallback != null ? fallback.transform : null;
        }

        private void UpdateProgressAndRebuildIfNeeded()
        {
            if (!HasRoute) { RebuildRoute(); return; }

            Vector3 current = truck.transform.position;
            while (nextIndex < path.Count && FlatDistance(current, path[nextIndex]) <= waypointReachDistance)
                nextIndex++;

            if (nextIndex >= path.Count)
            {
                DistanceRemainingKm = FlatDistance(current, destination.position) / 1000f;
                Instruction = "Arrived at destination";
                return;
            }

            DistanceRemainingKm = FlatDistance(current, destination.position);
            for (int i = nextIndex; i < path.Count; i++)
                DistanceRemainingKm += i == nextIndex
                    ? FlatDistance(current, path[i])
                    : FlatDistance(path[i - 1], path[i]);
            DistanceRemainingKm /= 1000f;

            Vector3 target = path[nextIndex] - current;
            target.y = 0f;
            if (target.sqrMagnitude > 0.01f)
            {
                float signed = Vector3.SignedAngle(truck.transform.forward, target.normalized, Vector3.up);
                if (Mathf.Abs(signed) < 18f) Instruction = "Continue straight";
                else if (signed > 0f) Instruction = signed > 65f ? "Turn right" : "Keep right";
                else Instruction = signed < -65f ? "Turn left" : "Keep left";
            }

            // If the final waypoint is reached, use the real destination trigger.
            if (nextIndex == path.Count - 1 && FlatDistance(current, destination.position) < 18f)
                Instruction = "Arrive at " + DestinationName;
        }

        private void RebuildRoute()
        {
            path.Clear();
            nextIndex = 0;
            DistanceRemainingKm = 0f;

            if (truck == null || destination == null) { Instruction = "Waiting for a job"; return; }

            int start = NearestNode(truck.transform.position);
            int goal = NearestNode(destination.position);
            List<int> nodePath = FindPath(start, goal);

            path.Add(truck.transform.position);
            for (int i = 0; i < nodePath.Count; i++) path.Add(nodes[nodePath[i]].position);
            path.Add(destination.position);

            DestinationName = destination.GetComponent<DeliveryTrigger>()?.LocationName ?? delivery.Destination;
            rebuildTimer = rebuildInterval;
            UpdateProgressAndRebuildIfNeeded();
        }

        private List<int> FindPath(int start, int goal)
        {
            var open = new List<int> { start };
            var cameFrom = new Dictionary<int, int>();
            var g = new float[nodes.Count];
            var f = new float[nodes.Count];
            for (int i = 0; i < g.Length; i++) { g[i] = float.PositiveInfinity; f[i] = float.PositiveInfinity; }
            g[start] = 0f;
            f[start] = FlatDistance(nodes[start].position, nodes[goal].position);

            while (open.Count > 0)
            {
                int current = open[0];
                for (int i = 1; i < open.Count; i++)
                    if (f[open[i]] < f[current]) current = open[i];

                if (current == goal) return Reconstruct(cameFrom, current);
                open.Remove(current);

                foreach (int neighbor in edges[current])
                {
                    float candidate = g[current] + FlatDistance(nodes[current].position, nodes[neighbor].position);
                    if (candidate >= g[neighbor]) continue;
                    cameFrom[neighbor] = current;
                    g[neighbor] = candidate;
                    f[neighbor] = candidate + FlatDistance(nodes[neighbor].position, nodes[goal].position);
                    if (!open.Contains(neighbor)) open.Add(neighbor);
                }
            }
            return new List<int> { start, goal };
        }

        private static List<int> Reconstruct(Dictionary<int, int> cameFrom, int current)
        {
            var result = new List<int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                result.Add(current);
            }
            result.Reverse();
            return result;
        }

        private int NearestNode(Vector3 position)
        {
            int best = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < nodes.Count; i++)
            {
                float d = FlatDistance(position, nodes[i].position);
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }

        private void BuildRoadGraph()
        {
            nodes.Clear(); edges.Clear();
            var ids = new Dictionary<string, int>();

            void AddNode(string id, Vector3 p)
            {
                ids[id] = nodes.Count;
                nodes.Add(new NavNode(p, id));
                edges.Add(new List<int>());
            }
            void Link(string a, string b)
            {
                if (!ids.ContainsKey(a) || !ids.ContainsKey(b)) return;
                int ai = ids[a], bi = ids[b];
                if (!edges[ai].Contains(bi)) edges[ai].Add(bi);
                if (!edges[bi].Contains(ai)) edges[bi].Add(ai);
            }

            float[] xs = { -160f, -90f, 0f, 80f, 160f };
            float[] zs = { -70f, 0f, 70f };
            for (int z = 0; z < zs.Length; z++)
                for (int x = 0; x < xs.Length; x++)
                    AddNode("R" + x + "_" + z, new Vector3(xs[x], 0f, zs[z]));

            for (int z = 0; z < zs.Length; z++)
                for (int x = 0; x < xs.Length - 1; x++)
                    Link("R" + x + "_" + z, "R" + (x + 1) + "_" + z);
            for (int x = 0; x < xs.Length; x++)
                if (x == 1) { Link("R1_0", "R1_1"); Link("R1_1", "R1_2"); }

            AddNode("DEPOT_APPROACH", new Vector3(-55f, 0f, 8f));
            AddNode("DEPOT", new Vector3(-55f, 0f, 16f));
            AddNode("FACTORY_APPROACH", new Vector3(55f, 0f, 8f));
            AddNode("FACTORY", new Vector3(55f, 0f, 16f));

            AddNode("TRUCK_STOP", new Vector3(28f, 0f, -17f));
            AddNode("SERVICE", new Vector3(-30f, 0f, -18f));

            Link("DEPOT_APPROACH", "DEPOT");
            Link("FACTORY_APPROACH", "FACTORY");
            Link("R2_1", "FACTORY_APPROACH");
            Link("R2_1", "DEPOT_APPROACH");
            Link("TRUCK_STOP", "R3_0");
            Link("SERVICE", "R2_0");
            Link("DEPOT_APPROACH", "R2_1");
            Link("FACTORY_APPROACH", "R2_1");
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}