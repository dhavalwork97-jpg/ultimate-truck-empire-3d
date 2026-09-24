using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    /// <summary>
    /// Applies the production calibration attached to a trailer prefab before the
    /// trailer enters gameplay. This is deliberately a calibration layer, not a
    /// replacement for TrailerPhysicsAttachment or TrailerController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrailerRuntimeCalibrator : MonoBehaviour
    {
        [SerializeField] private TrailerProductionCalibration calibration;
        [SerializeField] private bool applyOnAwake = true;

        public TrailerProductionCalibration Calibration => calibration;
        public float EmptyMassTons => calibration != null ? calibration.massTons : 0f;
        public bool HasValidCalibration => calibration != null && calibration.IsValid();

        private void Awake()
        {
            if (applyOnAwake)
                ApplyCalibration();
        }

        public bool ApplyCalibration()
        {
            calibration = calibration != null
                ? calibration
                : GetComponent<TrailerProductionCalibration>();

            if (calibration == null || !calibration.IsValid())
            {
                Debug.LogError("[TrailerCalibration] Missing or invalid production calibration on " + name, this);
                return false;
            }

            ApplyScale();
            ApplySockets();
            ApplyPhysics();
            return true;
        }

        private void ApplyScale()
        {
            // Preserve the authored prefab root transform while allowing calibration
            // to correct Meshy unit/axis differences before physics initialization.
            transform.localScale = Vector3.Scale(
                Vector3.one * calibration.modelScale,
                calibration.localScaleMultiplier);
        }

        private void ApplySockets()
        {
            SetLocalPosition(FindSocket("Kingpin"), calibration.kingpinLocalPosition);
            SetLocalPosition(FindSocket("Wheel_FL"), calibration.wheelFL);
            SetLocalPosition(FindSocket("Wheel_FR"), calibration.wheelFR);
            SetLocalPosition(FindSocket("Wheel_RL"), calibration.wheelRL);
            SetLocalPosition(FindSocket("Wheel_RR"), calibration.wheelRR);
        }

        private void ApplyPhysics()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.mass = Mathf.Max(0.01f, calibration.massTons * 1000f);
                body.centerOfMass = calibration.centerOfMassLocalPosition;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                BoxCollider box = colliders[i] as BoxCollider;
                if (box == null || box.isTrigger)
                    continue;

                box.center = calibration.colliderCenterLocalPosition;
                box.size = calibration.colliderSize;
                break;
            }
        }

        private Transform FindSocket(string socketName)
        {
            Transform direct = transform.Find("Sockets/" + socketName) ?? transform.Find(socketName);
            if (direct != null)
                return direct;

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && string.Equals(child.name, socketName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }
            return null;
        }

        private static void SetLocalPosition(Transform socket, Vector3 position)
        {
            if (socket != null)
                socket.localPosition = position;
        }
    }
}
