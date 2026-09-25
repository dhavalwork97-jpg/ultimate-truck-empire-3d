using UnityEngine;

namespace UltimateTruckEmpire.Bootstrap
{
    /// Lightweight runtime quality guard for Android/iOS.
    /// Keeps the existing gameplay systems unchanged while avoiding unnecessarily
    /// expensive desktop-quality defaults on mobile hardware.
    public sealed class MobilePerformanceSettings : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private float maxShadowDistance = 80f;
        [SerializeField] private int maxPixelLights = 2;

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            Application.targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 60);
            QualitySettings.vSyncCount = 0;
            QualitySettings.pixelLightCount = Mathf.Clamp(maxPixelLights, 0, 4);
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, maxShadowDistance);
            QualitySettings.softParticles = false;

            // Keep anti-aliasing bounded on mobile; never force it upward if the
            // selected quality level already disabled it.
            if (QualitySettings.antiAliasing > 2)
                QualitySettings.antiAliasing = 2;
#else
            // Keep the serialized mobile tuning values referenced when compiling
            // non-mobile editor/CI assemblies, where the mobile branch is excluded.
            _ = targetFrameRate;
            _ = maxShadowDistance;
            _ = maxPixelLights;
#endif
        }
    }
}
