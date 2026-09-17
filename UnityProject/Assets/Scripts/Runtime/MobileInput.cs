using UnityEngine;

namespace UltimateTruckEmpire
{
    public sealed class MobileInput : MonoBehaviour
    {
        [SerializeField] private TruckController truck;

        private float steering;
        private float throttle;
        private bool brake;

        private void Awake()
        {
            if (truck == null)
                truck = FindFirstObjectByType<TruckController>();
        }

        private void Update()
        {
            if (truck == null) return;
            truck.SetSteering(steering);
            truck.SetThrottle(throttle);
            truck.SetBraking(brake);
        }

        public void SetSteering(float value) => steering = Mathf.Clamp(value, -1f, 1f);
        public void SetThrottle(float value) => throttle = Mathf.Clamp(value, -1f, 1f);
        public void SetBrake(bool value) => brake = value;
    }
}
