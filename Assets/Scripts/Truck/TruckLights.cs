using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    /// <summary>
    /// Vehicle lighting state and the lamps that show it.
    ///
    /// Keeps the original serialized Light arrays and the SetBrakes entry point,
    /// and adds the state the HUD and camera presentation need: indicator blink,
    /// hazards, reverse lamps and the horn.
    ///
    /// Lamp count is deliberately small - two headlight spots plus cheap emissive
    /// lamp bodies - so a lit truck never costs a pile of realtime lights.
    /// </summary>
    public sealed class TruckLights : MonoBehaviour
    {
        [SerializeField] private Light[] headlights;
        [SerializeField] private Light[] brakeLights;
        [SerializeField] private Light[] leftIndicators;
        [SerializeField] private Light[] rightIndicators;
        [SerializeField] private Light[] reverseLights;

        [Header("Keys")]
        [SerializeField] private KeyCode headlightKey = KeyCode.L;
        [SerializeField] private KeyCode leftIndicatorKey = KeyCode.Q;
        [SerializeField] private KeyCode rightIndicatorKey = KeyCode.R;
        [SerializeField] private KeyCode hazardKey = KeyCode.Z;
        [SerializeField] private KeyCode hornKey = KeyCode.H;

        [Header("Blink")]
        [SerializeField] private float blinkPeriod = 0.7f;

        private bool headlightsOn, leftOn, rightOn, hazardsOn, brakesOn, reverseOn;
        private float blinkTimer;
        private bool blinkPhase;

        public bool HeadlightsOn => headlightsOn;
        public bool LeftIndicatorOn => leftOn || hazardsOn;
        public bool RightIndicatorOn => rightOn || hazardsOn;
        public bool HazardsOn => hazardsOn;
        public bool BrakesOn => brakesOn;
        public bool ReverseOn => reverseOn;
        public bool BlinkPhase => blinkPhase;
        public bool HornActive => Input.GetKey(hornKey);

        public void Configure(Light[] head, Light[] brake, Light[] left, Light[] right, Light[] reverse)
        {
            headlights = head; brakeLights = brake; leftIndicators = left; rightIndicators = right; reverseLights = reverse;
        }

        private void Update()
        {
            if (Input.GetKeyDown(headlightKey)) headlightsOn = !headlightsOn;
            if (Input.GetKeyDown(leftIndicatorKey)) { leftOn = !leftOn; rightOn = false; }
            if (Input.GetKeyDown(rightIndicatorKey)) { rightOn = !rightOn; leftOn = false; }
            if (Input.GetKeyDown(hazardKey)) { hazardsOn = !hazardsOn; if (hazardsOn) { leftOn = false; rightOn = false; } }

            bool blinking = LeftIndicatorOn || RightIndicatorOn;
            if (blinking)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkPeriod * 0.5f) { blinkTimer = 0f; blinkPhase = !blinkPhase; }
            }
            else
            {
                blinkTimer = 0f;
                blinkPhase = false;
            }

            Apply(headlights, headlightsOn);
            Apply(leftIndicators, LeftIndicatorOn && blinkPhase);
            Apply(rightIndicators, RightIndicatorOn && blinkPhase);
        }

        private static void Apply(Light[] lights, bool enabled)
        {
            if (lights == null) return;
            foreach (var light in lights) if (light != null) light.enabled = enabled;
        }

        public void SetBrakes(bool enabled)
        {
            brakesOn = enabled;
            Apply(brakeLights, enabled);
        }

        public void SetReverse(bool enabled)
        {
            reverseOn = enabled;
            Apply(reverseLights, enabled);
        }

        public void SetHeadlights(bool enabled)
        {
            headlightsOn = enabled;
            Apply(headlights, enabled);
        }
    }
}
