using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TruckController : MonoBehaviour
    {
        [SerializeField] private WheelCollider frontLeft, frontRight, rearLeft, rearRight;
        [SerializeField] private float motorTorque = 2200f;
        [SerializeField] private float brakeTorque = 7000f;
        [SerializeField] private float maxSteerAngle = 32f;
        [SerializeField] private float maxForwardKph = 110f;
        [SerializeField] private float reverseTorqueMultiplier = 0.45f;
        [SerializeField] private TruckLights lights;
        private Rigidbody body;
        private bool engineRunning = true;
        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float Fuel { get; private set; } = 100f;
        public bool EngineRunning => engineRunning;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 8000f;
            body.centerOfMass = new Vector3(0f, -0.65f, 0.15f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void ConfigureWheels(WheelCollider fl, WheelCollider fr, WheelCollider rl, WheelCollider rr)
        {
            frontLeft = fl; frontRight = fr; rearLeft = rl; rearRight = rr;
        }

        private void FixedUpdate()
        {
            float steer = Input.GetAxisRaw("Horizontal");
            float throttle = engineRunning ? Input.GetAxisRaw("Vertical") : 0f;
            bool braking = Input.GetKey(KeyCode.Space);
            float speedFactor = Mathf.InverseLerp(0f, maxForwardKph, SpeedKph);
            float steerLimit = Mathf.Lerp(maxSteerAngle, maxSteerAngle * 0.35f, speedFactor);
            if (frontLeft) frontLeft.steerAngle = steer * steerLimit;
            if (frontRight) frontRight.steerAngle = steer * steerLimit;
            float torque = throttle > 0f ? motorTorque * (1f - speedFactor) : throttle * motorTorque * reverseTorqueMultiplier;
            if (SpeedKph >= maxForwardKph && throttle > 0f) torque = 0f;
            SetMotor(rearLeft, torque); SetMotor(rearRight, torque);
            SetBrake(braking ? brakeTorque : 0f);
            if (lights) lights.SetBrakes(braking);
            if (Mathf.Abs(throttle) > 0.1f && engineRunning) Fuel = Mathf.Max(0f, Fuel - Time.fixedDeltaTime * (0.0015f + SpeedKph * 0.00002f));
            if (Input.GetKeyDown(KeyCode.I)) engineRunning = !engineRunning;
        }

        private static void SetMotor(WheelCollider wheel, float torque) { if (wheel) wheel.motorTorque = torque; }
        private void SetBrake(float torque)
        {
            if (frontLeft) frontLeft.brakeTorque = torque; if (frontRight) frontRight.brakeTorque = torque;
            if (rearLeft) rearLeft.brakeTorque = torque; if (rearRight) rearRight.brakeTorque = torque;
        }
    }
}
