using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    [DisallowMultipleComponent]
    public class TruckWheelVisualSync : MonoBehaviour
    {
        public WheelCollider[] wheelColliders;
        public Transform[] wheelVisuals;
        public bool deferToExistingRig = true;
        public string[] existingRigTypeNames = new string[] { "TruckWheelRig" };
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
                Debug.LogWarning("[TruckWheelVisualSync] Collider/visual counts differ on '" + name + "'. Only the overlapping range will be synchronised.");
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

        public void Configure(WheelCollider[] colliders, Transform[] visuals)
        {
            wheelColliders = colliders;
            wheelVisuals = visuals;
            _warned = false;
            Validate();
        }
    }
}