using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    /// <summary>
    /// Creates the four WheelColliders and their visible wheels, then keeps the
    /// visuals locked to the colliders.
    ///
    /// The visuals are parented to the truck root rather than to the collider
    /// object, because a WheelCollider's own transform does not steer, spin or
    /// move with suspension travel - parenting to it is why wheels look welded
    /// in place. One LateUpdate drives all four.
    /// </summary>
    public sealed class TruckWheelRig : MonoBehaviour
    {
        [SerializeField] private float wheelRadius = 0.62f;
        [SerializeField] private float wheelWidth = 0.42f;
        [SerializeField] private float suspensionDistance = 0.28f;
        [SerializeField] private float spring = 65000f;
        [SerializeField] private float damper = 8500f;

        private readonly WheelCollider[] wheels = new WheelCollider[4];
        private readonly Transform[] visuals = new Transform[4];
        private Transform visualRoot;

        public WheelCollider[] Wheels => wheels;
        public Transform[] Visuals => visuals;

        private void Awake()
        {
            var controller = GetComponent<TruckController>();
            if (controller == null) return;

            visualRoot = new GameObject("Wheel Visuals").transform;
            visualRoot.SetParent(transform, false);

            wheels[0] = CreateWheel("FrontLeft", new Vector3(-1.35f, 0.62f, 2.05f), 0);
            wheels[1] = CreateWheel("FrontRight", new Vector3(1.35f, 0.62f, 2.05f), 1);
            wheels[2] = CreateWheel("RearLeft", new Vector3(-1.35f, 0.62f, -2.05f), 2);
            wheels[3] = CreateWheel("RearRight", new Vector3(1.35f, 0.62f, -2.05f), 3);
            controller.ConfigureWheels(wheels[0], wheels[1], wheels[2], wheels[3]);
        }

        private WheelCollider CreateWheel(string wheelName, Vector3 localPosition, int index)
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
            suspension.targetPosition = 0.5f;
            wheel.suspensionSpring = suspension;
            wheel.mass = 120f;
            wheel.wheelDampingRate = 1f;
            wheel.forwardFriction = MakeFriction(1.6f, 0.8f);
            wheel.sidewaysFriction = MakeFriction(1.8f, 0.9f);
            visuals[index] = CreateVisual(wheelName + " Visual");
            return wheel;
        }

        private static WheelFrictionCurve MakeFriction(float stiffness, float extremum)
        {
            return new WheelFrictionCurve { extremumSlip = 0.35f, extremumValue = extremum, asymptoteSlip = 0.8f, asymptoteValue = extremum * 0.75f, stiffness = stiffness };
        }

        private Transform CreateVisual(string visualName)
        {
            var pivot = new GameObject(visualName).transform;
            pivot.SetParent(visualRoot, false);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Mesh";
            visual.transform.SetParent(pivot, false);
            visual.transform.localRotation = Quaternion.Euler(0, 0, 90);
            visual.transform.localScale = new Vector3(wheelRadius * 2f, wheelWidth * 0.5f, wheelRadius * 2f);
            Object.Destroy(visual.GetComponent<Collider>());
            return pivot;
        }

        /// <summary>
        /// Hands the rig a different set of wheel meshes (used when the procedural
        /// truck visuals replace the placeholder cylinders). The old visuals are
        /// hidden rather than destroyed so the swap is reversible.
        /// </summary>
        public void ReplaceVisuals(Transform[] replacements)
        {
            if (replacements == null) return;
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
            for (int i = 0; i < visuals.Length && i < replacements.Length; i++)
                if (replacements[i] != null) visuals[i] = replacements[i];
        }

        private void LateUpdate()
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelCollider wheel = wheels[i];
                Transform visual = visuals[i];
                if (wheel == null || visual == null) continue;

                Vector3 position;
                Quaternion rotation;
                wheel.GetWorldPose(out position, out rotation);
                visual.SetPositionAndRotation(position, rotation);
            }
        }
    }
}
