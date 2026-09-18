using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// Drives the generated wheel meshes from their WheelColliders so steering,
    /// suspension travel and wheel spin all read correctly.
    ///
    /// IMPORTANT: if the project already has a wheel rig component doing this
    /// (for example TruckWheelRig), this component disables itself on Awake
    /// rather than fighting it. That check is done by type *name* on purpose -
    /// this file must not take a compile-time dependency on gameplay code.
    /// Assign <see cref="wheelVisuals"/> to the existing rig instead and leave
    /// <see cref="deferToExistingRig"/> switched on.
    ///
    /// One LateUpdate for the whole vehicle: no per-wheel Update loops.
    /// </summary>
    [DisallowMultipleComponent]
    public class TruckWheelVisualSync : MonoBehaviour
    {
        [Header("Wiring (index-matched)")]
        public WheelCollider[] wheelColliders;
        public Transform[] wheelVisuals;

        [Header("Behaviour")]
        [Tooltip("Disable this component automatically when another wheel rig is already present.")]
        public bool deferToExistingRig = true;

        [Tooltip("Component type names that already drive wheel visuals.")]
        public string[] existingRigTypeNames = new string[] { "TruckWheelRig" };

        [Tooltip("Keeps the mesh upright if a collider is missing at runtime.")]
        public bool skipMissingSilently = true;

        private bool _warned;

        private void Awake()
        {
            if (deferToExistingRig && HasExternalRig())
            {
                enabled = false;
                return;
            }
            Validate();
        }

        private bool HasExternalRig()
        {
            if (existingRigTypeNames == null) return false;
            for (int i = 0; i < existingRigTypeNames.Length; i++)
            {
                string typeName = existingRigTypeNames[i];
                if (string.IsNullOrEmpty(typeName)) continue;
                if (GetComponent(typeName) != null) return true;
            }
            return false;
        }

        private void Validate()
        {
            if (wheelColliders == null || wheelVisuals == null) return;
            if (wheelColliders.Length != wheelVisuals.Length && !_warned)
            {
                _warned = true;
                Debug.LogWarning("[TruckWheelVisualSync] Collider/visual counts differ on '" + name +
                                 "'. Only the overlapping range will be synchronised.");
            }
        }

        private void LateUpdate()
        {
            if (wheelColliders == null || wheelVisuals == null) return;

            int count = Mathf.Min(wheelColliders.Length, wheelVisuals.Length);
            for (int i = 0; i < count; i++)
            {
                WheelCollider wc = wheelColliders[i];
                Transform visual = wheelVisuals[i];
                if (wc == null || visual == null)
                {
                    if (!skipMissingSilently && !_warned)
                    {
                        _warned = true;
                        Debug.LogWarning("[TruckWheelVisualSync] Missing collider or visual at index " + i);
                    }
                    continue;
                }

                Vector3 pos;
                Quaternion rot;
                wc.GetWorldPose(out pos, out rot);
                visual.position = pos;
                visual.rotation = rot;
            }
        }

        /// <summary>Called by the assembler once the visuals exist.</summary>
        public void Configure(WheelCollider[] colliders, Transform[] visuals)
        {
            wheelColliders = colliders;
            wheelVisuals = visuals;
            _warned = false;
            Validate();
        }
    }
}
