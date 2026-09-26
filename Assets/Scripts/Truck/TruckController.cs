using UnityEngine;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Truck
{
    public enum GearState { Park, Reverse, Neutral, Drive }

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

        [Header("Steering feel")]
        [SerializeField] private float steerRate = 2.6f;
        [SerializeField] private float steerReturnRate = 4.2f;
        [SerializeField] private float highSpeedSteerFactor = 0.35f;

        [Header("Gearbox")]
        [SerializeField] private KeyCode gearKey = KeyCode.G;
        [SerializeField] private float shiftSpeedLimitKph = 5f;
        [SerializeField] private float autoShiftHoldTime = 0.35f;

        [Header("Cruise control")]
        [SerializeField] private KeyCode cruiseKey = KeyCode.V;
        [SerializeField] private float minCruiseKph = 25f;

        private Rigidbody body;
        private bool engineRunning = true;
        private float steerInput, throttleInput;
        private bool brakingInput;
        private float autoShiftTimer;
        private float cruiseSpeedKph;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float Fuel { get; private set; } = 100f;
        public bool EngineRunning => engineRunning;
        public GearState Gear { get; private set; } = GearState.Drive;
        public float SteerInput => steerInput;
        public float ThrottleInput => throttleInput;
        public bool Braking => brakingInput || Gear == GearState.Park;
        public bool CruiseActive { get; private set; }
        public float CruiseSpeedKph => cruiseSpeedKph;
        public bool LimiterActive { get; private set; }
        public bool Reversing => Gear == GearState.Reverse;
        public float FuelCapacity { get; private set; } = 100f;
        public float FuelEfficiency { get; private set; } = 3.2f;
        public float Condition { get; private set; } = 100f;
        public string FleetTruckId { get; private set; } = "";
        private static long nextRuntimeEntityId;
        private string runtimeEntityId;

        /// <summary>
        /// Returns the stable identity used by gameplay systems for this truck.
        /// Fleet-backed trucks use their persisted fleet ID. Runtime-only trucks
        /// receive a process-local ID when they awaken, without relying on
        /// UnityEngine.Object.GetInstanceID().
        /// </summary>
        public string GetEntityId()
        {
            if (!string.IsNullOrEmpty(FleetTruckId))
                return FleetTruckId;

            if (string.IsNullOrEmpty(runtimeEntityId))
                runtimeEntityId = "runtime-truck-" + (++nextRuntimeEntityId);

            return runtimeEntityId;
        }

        public void ApplyFleetConfiguration(FleetTruckData truck)
        {
            if (truck == null) return;
            FleetTruckId = truck.id ?? "";
            FuelCapacity = Mathf.Max(1f, truck.fuelCapacity);
            Fuel = Mathf.Clamp(truck.fuel, 0f, FuelCapacity);
            FuelEfficiency = Mathf.Max(0.1f, truck.fuelEfficiency);
            Condition = Mathf.Clamp(truck.condition, 0f, 100f);
            motorTorque = Mathf.Max(800f, 2200f * (truck.enginePower / 300f));
            maxForwardKph = Mathf.Max(30f, truck.maxSpeedKph);
        }

        public float GetFuelLitres() => Fuel;
        public void SetFleetFuel(float litres) => Fuel = Mathf.Clamp(litres, 0f, FuelCapacity);

        public int GetAxleCount()
        {
            int wheelCount = 0;
            if (frontLeft != null) wheelCount++;
            if (frontRight != null) wheelCount++;
            if (rearLeft != null) wheelCount++;
            if (rearRight != null) wheelCount++;
            return Mathf.Max(1, Mathf.CeilToInt(wheelCount * 0.5f));
        }

        public string GearLabel
        {
            get
            {
                switch (Gear)
                {
                    case GearState.Park: return "P";
                    case GearState.Reverse: return "R";
                    case GearState.Neutral: return "N";
                    default: return "D";
                }
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = 8000f;
            body.centerOfMass = new Vector3(0f, -0.65f, 0.15f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (lights == null) lights = GetComponent<TruckLights>();
        }

        public void ConfigureWheels(WheelCollider fl, WheelCollider fr, WheelCollider rl, WheelCollider rr)
        {
            frontLeft = fl; frontRight = fr; rearLeft = rl; rearRight = rr;
        }

        public void AttachLights(TruckLights truckLights) => lights = truckLights;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I) || MobileInputState.ConsumeEngineToggle()) engineRunning = !engineRunning;
            if (Input.GetKeyDown(gearKey) || MobileInputState.ConsumeGear()) CycleGear();
            if (Input.GetKeyDown(cruiseKey)) ToggleCruise();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            ReadInput(dt);
            UpdateAutoShift(dt);
            ApplySteering();
            ApplyDrive();
            ConsumeFuel(dt);

            if (lights != null)
            {
                lights.SetBrakes(brakingInput || (Gear == GearState.Drive && throttleInput < -0.1f && SpeedKph > 1f));
                lights.SetReverse(Gear == GearState.Reverse);
            }
        }

        private void ReadInput(float dt)
        {
            float rawSteer = Mathf.Clamp(Input.GetAxisRaw("Horizontal") + MobileInputState.Steering, -1f, 1f);
            float rate = Mathf.Abs(rawSteer) > 0.01f ? steerRate : steerReturnRate;
            steerInput = Mathf.MoveTowards(steerInput, rawSteer, rate * dt);
            throttleInput = Mathf.Clamp(Input.GetAxisRaw("Vertical") + MobileInputState.Throttle, -1f, 1f);
            brakingInput = Input.GetKey(KeyCode.Space) || MobileInputState.Brake;
            if (brakingInput || (CruiseActive && throttleInput < -0.1f)) CruiseActive = false;
        }

        private void CycleGear()
        {
            bool stopped = SpeedKph <= shiftSpeedLimitKph;
            switch (Gear)
            {
                case GearState.Park: Gear = GearState.Reverse; break;
                case GearState.Reverse: Gear = GearState.Neutral; break;
                case GearState.Neutral: Gear = GearState.Drive; break;
                default: Gear = stopped ? GearState.Park : GearState.Neutral; break;
            }
            if (!stopped && (Gear == GearState.Park || Gear == GearState.Reverse)) Gear = GearState.Neutral;
            CruiseActive = false;
            autoShiftTimer = 0f;
        }

        private void ToggleCruise()
        {
            if (CruiseActive) { CruiseActive = false; return; }
            if (Gear != GearState.Drive || !engineRunning || SpeedKph < minCruiseKph) return;
            cruiseSpeedKph = Mathf.Min(SpeedKph, maxForwardKph);
            CruiseActive = true;
        }

        private void UpdateAutoShift(float dt)
        {
            if (SpeedKph > shiftSpeedLimitKph || Gear == GearState.Park || Gear == GearState.Neutral)
            {
                autoShiftTimer = 0f;
                return;
            }

            bool wantsReverse = Gear == GearState.Drive && throttleInput < -0.1f;
            bool wantsForward = Gear == GearState.Reverse && throttleInput > 0.1f;
            if (!wantsReverse && !wantsForward) { autoShiftTimer = 0f; return; }

            autoShiftTimer += dt;
            if (autoShiftTimer < autoShiftHoldTime) return;
            Gear = wantsReverse ? GearState.Reverse : GearState.Drive;
            autoShiftTimer = 0f;
            CruiseActive = false;
        }

        private void ApplySteering()
        {
            float speedFactor = Mathf.InverseLerp(0f, maxForwardKph, SpeedKph);
            float steerLimit = Mathf.Lerp(maxSteerAngle, maxSteerAngle * highSpeedSteerFactor, speedFactor);
            float angle = steerInput * steerLimit;
            if (frontLeft) frontLeft.steerAngle = angle;
            if (frontRight) frontRight.steerAngle = angle;
        }

        private void ApplyDrive()
        {
            LimiterActive = false;
            if (Gear == GearState.Park) { SetMotor(0f); SetBrake(brakeTorque); return; }
            if (!engineRunning || Gear == GearState.Neutral) { SetMotor(0f); SetBrake(brakingInput ? brakeTorque : 0f); return; }

            float speedFactor = Mathf.InverseLerp(0f, maxForwardKph, SpeedKph);
            float torque = 0f;
            float brake = brakingInput ? brakeTorque : 0f;

            if (Gear == GearState.Drive)
            {
                if (CruiseActive)
                {
                    float error = cruiseSpeedKph - SpeedKph;
                    torque = Mathf.Max(0f, Mathf.Clamp(error * 0.12f, -0.2f, 1f)) * motorTorque * (1f - speedFactor * 0.5f);
                }
                else if (throttleInput > 0.05f) torque = throttleInput * motorTorque * (1f - speedFactor);
                else if (throttleInput < -0.05f) brake = Mathf.Max(brake, brakeTorque * 0.85f * -throttleInput);
                if (SpeedKph >= maxForwardKph) { torque = 0f; LimiterActive = true; }
            }
            else
            {
                if (throttleInput < -0.05f) torque = throttleInput * motorTorque * reverseTorqueMultiplier;
                else if (throttleInput > 0.05f) brake = Mathf.Max(brake, brakeTorque * 0.85f * throttleInput);
                if (SpeedKph >= maxForwardKph * 0.35f) { torque = 0f; LimiterActive = true; }
            }

            SetMotor(torque);
            SetBrake(brake);
        }

        private void ConsumeFuel(float dt)
        {
            if (!engineRunning) return;
            if (Mathf.Abs(throttleInput) > 0.1f || CruiseActive)
                Fuel = Mathf.Max(0f, Fuel - dt * (0.0015f + SpeedKph * 0.00002f) * (3.2f / FuelEfficiency));
            if (Fuel <= 0f) { engineRunning = false; CruiseActive = false; }
        }

        public void Refuel(float amount) => Fuel = Mathf.Clamp(Fuel + amount, 0f, FuelCapacity);

        private void SetMotor(float torque) { SetMotor(rearLeft, torque); SetMotor(rearRight, torque); }
        private static void SetMotor(WheelCollider wheel, float torque) { if (wheel) wheel.motorTorque = torque; }

        private void SetBrake(float torque)
        {
            if (frontLeft) frontLeft.brakeTorque = torque;
            if (frontRight) frontRight.brakeTorque = torque;
            if (rearLeft) rearLeft.brakeTorque = torque;
            if (rearRight) rearRight.brakeTorque = torque;
        }
    }
}
