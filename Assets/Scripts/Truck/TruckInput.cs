using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    public sealed class TruckInput : MonoBehaviour
    {
        public float Steering => Input.GetAxisRaw("Horizontal");
        public float Throttle => Input.GetAxisRaw("Vertical");
        public bool Handbrake => Input.GetKey(KeyCode.Space);
        public bool EngineTogglePressed => Input.GetKeyDown(KeyCode.I);
        public bool HeadlightsPressed => Input.GetKeyDown(KeyCode.L);
        public bool Horn => Input.GetKey(KeyCode.H);
    }
}
