using UnityEngine;

namespace UltimateTruckEmpire
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TruckController : MonoBehaviour
    {
        [Header("Driving")]
        [SerializeField] private float acceleration = 9f;
        [SerializeField] private float reverseAcceleration = 4f;
        [SerializeField] private float brakeForce = 14f;
        [SerializeField] private float maxForwardSpeed = 28f;
        [SerializeField] private float maxReverseSpeed = 8f;
        [SerializeField] private float steering = 55f;
        [SerializeField] private float rollingResistance = 1.5f;

        private Rigidbody body;
        private float throttle;
        private float steeringInput;
        private bool braking;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float Throttle => throttle;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0f, -0.45f, 0.1f);
        }

        public void SetThrottle(float value) => throttle = Mathf.Clamp(value, -1f, 1f);
        public void SetSteering(float value) => steeringInput = Mathf.Clamp(value, -1f, 1f);
        public void SetBraking(bool value) => braking = value;

        private void Update()
        {
            if (Application.isMobilePlatform)
                return;

            SetThrottle(Input.GetAxisRaw("Vertical"));
            SetSteering(Input.GetAxisRaw("Horizontal"));
            SetBraking(Input.GetKey(KeyCode.Space));
        }

        private void FixedUpdate()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(body.linearVelocity);
            float forwardLimit = maxForwardSpeed;
            float reverseLimit = maxReverseSpeed;

            if (throttle > 0f && localVelocity.z < forwardLimit)
                body.AddForce(transform.forward * (throttle * acceleration), ForceMode.Acceleration);
            else if (throttle < 0f && localVelocity.z > -reverseLimit)
                body.AddForce(transform.forward * (throttle * reverseAcceleration), ForceMode.Acceleration);

            if (braking)
                body.AddForce(-body.linearVelocity.normalized * brakeForce, ForceMode.Acceleration);
            else if (Mathf.Abs(throttle) < 0.01f)
                body.AddForce(-body.linearVelocity * rollingResistance, ForceMode.Acceleration);

            float speedFactor = Mathf.Clamp01(Mathf.Abs(localVelocity.z) / 4f);
            float direction = localVelocity.z >= 0f ? 1f : -1f;
            float yaw = steeringInput * steering * speedFactor * direction * Time.fixedDeltaTime;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw, 0f));
        }
    }
}
