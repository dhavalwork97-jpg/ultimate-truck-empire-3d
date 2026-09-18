using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// All of the scene's atmosphere in one place: sun angle and colour, ambient
    /// level, fog, skybox and the night/weather hooks.
    ///
    /// It owns no update loop. <see cref="TimeWeatherManager"/> already ticks time
    /// of day and calls in here, so there is exactly one system driving lighting.
    ///
    /// Cost per call is a handful of property writes plus, at most, a few shader
    /// property sets on ONE cached skybox material. Nothing allocates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentAtmosphere : MonoBehaviour
    {
        public static EnvironmentAtmosphere Instance { get; private set; }

        [Header("Sun")]
        public Light sun;
        public float dayIntensity = 1.35f;
        public float nightIntensity = 0.14f;

        [Header("Fog")]
        public float clearFogDensity = 0.0035f;

        private static readonly Color DaySunColour = new Color(1f, 0.96f, 0.88f);
        private static readonly Color HorizonSunColour = new Color(1f, 0.58f, 0.33f);
        private static readonly Color MoonColour = new Color(0.55f, 0.66f, 0.92f);

        private static readonly Color DayAmbient = new Color(0.52f, 0.545f, 0.575f);
        private static readonly Color DuskAmbient = new Color(0.30f, 0.26f, 0.28f);
        // Deliberately not black: the road has to stay readable at night.
        private static readonly Color NightAmbient = new Color(0.115f, 0.135f, 0.185f);

        private static readonly Color DayFog = new Color(0.63f, 0.69f, 0.76f);
        private static readonly Color DuskFog = new Color(0.52f, 0.40f, 0.34f);
        private static readonly Color NightFog = new Color(0.055f, 0.065f, 0.095f);

        private Material skyboxMaterial;
        private float lastAppliedTime = -99f;
        private float lastSkyTime = -99f;
        private int lastWeather = -1;
        private bool nightState;

        public static EnvironmentAtmosphere Ensure()
        {
            if (Instance != null) return Instance;
            Instance = FindFirstObjectByType<EnvironmentAtmosphere>();
            if (Instance == null) Instance = new GameObject("Environment Atmosphere").AddComponent<EnvironmentAtmosphere>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            SetupSkybox();
            ResolveSun();
        }

        private void ResolveSun()
        {
            if (sun != null && sun.type == LightType.Directional) return;

            // Must be the directional light specifically - picking "the first Light
            // in the scene" finds a street lamp and silently breaks the sky.
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional) { sun = lights[i]; break; }
            }
            if (sun != null) RenderSettings.sun = sun;
        }

        private void SetupSkybox()
        {
            Shader procedural = Shader.Find("Skybox/Procedural");
            if (procedural == null) return;

            skyboxMaterial = new Material(procedural);
            skyboxMaterial.name = "UTE Procedural Sky";
            if (skyboxMaterial.HasProperty("_SunSize")) skyboxMaterial.SetFloat("_SunSize", 0.045f);
            if (skyboxMaterial.HasProperty("_SunSizeConvergence")) skyboxMaterial.SetFloat("_SunSizeConvergence", 6f);
            RenderSettings.skybox = skyboxMaterial;
        }

        /// <summary>
        /// Applies a time of day (0-24) and weather state. Safe to call every
        /// frame; the expensive parts only run when something actually changed.
        /// </summary>
        public void Apply(float timeOfDay, WeatherState weather)
        {
            if (sun == null) ResolveSun();

            float daylight = Mathf.Clamp01(Mathf.Sin((timeOfDay - 6f) * Mathf.PI / 12f));
            float horizon = Mathf.Clamp01(1f - Mathf.Abs(daylight - 0.18f) / 0.18f);  // peaks at dawn/dusk
            bool isNight = daylight < 0.04f;

            float weatherLight, weatherFog;
            bool wet;
            WeatherFactors(weather, out weatherLight, out weatherFog, out wet);

            ApplySun(timeOfDay, daylight, horizon, isNight, weatherLight);
            ApplyAmbient(daylight, horizon, weatherLight);
            ApplyFog(daylight, horizon, weatherFog);
            ApplySky(timeOfDay, daylight, horizon, weather, weatherLight);

            WorldPalette.SetWetRoads(wet);

            if (nightState != (daylight < 0.16f))
            {
                nightState = daylight < 0.16f;
                StreetLightManager.SetNight(nightState);
            }

            lastAppliedTime = timeOfDay;
            lastWeather = (int)weather;
        }

        private static void WeatherFactors(WeatherState weather, out float light, out float fog, out bool wet)
        {
            switch (weather)
            {
                case WeatherState.Cloudy: light = 0.62f; fog = 0.0055f; wet = false; break;
                case WeatherState.Rain: light = 0.40f; fog = 0.0090f; wet = true; break;
                case WeatherState.HeavyRain: light = 0.28f; fog = 0.0125f; wet = true; break;
                case WeatherState.Fog: light = 0.48f; fog = 0.0220f; wet = false; break;
                case WeatherState.Storm: light = 0.22f; fog = 0.0145f; wet = true; break;
                default: light = 1f; fog = 0.0035f; wet = false; break;
            }
        }

        private void ApplySun(float timeOfDay, float daylight, float horizon, bool isNight, float weatherLight)
        {
            if (sun == null) return;

            if (isNight)
            {
                // A low, cool key light stands in for moonlight so night still has
                // shape instead of being a flat black screen.
                sun.transform.rotation = Quaternion.Euler(52f, 200f, 0f);
                sun.color = MoonColour;
                sun.intensity = nightIntensity * Mathf.Lerp(1f, 0.55f, 1f - weatherLight);
                sun.shadows = LightShadows.None;
                return;
            }

            // 06:00 on the horizon, 12:00 overhead, 18:00 on the opposite horizon.
            sun.transform.rotation = Quaternion.Euler((timeOfDay - 6f) * 15f, 170f, 0f);
            sun.color = Color.Lerp(DaySunColour, HorizonSunColour, horizon);
            sun.intensity = Mathf.Lerp(0f, dayIntensity, Mathf.Clamp01(daylight * 1.6f)) * weatherLight;
            sun.shadows = daylight > 0.12f && weatherLight > 0.35f ? LightShadows.Soft : LightShadows.None;
        }

        private void ApplyAmbient(float daylight, float horizon, float weatherLight)
        {
            Color ambient = daylight < 0.2f
                ? Color.Lerp(NightAmbient, DuskAmbient, Mathf.Clamp01(daylight / 0.2f))
                : Color.Lerp(DuskAmbient, DayAmbient, Mathf.Clamp01((daylight - 0.2f) / 0.8f));

            ambient = Color.Lerp(ambient * 0.75f, ambient, weatherLight);
            RenderSettings.ambientLight = ambient;
            RenderSettings.ambientIntensity = Mathf.Lerp(0.45f, 1.05f, daylight) * Mathf.Lerp(0.8f, 1f, weatherLight);
        }

        private void ApplyFog(float daylight, float horizon, float weatherFog)
        {
            Color fog = daylight < 0.2f
                ? Color.Lerp(NightFog, DuskFog, Mathf.Clamp01(daylight / 0.2f))
                : Color.Lerp(DuskFog, DayFog, Mathf.Clamp01((daylight - 0.2f) / 0.8f));

            RenderSettings.fog = true;
            RenderSettings.fogColor = fog;
            // Slightly thicker after dark, which also hides the edge of the world.
            RenderSettings.fogDensity = weatherFog * Mathf.Lerp(1.35f, 1f, daylight);
        }

        private void ApplySky(float timeOfDay, float daylight, float horizon, WeatherState weather, float weatherLight)
        {
            if (skyboxMaterial == null) return;

            // Sky properties only move when the state actually changed, so the
            // common case costs one float comparison per frame.
            bool weatherChanged = lastWeather != (int)weather;
            if (!weatherChanged && Mathf.Abs(timeOfDay - lastSkyTime) < 0.04f) return;
            lastSkyTime = timeOfDay;

            Color skyTint = Color.Lerp(new Color(0.34f, 0.42f, 0.56f), new Color(0.52f, 0.60f, 0.72f), daylight);
            skyTint = Color.Lerp(skyTint, new Color(0.62f, 0.42f, 0.32f), horizon * 0.8f);
            if (weather != WeatherState.Clear)
                skyTint = Color.Lerp(skyTint, new Color(0.40f, 0.42f, 0.45f), 1f - weatherLight);

            if (skyboxMaterial.HasProperty("_SkyTint")) skyboxMaterial.SetColor("_SkyTint", skyTint);
            if (skyboxMaterial.HasProperty("_GroundColor"))
                skyboxMaterial.SetColor("_GroundColor", Color.Lerp(new Color(0.06f, 0.07f, 0.09f), new Color(0.30f, 0.29f, 0.26f), daylight));
            if (skyboxMaterial.HasProperty("_AtmosphereThickness"))
                skyboxMaterial.SetFloat("_AtmosphereThickness", Mathf.Lerp(1.9f, 0.95f, daylight));
            if (skyboxMaterial.HasProperty("_Exposure"))
                skyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(0.32f, 1.25f, daylight) * Mathf.Lerp(0.7f, 1f, weatherLight));
        }
    }
}
