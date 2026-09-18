using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    public enum HeadlightMode { Off, Low, High }
    public enum IndicatorState { None, Left, Right, Hazard }

    [DisallowMultipleComponent]
    public class TruckLightRig : MonoBehaviour
    {
        public List<Renderer> headlightLenses = new List<Renderer>();
        public List<Renderer> tailLamps = new List<Renderer>();
        public List<Renderer> brakeLamps = new List<Renderer>();
        public List<Renderer> reverseLamps = new List<Renderer>();
        public List<Renderer> indicatorLeft = new List<Renderer>();
        public List<Renderer> indicatorRight = new List<Renderer>();
        public List<Renderer> markerLamps = new List<Renderer>();

        public bool allowRealtimeHeadlights = true;
        public List<Transform> headlightAnchors = new List<Transform>();
        public Transform cabInteriorAnchor;
        public float lowBeamRange = 45f;
        public float lowBeamAngle = 62f;
        public float highBeamRange = 90f;
        public float highBeamAngle = 42f;
        [Range(0f, 4f)] public float beamIntensity = 2.2f;
        public float indicatorPeriod = 0.72f;

        private HeadlightMode _headlights = HeadlightMode.Off;
        private IndicatorState _indicators = IndicatorState.None;
        private bool _braking, _reversing, _indicatorPhase, _interiorOn;
        private readonly List<Light> _beams = new List<Light>();
        private Light _cabLight;
        private Coroutine _blink;
        private Material _clearOff, _clearOn, _redOff, _redTail, _redBrake, _amberOff, _amberOn;
        private bool _materialsReady;

        public HeadlightMode Headlights { get { return _headlights; } }
        public IndicatorState Indicators { get { return _indicators; } }
        public bool Braking { get { return _braking; } }
        public bool InteriorLightOn { get { return _interiorOn; } }

        private void Awake() { EnsureMaterials(); Refresh(); }

        private void OnDisable()
        {
            if (_blink != null) { StopCoroutine(_blink); _blink = null; }
        }

        private void EnsureMaterials()
        {
            if (_materialsReady) return;
            Color warmWhite = new Color(0.93f, 0.94f, 0.85f);
            Color red = new Color(0.55f, 0.04f, 0.04f);
            Color amber = new Color(0.70f, 0.33f, 0.03f);
            _clearOff = TruckMaterialLibrary.MakeLens("lensClear", warmWhite, 0f);
            _clearOn = TruckMaterialLibrary.MakeLens("lensClearOn", warmWhite, 3.2f);
            _redOff = TruckMaterialLibrary.MakeLens("lensRed", red, 0f);
            _redTail = TruckMaterialLibrary.MakeLens("lensRedTail", red, 1.1f);
            _redBrake = TruckMaterialLibrary.MakeLens("lensRedBrake", red, 4.2f);
            _amberOff = TruckMaterialLibrary.MakeLens("lensAmber", amber, 0f);
            _amberOn = TruckMaterialLibrary.MakeLens("lensAmberOn", amber, 3.6f);
            _materialsReady = true;
        }

        public void SetHeadlights(HeadlightMode mode)
        {
            if (_headlights == mode) return;
            _headlights = mode; UpdateBeams(); Refresh();
        }

        public void ToggleHeadlights()
        {
            SetHeadlights(_headlights == HeadlightMode.Off ? HeadlightMode.Low
                        : _headlights == HeadlightMode.Low ? HeadlightMode.High
                        : HeadlightMode.Off);
        }

        public void SetBrakes(bool on) { if (_braking == on) return; _braking = on; Refresh(); }
        public void SetReverse(bool on) { if (_reversing == on) return; _reversing = on; Refresh(); }

        public void SetInteriorLight(bool on)
        {
            _interiorOn = on;
            if (on && _cabLight == null && cabInteriorAnchor != null)
            {
                GameObject go = new GameObject("CabLight");
                go.transform.SetParent(cabInteriorAnchor, false);
                _cabLight = go.AddComponent<Light>();
                _cabLight.type = LightType.Point;
                _cabLight.range = 1.9f;
                _cabLight.intensity = 0.7f;
                _cabLight.color = new Color(1f, 0.92f, 0.78f);
                _cabLight.shadows = LightShadows.None;
            }
            if (_cabLight != null) _cabLight.enabled = on;
        }

        public void SetIndicator(IndicatorState state)
        {
            if (_indicators == state) return;
            _indicators = state;
            if (_blink != null) { StopCoroutine(_blink); _blink = null; }
            _indicatorPhase = false;
            if (state != IndicatorState.None && isActiveAndEnabled) _blink = StartCoroutine(BlinkRoutine());
            Refresh();
        }

        public void SetRunningLights(bool on) { SetHeadlights(on ? HeadlightMode.Low : HeadlightMode.Off); }

        private IEnumerator BlinkRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.12f, indicatorPeriod * 0.5f));
            while (_indicators != IndicatorState.None)
            {
                _indicatorPhase = !_indicatorPhase;
                ApplyIndicators();
                yield return wait;
            }
            _indicatorPhase = false; ApplyIndicators(); _blink = null;
        }

        private void UpdateBeams()
        {
            bool wantBeams = allowRealtimeHeadlights && _headlights != HeadlightMode.Off;
            if (!wantBeams)
            {
                for (int i = 0; i < _beams.Count; i++) if (_beams[i] != null) _beams[i].enabled = false;
                return;
            }
            if (_beams.Count == 0) CreateBeams();
            bool high = _headlights == HeadlightMode.High;
            for (int i = 0; i < _beams.Count; i++)
            {
                Light beam = _beams[i]; if (beam == null) continue;
                beam.enabled = true;
                beam.range = high ? highBeamRange : lowBeamRange;
                beam.spotAngle = high ? highBeamAngle : lowBeamAngle;
                beam.intensity = high ? beamIntensity * 1.35f : beamIntensity;
            }
        }

        private void CreateBeams()
        {
            int made = 0;
            for (int i = 0; i < headlightAnchors.Count && made < 2; i++)
            {
                Transform anchor = headlightAnchors[i]; if (anchor == null) continue;
                GameObject go = new GameObject("HeadlightBeam");
                go.transform.SetParent(anchor, false);
                go.transform.localPosition = new Vector3(0f, 0f, 0.06f);
                go.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
                Light beam = go.AddComponent<Light>();
                beam.type = LightType.Spot;
                beam.color = new Color(1f, 0.97f, 0.88f);
                beam.shadows = LightShadows.None;
                beam.range = lowBeamRange;
                beam.spotAngle = lowBeamAngle;
                beam.intensity = beamIntensity;
                _beams.Add(beam); made++;
            }
        }

        private void Refresh()
        {
            EnsureMaterials();
            bool running = _headlights != HeadlightMode.Off;
            Apply(headlightLenses, running ? _clearOn : _clearOff);
            Apply(markerLamps, running ? _amberOn : _amberOff);
            Apply(reverseLamps, _reversing ? _clearOn : _clearOff);
            Apply(tailLamps, running ? _redTail : _redOff);
            if (_braking) Apply(brakeLamps, _redBrake);
            else if (!running) Apply(brakeLamps, _redOff);
            else Apply(brakeLamps, _redTail);
            ApplyIndicators();
        }

        private void ApplyIndicators()
        {
            EnsureMaterials();
            bool left = _indicatorPhase && (_indicators == IndicatorState.Left || _indicators == IndicatorState.Hazard);
            bool right = _indicatorPhase && (_indicators == IndicatorState.Right || _indicators == IndicatorState.Hazard);
            Apply(indicatorLeft, left ? _amberOn : _amberOff);
            Apply(indicatorRight, right ? _amberOn : _amberOff);
        }

        private static void Apply(List<Renderer> lamps, Material material)
        {
            if (lamps == null || material == null) return;
            for (int i = 0; i < lamps.Count; i++) { Renderer r = lamps[i]; if (r != null) r.sharedMaterial = material; }
        }

        public void Absorb(TruckLightSet set)
        {
            if (set == null) return;
            tailLamps.AddRange(set.tailLights);
            brakeLamps.AddRange(set.brakeLights);
            reverseLamps.AddRange(set.reverseLights);
            indicatorLeft.AddRange(set.indicatorsLeft);
            indicatorRight.AddRange(set.indicatorsRight);
            markerLamps.AddRange(set.markerLights);
            Refresh();
        }
    }
}