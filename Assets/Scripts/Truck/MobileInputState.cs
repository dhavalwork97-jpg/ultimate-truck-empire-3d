using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    /// <summary>
    /// Frame-safe bridge between touch UI and the existing WheelCollider controller.
    /// Keyboard input remains fully supported; mobile values are simply overlaid.
    /// </summary>
    public static class MobileInputState
    {
        public static float Steering { get; set; }
        public static float Throttle { get; set; }
        public static bool Brake { get; set; }
        public static bool Horn { get; set; }

        public static bool ConsumeEngineToggle()
        {
            bool value = engineToggle;
            engineToggle = false;
            return value;
        }

        public static bool ConsumeGear()
        {
            bool value = gearToggle;
            gearToggle = false;
            return value;
        }

        public static bool ConsumeHeadlights()
        {
            bool value = headlightsToggle;
            headlightsToggle = false;
            return value;
        }

        public static bool ConsumeLeftIndicator()
        {
            bool value = leftIndicatorToggle;
            leftIndicatorToggle = false;
            return value;
        }

        public static bool ConsumeRightIndicator()
        {
            bool value = rightIndicatorToggle;
            rightIndicatorToggle = false;
            return value;
        }

        public static bool ConsumeHazards()
        {
            bool value = hazardsToggle;
            hazardsToggle = false;
            return value;
        }

        private static bool engineToggle, gearToggle, headlightsToggle;
        private static bool leftIndicatorToggle, rightIndicatorToggle, hazardsToggle;

        public static void RequestEngineToggle() => engineToggle = true;
        public static void RequestGear() => gearToggle = true;
        public static void RequestHeadlights() => headlightsToggle = true;
        public static void RequestLeftIndicator() => leftIndicatorToggle = true;
        public static void RequestRightIndicator() => rightIndicatorToggle = true;
        public static void RequestHazards() => hazardsToggle = true;

        public static void Reset()
        {
            Steering = 0f; Throttle = 0f; Brake = false; Horn = false;
            engineToggle = gearToggle = headlightsToggle = false;
            leftIndicatorToggle = rightIndicatorToggle = hazardsToggle = false;
        }
    }
}