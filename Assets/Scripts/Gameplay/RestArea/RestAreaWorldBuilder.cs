using UnityEngine;

namespace UltimateTruckEmpire.Gameplay.RestArea
{
    public static class RestAreaWorldBuilder
    {
        public static void Build()
        {
            if (RestAreaZone.Find("rest-ahmedabad") != null) return;

            var root = new GameObject("Rest Area - Ahmedabad");
            root.transform.position = new Vector3(0f, 0f, -35f);

            var zone = root.AddComponent<RestAreaZone>();
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(34f, 4f, 26f);

            var bays = new Transform[4];
            for (int i = 0; i < bays.Length; i++)
            {
                var bay = new GameObject("Parking Bay " + (i + 1));
                bay.transform.SetParent(root.transform, false);
                bay.transform.localPosition = new Vector3(-11f + i * 7.3f, 0.6f, 0f);
                bay.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                bays[i] = bay.transform;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Bay Marker " + (i + 1);
                marker.transform.SetParent(bay.transform, false);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(6.2f, .04f, 10f);
                Object.Destroy(marker.GetComponent<Collider>());
            }

            zone.Configure("rest-ahmedabad", "Ahmedabad Highway Rest Area", bays, 8f, 35f);
            BuildFuelStation(root.transform);
        }

        private static void BuildFuelStation(Transform root)
        {
            var station = new GameObject("Fuel Station - Ahmedabad");
            station.transform.SetParent(root, false);
            station.transform.localPosition = new Vector3(0f, 0f, 11f);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Fuel Canopy";
            canopy.transform.SetParent(station.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            canopy.transform.localScale = new Vector3(22f, .35f, 8f);
            Object.Destroy(canopy.GetComponent<Collider>());

            for (int i = 0; i < 3; i++)
            {
                var pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pump.name = "Fuel Pump " + (i + 1);
                pump.transform.SetParent(station.transform, false);
                pump.transform.localPosition = new Vector3(-7f + i * 7f, 1.1f, 0f);
                pump.transform.localScale = new Vector3(1.2f, 2.2f, 1.6f);
                Object.Destroy(pump.GetComponent<Collider>());
            }

            var trigger = station.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(22f, 3f, 8f);
            station.AddComponent<UltimateTruckEmpire.Economy.FuelStationEconomy>().Configure("Ahmedabad");
        }
    }
}
