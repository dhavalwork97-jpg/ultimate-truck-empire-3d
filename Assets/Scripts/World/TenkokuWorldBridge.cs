using System;
using System.Reflection;
using UnityEngine;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Optional, dependency-free bridge to Tenkoku Dynamic Sky.
    /// Reflection is intentional: the project still compiles when the Asset Store
    /// package has not been imported, while an imported Tenkoku prefab becomes the
    /// authoritative sky, sun and weather renderer automatically.
    /// </summary>
    public static class TenkokuWorldBridge
    {
        private const string ResourcePath = "VersatileStudioWorld/Tenkoku DynamicSky";
        private static GameObject moduleObject;
        private static Component module;
        private static bool attempted;

        public static bool IsActive => module != null;

        public static bool Ensure()
        {
            // Tenkoku's publisher documents it for desktop/console, not mobile.
            // Keep the existing lightweight atmosphere on Android/iOS.
            if (Application.isMobilePlatform) return false;
            if (attempted) return IsActive;
            attempted = true;

            moduleObject = GameObject.Find("Tenkoku DynamicSky");
            if (moduleObject == null)
            {
                GameObject prefab = Resources.Load<GameObject>(ResourcePath);
                if (prefab != null)
                    moduleObject = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            }

            if (moduleObject == null)
                return false;

            module = FindTenkokuModule(moduleObject);
            if (module == null)
            {
                Debug.LogWarning("[TenkokuWorldBridge] Tenkoku prefab found, but no Tenkoku.Core.TenkokuModule component was found.");
                return false;
            }

            moduleObject.name = "Tenkoku DynamicSky";
            ApplyDefaults();
            return true;
        }

        public static void BindCamera(Camera camera)
        {
            if (!Ensure() || camera == null) return;
            Set(module, "cameraTypeIndex", 0);
            Set(module, "manualCamera", camera.transform);
        }

        public static void Apply(float timeOfDay, WeatherState weather)
        {
            if (!Ensure()) return;

            int hour = Mathf.FloorToInt(timeOfDay);
            int minute = Mathf.FloorToInt((timeOfDay - hour) * 60f);
            Set(module, "currentYear", 2026);
            Set(module, "currentMonth", 1);
            Set(module, "currentDay", 1);
            Set(module, "currentHour", Mathf.Clamp(hour, 0, 23));
            Set(module, "currentMinute", Mathf.Clamp(minute, 0, 59));
            Set(module, "currentSecond", 0);
            Set(module, "setLatitude", 23.0225f);
            Set(module, "setLongitude", 72.5714f);

            // Tenkoku weather values are normalized 0..1 in its public API.
            float rain = 0f;
            float fog = 0f;
            float overcast = 0f;
            switch (weather)
            {
                case WeatherState.Cloudy: overcast = 0.55f; break;
                case WeatherState.Rain: overcast = 0.70f; rain = 0.55f; fog = 0.12f; break;
                case WeatherState.HeavyRain: overcast = 0.90f; rain = 0.85f; fog = 0.25f; break;
                case WeatherState.Fog: fog = 0.75f; overcast = 0.45f; break;
                case WeatherState.Storm: overcast = 1f; rain = 1f; fog = 0.28f; break;
            }

            Set(module, "weatherTypeIndex", 0);
            Set(module, "weather_OvercastAmt", overcast);
            Set(module, "weather_RainAmt", rain);
            Set(module, "weather_FogAmt", fog);
            Set(module, "weather_cloudCumulusAmt", Mathf.Lerp(0.15f, 0.8f, overcast));
            Set(module, "weather_cloudAltoStratusAmt", Mathf.Lerp(0.05f, 0.65f, overcast));
            Set(module, "weather_cloudCirrusAmt", Mathf.Lerp(0.05f, 0.45f, overcast));
            Set(module, "weather_WindAmt", weather == WeatherState.Storm ? 1f : rain > 0f ? 0.45f : 0.2f);
            Set(module, "weather_WindDir", 225f);
        }

        private static void ApplyDefaults()
        {
            Set(module, "setLatitude", 23.0225f);
            Set(module, "setLongitude", 72.5714f);
            Set(module, "weatherTypeIndex", 0);
            Set(module, "weather_cloudScale", 1f);
            Set(module, "weather_cloudSpeed", 0.2f);
        }

        private static Component FindTenkokuModule(GameObject root)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component candidate = components[i];
                if (candidate == null) continue;
                Type type = candidate.GetType();
                if (type.FullName == "Tenkoku.Core.TenkokuModule" || type.Name == "TenkokuModule")
                    return candidate;
            }
            return null;
        }

        private static bool Set(Component target, string member, object value)
        {
            if (target == null) return false;
            Type type = target.GetType();

            PropertyInfo property = type.GetProperty(member, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
            {
                try { property.SetValue(target, ConvertValue(value, property.PropertyType), null); return true; }
                catch { }
            }

            FieldInfo field = type.GetField(member, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                try { field.SetValue(target, ConvertValue(value, field.FieldType)); return true; }
                catch { }
            }

            return false;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;
            return Convert.ChangeType(value, targetType);
        }
    }
}
