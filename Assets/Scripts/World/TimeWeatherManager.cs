using UnityEngine;

namespace UltimateTruckEmpire.World
{
    public enum WeatherState { Clear, Cloudy, Rain, HeavyRain, Fog, Storm }

    public sealed class TimeWeatherManager : MonoBehaviour
    {
        public static TimeWeatherManager Instance { get; private set; }
        [Range(0f, 24f)] public float timeOfDay = 8f;
        public float timeScale = 0.08f;
        public WeatherState Weather { get; private set; } = WeatherState.Clear;
        private Light sun;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        private void Start() { sun = FindFirstObjectByType<Light>(); Apply(); }

        private void Update()
        {
            timeOfDay = Mathf.Repeat(timeOfDay + Time.deltaTime * timeScale, 24f);
            if (Input.GetKeyDown(KeyCode.F7)) CycleWeather();
            Apply();
        }

        public void SetWeather(WeatherState state) { Weather = state; Apply(); }
        public void CycleWeather() { Weather = (WeatherState)(((int)Weather + 1) % 6); Apply(); }

        private void Apply()
        {
            float daylight = Mathf.Clamp01(Mathf.Sin((timeOfDay - 6f) * Mathf.PI / 12f));
            RenderSettings.ambientIntensity = Mathf.Lerp(.18f, 1.1f, daylight);
            RenderSettings.fog = true;
            RenderSettings.fogDensity = Weather == WeatherState.Fog ? .018f : Weather == WeatherState.HeavyRain || Weather == WeatherState.Storm ? .009f : .004f;
            if (sun != null) { sun.intensity = Mathf.Lerp(.15f, 1.2f, daylight); sun.transform.rotation = Quaternion.Euler((timeOfDay - 6f) * 15f - 90f, -30f, 0f); }
        }
    }
}
