using UnityEngine;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Lightweight runtime presentation layer for the mobile build.
    /// Adds procedural engine/wind/rain audio without
    /// requiring external audio or VFX assets. Gameplay state remains owned
    /// by the existing truck and weather systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldImmersionFX : MonoBehaviour
    {
        private TruckController truck;
        private TimeWeatherManager weather;
        private AudioSource engineSource;
        private AudioSource windSource;
        private AudioSource rainSource;
        private float targetRain;
        private float targetWind;

        private static AudioClip engineClip;
        private static AudioClip windClip;
        private static AudioClip rainClip;

        public static WorldImmersionFX Ensure()
        {
            var existing = FindFirstObjectByType<WorldImmersionFX>();
            if (existing != null) return existing;
            return new GameObject("World Immersion FX").AddComponent<WorldImmersionFX>();
        }

        private void Awake()
        {
            weather = TimeWeatherManager.Instance;
            BuildAudio();
        }

        private void Update()
        {
            if (weather == null) weather = TimeWeatherManager.Instance;
            if (truck == null) truck = FindFirstObjectByType<TruckController>();

            float speed = truck != null ? truck.SpeedKph : 0f;
            bool engineOn = truck != null && truck.EngineRunning;
            float throttle = truck != null ? Mathf.Abs(truck.ThrottleInput) : 0f;

            float engineT = Mathf.Clamp01(speed / 100f);
            if (engineSource != null)
            {
                engineSource.pitch = Mathf.Lerp(0.72f, 1.42f, engineT) + throttle * 0.10f;
                engineSource.volume = engineOn ? Mathf.Lerp(0.16f, 0.42f, engineT) + throttle * 0.08f : 0f;
            }

            float windT = Mathf.Clamp01(speed / 90f);
            if (windSource != null)
            {
                windSource.volume = Mathf.Lerp(0.015f, 0.20f, windT) + targetWind * 0.035f;
                windSource.pitch = Mathf.Lerp(0.78f, 1.12f, windT) + targetWind * 0.04f;
            }

            WeatherState state = weather != null ? weather.Weather : WeatherState.Clear;
            targetRain = RainAmount(state);
            targetWind = WindAmount(state);

            if (rainSource != null)
            {
                rainSource.volume = Mathf.MoveTowards(rainSource.volume, targetRain * 0.24f, Time.deltaTime * 0.5f);
                rainSource.pitch = Mathf.Lerp(0.88f, 1.08f, targetRain);
            }

        }

        private void BuildAudio()
        {
            if (engineClip == null) engineClip = CreateEngineClip();
            if (windClip == null) windClip = CreateNoiseClip("UTE Wind", 2.0f, 0.16f);
            if (rainClip == null) rainClip = CreateNoiseClip("UTE Rain", 2.0f, 0.30f);

            engineSource = CreateSource("Engine Audio", engineClip, true, 0.25f, 0.72f);
            windSource = CreateSource("Cabin Wind", windClip, true, 0f, 0.90f);
            rainSource = CreateSource("Rain Audio", rainClip, true, 0f, 0.95f);

            engineSource.spatialBlend = 0f;
            windSource.spatialBlend = 0f;
            rainSource.spatialBlend = 0f;
        }

        private AudioSource CreateSource(string name, AudioClip clip, bool loop, float volume, float spatialBlend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = true;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.priority = 64;
            source.dopplerLevel = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.Play();
            return source;
        }

        private static AudioClip CreateEngineClip()
        {
            const int sampleRate = 22050;
            const int seconds = 2;
            int samples = sampleRate * seconds;
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float fundamental = 58f;
                float phase = 2f * Mathf.PI * fundamental * t;
                float value =
                    Mathf.Sin(phase) * 0.36f +
                    Mathf.Sin(phase * 2.01f) * 0.20f +
                    Mathf.Sin(phase * 3.02f) * 0.11f +
                    Mathf.Sin(phase * 6.04f) * 0.045f;

                // Subtle low-frequency modulation prevents a sterile constant tone.
                value *= 0.82f + 0.18f * Mathf.Sin(2f * Mathf.PI * 2.2f * t);
                data[i] = Mathf.Clamp(value, -0.85f, 0.85f);
            }

            var clip = AudioClip.Create("UTE Procedural Engine", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateNoiseClip(string name, float seconds, float amplitude)
        {
            const int sampleRate = 22050;
            int samples = Mathf.RoundToInt(sampleRate * seconds);
            var data = new float[samples];
            uint seed = 0x9E3779B9u;

            for (int i = 0; i < samples; i++)
            {
                // Tiny deterministic PRNG keeps the generated asset repeatable.
                seed ^= seed << 13;
                seed ^= seed >> 17;
                seed ^= seed << 5;
                float n = ((seed & 0xFFFFu) / 32767.5f) - 1f;
                data[i] = n * amplitude;
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void BuildRain()
        {
            rainParticles = CreateRainSystem("Rain", 700, 11f, 0.55f, 1.2f);
            heavyRainParticles = CreateRainSystem("Heavy Rain", 1400, 16f, 0.42f, 1.7f);
            rainParticles.gameObject.SetActive(false);
            heavyRainParticles.gameObject.SetActive(false);
        }

        private ParticleSystem CreateRainSystem(string name, int maxParticles, float rate, float size, float speed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 9f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.startLifetime = 1.3f;
            main.startSpeed = speed * 10f;
            main.startSize = size;
            main.gravityModifier = 0.10f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(42f, 0.5f, 42f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 4f;
            renderer.velocityScale = 0.3f;

            return ps;
        }

        private void UpdateParticles(float amount)
        {
            if (rainParticles == null || heavyRainParticles == null) return;

            bool heavy = amount > 0.65f;
            bool normal = amount > 0.05f;

            if (normal)
            {
                if (!rainParticles.gameObject.activeSelf) rainParticles.gameObject.SetActive(true);
                if (!rainParticles.isPlaying) rainParticles.Play();
            }
            else
            {
                if (rainParticles.isPlaying) rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (rainParticles.gameObject.activeSelf) rainParticles.gameObject.SetActive(false);
            }

            if (heavy)
            {
                if (!heavyRainParticles.gameObject.activeSelf) heavyRainParticles.gameObject.SetActive(true);
                if (!heavyRainParticles.isPlaying) heavyRainParticles.Play();
            }
            else
            {
                if (heavyRainParticles.isPlaying) heavyRainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (heavyRainParticles.gameObject.activeSelf) heavyRainParticles.gameObject.SetActive(false);
            }

            var player = truck != null ? truck.transform : null;
            if (player != null)
            {
                transform.position = player.position;
                transform.rotation = Quaternion.identity;
            }

            var normalEmission = rainParticles.emission;
            normalEmission.rateOverTime = Mathf.Lerp(0f, 700f, Mathf.Clamp01(amount));
            var heavyEmission = heavyRainParticles.emission;
            heavyEmission.rateOverTime = Mathf.Lerp(0f, 1500f, Mathf.Clamp01((amount - 0.45f) / 0.55f));
        }

        private static float RainAmount(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.Rain: return 0.55f;
                case WeatherState.HeavyRain: return 0.82f;
                case WeatherState.Storm: return 1f;
                default: return 0f;
            }
        }

        private static float WindAmount(WeatherState state)
        {
            switch (state)
            {
                case WeatherState.Storm: return 1f;
                case WeatherState.HeavyRain: return 0.7f;
                case WeatherState.Rain: return 0.35f;
                case WeatherState.Cloudy: return 0.15f;
                default: return 0f;
            }
        }
    }
}
