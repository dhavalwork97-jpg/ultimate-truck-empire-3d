using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public sealed class TrafficSpawner : MonoBehaviour
    {
        [SerializeField, Range(0, 24)] private int vehicleCount = 14;
        [SerializeField] private float cruiseSpeed = 12f;
        [SerializeField] private float speedVariation = 2.5f;
        [SerializeField] private float laneOffset = 1.75f;

        private static readonly Vector3[] EastWestMain =
        {
            new Vector3(-160f,.55f,0f), new Vector3(-90f,.55f,0f),
            new Vector3(0f,.55f,0f), new Vector3(80f,.55f,0f), new Vector3(160f,.55f,0f)
        };

        private static readonly Vector3[] NorthSouthMain =
        {
            new Vector3(-90f,.55f,-35f), new Vector3(-90f,.55f,0f),
            new Vector3(-90f,.55f,70f), new Vector3(-90f,.55f,105f)
        };

        // Curved centerline routes use multiple points through the real junction pads.
        // This prevents vehicles from cutting a 90-degree corner between corridor endpoints.
        private static readonly Vector3[] WestToNorth =
        {
            new Vector3(-160f,.55f,0f), new Vector3(-110f,.55f,0f), new Vector3(-100f,.55f,0f),
            new Vector3(-95f,.55f,2f), new Vector3(-91f,.55f,7f), new Vector3(-90f,.55f,14f),
            new Vector3(-90f,.55f,35f), new Vector3(-90f,.55f,70f), new Vector3(-90f,.55f,105f)
        };

        private static readonly Vector3[] EastToSouth =
        {
            new Vector3(160f,.55f,70f), new Vector3(110f,.55f,70f), new Vector3(-75f,.55f,70f),
            new Vector3(-83f,.55f,69f), new Vector3(-88f,.55f,64f), new Vector3(-90f,.55f,57f),
            new Vector3(-90f,.55f,35f), new Vector3(-90f,.55f,0f), new Vector3(-90f,.55f,-35f)
        };

        private static readonly Vector3[] NorthToEast =
        {
            new Vector3(-90f,.55f,105f), new Vector3(-90f,.55f,82f), new Vector3(-89f,.55f,76f),
            new Vector3(-85f,.55f,72f), new Vector3(-78f,.55f,70f), new Vector3(0f,.55f,70f),
            new Vector3(80f,.55f,70f), new Vector3(160f,.55f,70f)
        };

        private static readonly Vector3[] SouthToWest =
        {
            new Vector3(-90f,.55f,-35f), new Vector3(-90f,.55f,-12f), new Vector3(-89f,.55f,-6f),
            new Vector3(-94f,.55f,-2f), new Vector3(-101f,.55f,0f), new Vector3(-160f,.55f,0f)
        };

        private static readonly Vector3[][] Routes =
        {
            EastWestMain, NorthSouthMain, WestToNorth, EastToSouth, NorthToEast, SouthToWest
        };

        private void Start()
        {
            if (TrafficSignalController.Instance == null)
                new GameObject("Traffic Signal Controller").AddComponent<TrafficSignalController>();

            Transform fleet = new GameObject("Traffic").transform;
            int count = Mathf.Clamp(vehicleCount, 0, 24);

            for (int i=0;i<count;i++)
            {
                GameObject vehicle=TrafficVisualFactory.Create(i,fleet);
                vehicle.name="Traffic Vehicle "+(i+1);

                Vector3[] route=Routes[i%Routes.Length];
                int routeStart=(i*2)%route.Length;
                float speed=Mathf.Max(4f,cruiseSpeed+((i%5)-2)*speedVariation*0.5f);
                bool forward=i%2==0;
                float directionLane=forward?laneOffset:-laneOffset;

                TrafficVehicle ai=vehicle.AddComponent<TrafficVehicle>();
                ai.Configure(route,routeStart,speed,directionLane,true,forward);
            }
        }
    }
}