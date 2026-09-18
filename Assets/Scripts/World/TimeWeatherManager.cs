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
        private EnvironmentAtmosphere atmosphere;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        // The old lookup grabbed "the first Light in the scene", which finds a
        // street lamp as often as the sun. Lighting now lives in one system.
        private void Start() { atmosphere = EnvironmentAtmosphere.Ensure(); Apply(); }

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
            if (atmosphere == null) atmosphere = EnvironmentAtmosphere.Ensure();
            if (atmosphere != null) atmosphere.Apply(timeOfDay, Weather);
        }

        /// <summary>Normalised daylight, 0 at night and 1 at midday. Handy for UI.</summary>
        public float Daylight => Mathf.Clamp01(Mathf.Sin((timeOfDay - 6f) * Mathf.PI / 12f));
    }
}
