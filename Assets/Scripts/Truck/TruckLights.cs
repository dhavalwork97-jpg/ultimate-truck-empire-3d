using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    public sealed class TruckLights : MonoBehaviour
    {
        [SerializeField] private Light[] headlights;
        [SerializeField] private Light[] brakeLights;
        [SerializeField] private Light[] leftIndicators;
        [SerializeField] private Light[] rightIndicators;

        private bool headlightsOn;
        private bool leftOn;
        private bool rightOn;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L)) headlightsOn = !headlightsOn;
            if (Input.GetKeyDown(KeyCode.Q)) { leftOn = !leftOn; rightOn = false; }
            if (Input.GetKeyDown(KeyCode.R)) { rightOn = !rightOn; leftOn = false; }
            Apply(headlights, headlightsOn);
            Apply(leftIndicators, leftOn);
            Apply(rightIndicators, rightOn);
        }

        private static void Apply(Light[] lights, bool enabled)
        {
            if (lights == null) return;
            foreach (var light in lights) if (light != null) light.enabled = enabled;
        }

        public void SetBrakes(bool enabled) => Apply(brakeLights, enabled);
    }
}
