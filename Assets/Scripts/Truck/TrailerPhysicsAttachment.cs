using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    /// <summary>
    /// Owns the physical truck-to-trailer connection. The trailer remains an
    /// independent rigidbody so it can articulate around the kingpin while the
    /// truck continues to use its existing WheelCollider drivetrain.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrailerPhysicsAttachment : MonoBehaviour
    {
        [Header("Hitch")]
        [SerializeField] private Transform hitchPoint;
        [SerializeField] private float defaultHitchHeight = 1.35f;
        [SerializeField] private float defaultHitchForward = -2.35f;

        [Header("Trailer tuning")]
        [SerializeField] private float minimumTrailerMass = 1200f;
        [SerializeField] private float maximumTrailerMass = 40000f;
        [SerializeField] private float angularDriveDamper = 120f;
        [SerializeField] private float projectionDistance = 0.08f;
        [SerializeField] private float projectionAngle = 4f;

        private Rigidbody truckBody;
        private ConfigurableJoint joint;
        private GameObject attachedTrailer;
        private Rigidbody trailerBody;
        private Transform attachedKingpin;

        public bool IsAttached => joint != null && attachedTrailer != null && trailerBody != null;
        public GameObject AttachedTrailer => attachedTrailer;
        public Transform HitchPoint => EnsureHitchPoint();

        private void Awake()
        {
            truckBody = GetComponent<Rigidbody>();
            EnsureHitchPoint();
        }

        public bool Attach(GameObject trailerRoot, Transform kingpin, float trailerMass = 8000f)
        {
            if (trailerRoot == null || kingpin == null || truckBody == null) return false;
            if (trailerRoot == gameObject || kingpin == transform) return false;

            Detach(false);

            trailerBody = trailerRoot.GetComponent<Rigidbody>();
            if (trailerBody == null)
                trailerBody = trailerRoot.AddComponent<Rigidbody>();

            trailerBody.isKinematic = false;
            trailerBody.useGravity = true;
            trailerBody.interpolation = RigidbodyInterpolation.Interpolate;
            trailerBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            SetTrailerMass(trailerMass);

            Transform hitch = EnsureHitchPoint();
            Vector3 offset = hitch.position - kingpin.position;
            trailerRoot.transform.position += offset;

            joint = trailerRoot.GetComponent<ConfigurableJoint>();
            if (joint == null) joint = trailerRoot.AddComponent<ConfigurableJoint>();

            joint.connectedBody = truckBody;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = trailerRoot.transform.InverseTransformPoint(kingpin.position);
            joint.connectedAnchor = truckBody.transform.InverseTransformPoint(hitch.position);

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = projectionDistance;
            joint.projectionAngle = projectionAngle;
            joint.enableCollision = false;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;

            // Spring is deliberately zero: the kingpin must articulate instead of
            // being pulled back toward straight. Damper-only yaw control suppresses
            // high-frequency oscillation without fighting low-speed reversing.
            var drive = joint.angularYZDrive;
            drive.positionSpring = 0f;
            drive.positionDamper = angularDriveDamper;
            drive.maximumForce = Mathf.Infinity;
            joint.angularYZDrive = drive;

            attachedTrailer = trailerRoot;
            attachedKingpin = kingpin;
            return true;
        }

        public void SetTrailerMass(float mass)
        {
            if (trailerBody == null) return;
            trailerBody.mass = Mathf.Clamp(mass, minimumTrailerMass, maximumTrailerMass);
        }

        public void SetPayloadWeightTons(float payloadTons, float emptyWeightTons)
        {
            SetTrailerMass(Mathf.Max(0f, emptyWeightTons) * 1000f + Mathf.Max(0f, payloadTons) * 1000f);
        }

        public void Detach(bool preserveVelocity = true)
        {
            if (joint != null)
            {
                if (preserveVelocity && trailerBody != null && truckBody != null)
                    trailerBody.linearVelocity = truckBody.linearVelocity;
                Destroy(joint);
            }

            attachedTrailer = null;
            attachedKingpin = null;
            trailerBody = null;
            joint = null;
        }

        private Transform EnsureHitchPoint()
        {
            if (hitchPoint != null) return hitchPoint;

            Transform authored = transform.Find("Trailer Hitch Point");
            if (authored == null)
            {
                var point = new GameObject("Trailer Hitch Point");
                point.transform.SetParent(transform, false);
                point.transform.localPosition = new Vector3(0f, defaultHitchHeight, defaultHitchForward);
                authored = point.transform;
            }

            hitchPoint = authored;
            return hitchPoint;
        }

        private void OnDisable()
        {
            if (joint != null) Destroy(joint);
        }
    }
}