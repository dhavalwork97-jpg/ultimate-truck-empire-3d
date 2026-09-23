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
        private int roadNetworkVersion = -1;

        public bool HasRoute => destination != null && path.Count > 0 && nextIndex < path.Count;
        public float DistanceRemainingKm { get; private set; }
        public string DestinationName { get; private set; } = "";
        public string Instruction { get; private set; } = "Waiting for a job";
        public Vector3 NextWaypoint => HasRoute ? path[nextIndex] : Vector3.zero;
        public IReadOnlyList<Vector3> RoutePoints => path;

        private sealed class NavNode
        {
            public Vector3 position;
            public NavNode(Vector3 p) { position = p; }
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
            if (roadNetworkVersion != RoadNetwork.Version)
            {
                BuildRoadGraph();
                roadNetworkVersion = RoadNetwork.Version;
                if (destination != null) RebuildRoute();
            }

            if (truck == null) truck = FindFirstObjectByType<TruckController>();
            if (delivery == null) delivery = DeliveryManager.Instance;
            if (truck == null || delivery == null) return;

            Transform target = ResolveTarget(delivery);
            if (target != destination) { destination = target; RebuildRoute(); }

            rebuildTimer -= Time.deltaTime;
            if (rebuildTimer <= 0f)
            {
                rebuildTimer = rebuildInterval;
                if (destination != null) UpdateProgress();
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
                if (string.Equals(triggers[i].gameObject.name, preferred, StringComparison.OrdinalIgnoreCase))
                    return triggers[i].transform;
            }
            return fallback != null ? fallback.transform : null;
        }

        private void UpdateProgress()
        {
            if (!HasRoute) { RebuildRoute(); return; }
            Vector3 current = truck.transform.position;
            while (nextIndex < path.Count && FlatDistance(current, path[nextIndex]) <= waypointReachDistance) nextIndex++;

            if (nextIndex >= path.Count)
            {
                DistanceRemainingKm = FlatDistance(current, destination.position) / 1000f;
                Instruction = "Arrived at destination";
                return;
            }

            float metres = FlatDistance(current, destination.position);
            for (int i = nextIndex; i < path.Count; i++)
                metres += i == nextIndex ? FlatDistance(current, path[i]) : FlatDistance(path[i - 1], path[i]);
            DistanceRemainingKm = metres / 1000f;

            Vector3 target = path[nextIndex] - current;
            target.y = 0f;
            if (target.sqrMagnitude > 0.01f)
            {
                float signed = Vector3.SignedAngle(truck.transform.forward, target.normalized, Vector3.up);
                if (Mathf.Abs(signed) < 18f) Instruction = "Continue straight";
                else if (signed > 0f) Instruction = signed > 65f ? "Turn right" : "Keep right";
                else Instruction = signed < -65f ? "Turn left" : "Keep left";
            }

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

            DestinationName = destination.gameObject.name;
            rebuildTimer = rebuildInterval;
            UpdateProgress();
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
                for (int i = 1; i < open.Count; i++) if (f[open[i]] < f[current]) current = open[i];
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
            while (cameFrom.ContainsKey(current)) { current = cameFrom[current]; result.Add(current); }
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
            nodes.Clear();
            edges.Clear();

            if (RoadNetwork.Corridors.Count == 0)
                return;

            var ids = new Dictionary<string, int>();

            void AddNode(string id, Vector3 position)
            {
                if (ids.ContainsKey(id))
                    return;
                ids[id] = nodes.Count;
                nodes.Add(new NavNode(position));
                edges.Add(new List<int>());
            }

            void Link(string a, string b)
            {
                if (!ids.ContainsKey(a) || !ids.ContainsKey(b)) return;
                int ai = ids[a], bi = ids[b];
                if (!edges[ai].Contains(bi)) edges[ai].Add(bi);
                if (!edges[bi].Contains(ai)) edges[bi].Add(ai);
            }

            string NodeId(Vector3 position)
            {
                return "R_" + Mathf.RoundToInt(position.x * 10f) + "_" + Mathf.RoundToInt(position.z * 10f);
            }

            // Create nodes at corridor ends and real junction centers.
            for (int i = 0; i < RoadNetwork.Corridors.Count; i++)
            {
                RoadNetwork.RoadCorridor corridor = RoadNetwork.Corridors[i];
                Vector3 start = corridor.AlongX
                    ? new Vector3(corridor.Min, 0f, corridor.FixedCoordinate)
                    : new Vector3(corridor.FixedCoordinate, 0f, corridor.Min);
                Vector3 end = corridor.AlongX
                    ? new Vector3(corridor.Max, 0f, corridor.FixedCoordinate)
                    : new Vector3(corridor.FixedCoordinate, 0f, corridor.Max);

                AddNode(NodeId(start), start);
                AddNode(NodeId(end), end);

                for (int j = 0; j < RoadNetwork.Junctions.Count; j++)
                {
                    RoadNetwork.Junction junction = RoadNetwork.Junctions[j];
                    bool onCorridor = corridor.AlongX
                        ? Mathf.Abs(junction.Center.z - corridor.FixedCoordinate) <= corridor.Width * 0.5f &&
                          junction.Center.x >= corridor.Min && junction.Center.x <= corridor.Max
                        : Mathf.Abs(junction.Center.x - corridor.FixedCoordinate) <= corridor.Width * 0.5f &&
                          junction.Center.z >= corridor.Min && junction.Center.z <= corridor.Max;

                    if (onCorridor)
                        AddNode(NodeId(junction.Center), junction.Center);
                }

                var corridorNodes = new List<Vector3>();
                for (int j = 0; j < nodes.Count; j++)
                {
                    Vector3 position = nodes[j].position;
                    bool onCorridor = corridor.AlongX
                        ? Mathf.Abs(position.z - corridor.FixedCoordinate) <= 0.01f &&
                          position.x >= corridor.Min - 0.01f && position.x <= corridor.Max + 0.01f
                        : Mathf.Abs(position.x - corridor.FixedCoordinate) <= 0.01f &&
                          position.z >= corridor.Min - 0.01f && position.z <= corridor.Max + 0.01f;

                    if (onCorridor)
                        corridorNodes.Add(position);
                }

                corridorNodes.Sort((a, b) => corridor.AlongX
                    ? a.x.CompareTo(b.x)
                    : a.z.CompareTo(b.z));

                for (int j = 1; j < corridorNodes.Count; j++)
                    Link(NodeId(corridorNodes[j - 1]), NodeId(corridorNodes[j]));
            }

            // Site access nodes remain explicit, but connect to the shared main road.
            AddNode("DEPOT_APPROACH", new Vector3(-55f, 0f, 8f));
            AddNode("DEPOT", new Vector3(-55f, 0f, 16f));
            AddNode("FACTORY_APPROACH", new Vector3(55f, 0f, 8f));
            AddNode("FACTORY", new Vector3(55f, 0f, 16f));
            Link("DEPOT_APPROACH", "DEPOT");
            Link("FACTORY_APPROACH", "FACTORY");

            string depotRoad = NodeId(new Vector3(-55f, 0f, 0f));
            string factoryRoad = NodeId(new Vector3(55f, 0f, 0f));
            if (ids.ContainsKey(depotRoad)) Link("DEPOT_APPROACH", depotRoad);
            if (ids.ContainsKey(factoryRoad)) Link("FACTORY_APPROACH", factoryRoad);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}