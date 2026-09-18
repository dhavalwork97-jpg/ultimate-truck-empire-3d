using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// Optional convenience wrapper. Drop this on a truck or trailer prefab and
    /// the visual pass runs itself on Awake - no change to WorldBootstrap,
    /// TruckDealer or any other existing script is required.
    ///
    /// If you would rather drive it from code, delete this component and call
    /// TruckVisualAssembler.Apply(gameObject, modelId) at spawn time instead.
    /// </summary>
    [DisallowMultipleComponent]
    public class TruckVisualApplier : MonoBehaviour
    {
        [Header("Model")]
        [Tooltip("Catalogue id or display name. Unknown values fall back to a stable preset.")]
        public string modelId = "nomad-aero";

        public bool isTrailer;

        [Header("Paint")]
        public bool overridePaint;
        public Color paint = new Color(0.16f, 0.35f, 0.62f);
        public Color accent = new Color(0.08f, 0.09f, 0.11f);

        [Header("Options")]
        [Tooltip("Disables any pre-existing placeholder renderers on this object.")]
        public bool hidePlaceholderRenderers = true;

        public bool buildOnAwake = true;

        private TruckVisuals _visuals;

        public TruckVisuals Visuals { get { return _visuals; } }

        private void Awake()
        {
            if (buildOnAwake) Rebuild();
        }

        /// <summary>Safe to call repeatedly - the pass replaces its own output.</summary>
        public TruckVisuals Rebuild()
        {
            if (isTrailer)
            {
                TrailerVisualSpec trailerSpec = new TrailerVisualSpec();
                if (overridePaint) trailerSpec.skin = paint;
                _visuals = TruckVisualAssembler.ApplyTrailer(gameObject, trailerSpec, hidePlaceholderRenderers);
                return _visuals;
            }

            TruckVisualSpec spec = TruckVisualPresets.Resolve(modelId);
            if (overridePaint)
            {
                spec.paint = paint;
                spec.accent = accent;
            }

            _visuals = TruckVisualAssembler.Apply(gameObject, spec, hidePlaceholderRenderers);
            return _visuals;
        }
    }
}
