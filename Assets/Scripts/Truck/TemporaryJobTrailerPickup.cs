using UnityEngine;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.TrailerSystem;

namespace UltimateTruckEmpire.Truck
{
    /// <summary>
    /// Represents a non-owned job trailer physically parked at the pickup warehouse.
    /// The player must back the truck into the kingpin/coupling zone at low speed;
    /// the existing TrailerPhysicsAttachment then performs the actual coupling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TemporaryJobTrailerPickup : MonoBehaviour
    {
        private string jobId;
        private TrailerDefinition definition;
        private TrailerSkinDefinition skin;
        private UltimateTruckEmpire.Gameplay.TrailerType jobType;
        private GameObject couplingTrigger;

        public string JobId => jobId;
        public UltimateTruckEmpire.Gameplay.TrailerType JobType => jobType;

        public void Initialize(string freightJobId, TrailerDefinition trailerDefinition,
            TrailerSkinDefinition trailerSkin, UltimateTruckEmpire.Gameplay.TrailerType type)
        {
            jobId = freightJobId ?? string.Empty;
            definition = trailerDefinition;
            skin = trailerSkin;
            jobType = type;
            BuildCouplingTrigger();
        }

        private void BuildCouplingTrigger()
        {
            if (couplingTrigger != null) return;

            var kingpin = FindKingpin(transform);
            var trigger = new GameObject("Temporary Trailer Coupling Zone");
            trigger.transform.SetParent(transform, false);
            trigger.transform.localPosition = kingpin != null
                ? transform.InverseTransformPoint(kingpin.position)
                : new Vector3(0f, 1.2f, 2.0f);
            trigger.transform.localRotation = Quaternion.identity;

            var box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.2f, 2.2f, 3.0f);
            couplingTrigger = trigger;
        }

        private void OnTriggerStay(Collider other)
        {
            if (couplingTrigger == null || other == null) return;
            var truck = other.transform.root.GetComponent<TruckController>();
            if (truck == null || FleetManager.Instance == null) return;

            var body = truck.GetComponent<Rigidbody>();
            if (body != null && body.linearVelocity.magnitude > 1.5f) return;

            var attachment = truck.GetComponent<TrailerPhysicsAttachment>();
            if (attachment != null && attachment.IsAttached) return;

            Transform kingpin = FindKingpin(transform);
            if (kingpin == null) return;

            Transform hitch = attachment != null ? attachment.HitchPoint : EnsureTruckHitch(truck);
            if (hitch == null) return;

            if (Vector3.Distance(hitch.position, kingpin.position) > 1.6f) return;

            Vector3 truckForward = truck.transform.forward;
            Vector3 trailerForward = transform.forward;
            if (Vector3.Dot(truckForward, trailerForward) < 0.72f) return;

            if (FleetManager.Instance.TryAttachParkedTemporaryJobTrailer(truck, gameObject, definition, skin))
            {
                RemoveCouplingTrigger();
                Destroy(this);
            }
        }

        private static Transform EnsureTruckHitch(TruckController truck)
        {
            var attachment = truck.GetComponent<TrailerPhysicsAttachment>();
            if (attachment == null) attachment = truck.gameObject.AddComponent<TrailerPhysicsAttachment>();
            return attachment.HitchPoint;
        }

        public void RemoveCouplingTrigger()
        {
            if (couplingTrigger != null)
                Destroy(couplingTrigger);
            couplingTrigger = null;
        }

        private static Transform FindKingpin(Transform root)
        {
            if (root == null) return null;
            var direct = root.Find("Sockets/Kingpin") ?? root.Find("Kingpin");
            if (direct != null) return direct;

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child != root && string.Equals(child.name, "Kingpin", System.StringComparison.OrdinalIgnoreCase))
                    return child;
            return null;
        }

        private void OnDestroy()
        {
            RemoveCouplingTrigger();
        }
    }
}
