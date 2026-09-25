using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Runtime freight infrastructure for the master world map. City sites stay
    /// deliberately lightweight: reusable pads, trigger volumes and signs rather
    /// than a unique environment asset per city.
    /// </summary>
    public static class FreightWorldBuilder
    {
        public static void BuildZones(Transform parent = null)
        {
            foreach (var city in FreightWorldMap.Cities)
            {
                if (string.Equals(city.name, "Ahmedabad", System.StringComparison.OrdinalIgnoreCase))
                {
                    CreateZone(new Vector3(-55f, 0f, 16f), "Ahmedabad", DeliveryTrigger.TriggerType.Pickup, parent, true);
                    CreateZone(new Vector3(-55f, 0f, -16f), "Ahmedabad", DeliveryTrigger.TriggerType.Destination, parent, false);
                    continue;
                }

                if (string.Equals(city.name, "Vadodara", System.StringComparison.OrdinalIgnoreCase))
                {
                    CreateZone(new Vector3(55f, 0f, 28f), "Vadodara", DeliveryTrigger.TriggerType.Pickup, parent, true);
                    CreateZone(new Vector3(55f, 0f, 16f), "Vadodara", DeliveryTrigger.TriggerType.Destination, parent, false);
                    continue;
                }

                Vector3 basePosition = city.worldPosition;
                CreateZone(basePosition + new Vector3(0f, 0f, 12f), city.name, DeliveryTrigger.TriggerType.Pickup, parent, true);
                CreateZone(basePosition + new Vector3(0f, 0f, -12f), city.name, DeliveryTrigger.TriggerType.Destination, parent, false);
            }
        }

        private static void CreateZone(Vector3 position, string city, DeliveryTrigger.TriggerType type,
                                       Transform parent, bool pickup)
        {
            var zone = new GameObject(city + (pickup ? " Freight Pickup" : " Freight Delivery"));
            if (parent != null) zone.transform.SetParent(parent, false);
            zone.transform.position = position + Vector3.up;

            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(22f, 4f, 14f);

            zone.AddComponent<DeliveryTrigger>().Configure(type, city);

            // A single reusable pad/marker gives the player a physical target
            // without requiring a bespoke warehouse mesh for every city.
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = city + (pickup ? " Pickup Pad" : " Delivery Pad");
            pad.transform.SetParent(zone.transform, false);
            pad.transform.localPosition = new Vector3(0f, -0.92f, 0f);
            pad.transform.localScale = new Vector3(18f, 0.12f, 10f);
            Object.Destroy(pad.GetComponent<Collider>());
            var renderer = pad.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = TruckMaterialLibrary.MakeLens(
                    city + (pickup ? "FreightPickup" : "FreightDelivery"),
                    pickup ? new Color(.20f, .60f, .95f) : new Color(.95f, .55f, .12f),
                    1.35f);

            PropBuilder.CreateSign(
                zone.transform,
                new Vector3(0f, 0f, pickup ? 5.5f : -5.5f),
                pickup ? 0f : 180f,
                7f, 2f, 3f,
                pickup ? WorldSurface.SignBlue : WorldSurface.SignGreen,
                city.ToUpperInvariant() + (pickup ? "\nFREIGHT PICKUP" : "\nFREIGHT DELIVERY"));
        }
    }
}
