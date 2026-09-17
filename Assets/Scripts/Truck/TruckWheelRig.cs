using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    public sealed class TruckWheelRig : MonoBehaviour
    {
        [SerializeField] private float wheelRadius = 0.62f;
        [SerializeField] private float wheelWidth = 0.42f;
        [SerializeField] private float suspensionDistance = 0.28f;
        [SerializeField] private float spring = 65000f;
        [SerializeField] private float damper = 8500f;

        private void Awake()
        {
            var controller = GetComponent<TruckController>();
            if (controller == null) return;
            CreateWheel("FrontLeft", new Vector3(-1.35f, 0.62f, 2.05f));
            CreateWheel("FrontRight", new Vector3(1.35f, 0.62f, 2.05f));
            CreateWheel("RearLeft", new Vector3(-1.35f, 0.62f, -2.05f));
            CreateWheel("RearRight", new Vector3(1.35f, 0.62f, -2.05f));
        }

        private void CreateWheel(string wheelName, Vector3 localPosition)
        {
            var go = new GameObject(wheelName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            var wheel = go.AddComponent<WheelCollider>();
            wheel.radius = wheelRadius;
            wheel.suspensionDistance = suspensionDistance;
            var suspension = wheel.suspensionSpring;
            suspension.spring = spring;
            suspension.damper = damper;
            wheel.suspensionSpring = suspension;
            wheel.mass = 120f;
            wheel.wheelDampingRate = 1f;
            wheel.forwardFriction = MakeFriction(1.6f, 0.8f);
            wheel.sidewaysFriction = MakeFriction(1.8f, 0.9f);
            CreateVisual(go.transform, wheelName + " Visual");
        }

        private static WheelFrictionCurve MakeFriction(float stiffness, float extremum)
        {
            return new WheelFrictionCurve { extremumSlip = 0.35f, extremumValue = extremum, asymptoteSlip = 0.8f, asymptoteValue = extremum * 0.75f, stiffness = stiffness };
        }

        private void CreateVisual(Transform parent, string name)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localRotation = Quaternion.Euler(0, 0, 90);
            visual.transform.localScale = new Vector3(wheelRadius, wheelWidth * 0.5f, wheelRadius);
            Object.Destroy(visual.GetComponent<Collider>());
        }
    }
}
