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
        }
    }
}
