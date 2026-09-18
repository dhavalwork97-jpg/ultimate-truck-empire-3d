using UnityEngine;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.CameraSystem
{
    public enum DriveCameraMode { Chase, Bumper, Hood, Cockpit }

    /// <summary>
    /// The single driving camera. One Camera component, four viewpoints - there is
    /// deliberately no second camera in the scene, so "only the active view renders"
    /// is guaranteed by construction rather than by enabling and disabling cameras.
    ///
    /// Runs in LateUpdate and reads the truck's interpolated transform, so the
    /// image is smooth even though the truck is driven in FixedUpdate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TruckCameraRig : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public Rigidbody targetBody;
        public Transform cockpitAnchor;
        public Transform hoodAnchor;
        public Transform bumperAnchor;

        [Header("Mode")]
        public DriveCameraMode mode = DriveCameraMode.Chase;
        public KeyCode cycleKey = KeyCode.C;

        [Header("Chase")]
        public float chaseDistance = 11.5f;
        public float chaseHeight = 4.7f;
        public float pivotHeight = 2.1f;
        public float lookAhead = 7f;
        public float positionSmoothTime = 0.16f;
        public float rotationLerpSpeed = 7.5f;

        [Header("Field of view")]
        public float baseFov = 60f;
        public float maxFov = 70f;
        public float interiorFov = 66f;
        public float fovSpeedKph = 110f;

        [Header("Collision")]
        public bool avoidClipping = true;
        public float collisionRadius = 0.38f;
        public float minDistance = 3.2f;
        public float collisionMargin = 0.25f;

        [Header("Orbit (hold right mouse)")]
        public bool allowOrbit = true;
        public float orbitSensitivity = 3.2f;
        public float orbitRecenterDelay = 1.4f;
        public float minPitch = -12f;
        public float maxPitch = 55f;

        private Camera cam;
        private TruckCockpit cockpit;
        private Vector3 velocity;
        private float orbitYaw, orbitPitch, orbitIdleTime;
        private bool orbiting;
        private bool interiorVisible;

        public DriveCameraMode Mode { get { return mode; } }
        public bool IsInterior { get { return mode == DriveCameraMode.Cockpit; } }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = gameObject.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 900f;
            cam.fieldOfView = baseFov;
        }

        private void Start()
        {
            if (target != null && cockpit == null) cockpit = target.GetComponentInChildren<TruckCockpit>();
            ApplyInteriorVisibility(true);
            SnapToTarget();
        }

        /// <summary>Called by the bootstrap once the truck and its anchors exist.</summary>
        public void Bind(Transform truck, Rigidbody body, Transform cockpitEye, Transform hood, Transform bumper)
        {
            target = truck;
            targetBody = body;
            cockpitAnchor = cockpitEye;
            hoodAnchor = hood;
            bumperAnchor = bumper;
            if (truck != null) cockpit = truck.GetComponentInChildren<TruckCockpit>();
            ApplyInteriorVisibility(true);
            SnapToTarget();
        }

        public void SetMode(DriveCameraMode newMode)
        {
            mode = newMode;
            orbitYaw = 0f;
            orbitPitch = 0f;
            ApplyInteriorVisibility(true);
            SnapToTarget();
        }

        public void CycleMode()
        {
            switch (mode)
            {
                case DriveCameraMode.Chase: SetMode(DriveCameraMode.Bumper); break;
                case DriveCameraMode.Bumper: SetMode(DriveCameraMode.Hood); break;
                case DriveCameraMode.Hood: SetMode(DriveCameraMode.Cockpit); break;
                default: SetMode(DriveCameraMode.Chase); break;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(cycleKey)) CycleMode();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateOrbitInput();

            if (mode == DriveCameraMode.Chase) UpdateChase();
            else UpdateFixedView();

            UpdateFov();
        }

        // ------------------------------------------------------------------
        private void UpdateOrbitInput()
        {
            if (!allowOrbit)
            {
                orbiting = false;
                return;
            }

            orbiting = Input.GetMouseButton(1);
            if (orbiting)
            {
                orbitIdleTime = 0f;
                orbitYaw += Input.GetAxisRaw("Mouse X") * orbitSensitivity;
                orbitPitch = Mathf.Clamp(orbitPitch - Input.GetAxisRaw("Mouse Y") * orbitSensitivity, minPitch, maxPitch);
                return;
            }

            // Recentre gently once the player lets go.
            orbitIdleTime += Time.deltaTime;
            if (orbitIdleTime < orbitRecenterDelay) return;
            float t = 1f - Mathf.Exp(-3.5f * Time.deltaTime);
            orbitYaw = Mathf.Lerp(orbitYaw, 0f, t);
            orbitPitch = Mathf.Lerp(orbitPitch, 0f, t);
        }

        private void UpdateChase()
        {
            ApplyInteriorVisibility(false);

            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Vector3 flatForward = FlatForward();

            // Orbit offsets ride on top of the truck's own heading.
            Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 offset = orbitRotation * (-flatForward * chaseDistance + Vector3.up * chaseHeight);
            Vector3 desired = pivot + offset;

            if (avoidClipping) desired = ResolveCollision(pivot, desired);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, positionSmoothTime);

            Vector3 lookTarget = pivot + flatForward * lookAhead;
            Vector3 toLook = lookTarget - transform.position;
            if (toLook.sqrMagnitude > 0.001f)
            {
                Quaternion wanted = Quaternion.LookRotation(toLook.normalized, Vector3.up);
                float t = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, wanted, t);
            }
        }

        private void UpdateFixedView()
        {
            ApplyInteriorVisibility(mode == DriveCameraMode.Cockpit);

            Transform anchor = CurrentAnchor();
            if (anchor == null)
            {
                UpdateChase();
                return;
            }

            transform.position = anchor.position;

            Quaternion wanted = anchor.rotation;
            if (orbiting || Mathf.Abs(orbitYaw) > 0.01f || Mathf.Abs(orbitPitch) > 0.01f)
                wanted = anchor.rotation * Quaternion.Euler(orbitPitch, orbitYaw, 0f);

            // A touch of rotational lag keeps interior views from feeling welded,
            // without ever letting the camera drift out of the cab.
            float t = 1f - Mathf.Exp(-22f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, wanted, t);
        }

        private Transform CurrentAnchor()
        {
            switch (mode)
            {
                case DriveCameraMode.Cockpit: return cockpitAnchor;
                case DriveCameraMode.Hood: return hoodAnchor;
                case DriveCameraMode.Bumper: return bumperAnchor;
                default: return null;
            }
        }

        private Vector3 FlatForward()
        {
            Vector3 forward = target.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            return forward.normalized;
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desired)
        {
            Vector3 direction = desired - pivot;
            float distance = direction.magnitude;
            if (distance < 0.01f) return desired;
            direction /= distance;

            RaycastHit hit;
            if (Physics.SphereCast(pivot, collisionRadius, direction, out hit, distance,
                                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // Ignore the truck's own colliders.
                if (hit.collider != null && hit.collider.transform.root == target.root) return desired;
                float safe = Mathf.Max(minDistance, hit.distance - collisionMargin);
                return pivot + direction * safe;
            }
            return desired;
        }

        private void UpdateFov()
        {
            if (cam == null) return;
            float speedKph = targetBody != null ? targetBody.linearVelocity.magnitude * 3.6f : 0f;
            float wanted = mode == DriveCameraMode.Cockpit
                ? interiorFov
                : Mathf.Lerp(baseFov, maxFov, Mathf.Clamp01(speedKph / Mathf.Max(1f, fovSpeedKph)));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, wanted, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }

        private void ApplyInteriorVisibility(bool force)
        {
            bool wanted = mode == DriveCameraMode.Cockpit;
            if (!force && wanted == interiorVisible) return;
            interiorVisible = wanted;
            if (cockpit != null) cockpit.SetInteriorVisible(wanted);
        }

        private void SnapToTarget()
        {
            if (target == null) return;

            if (mode == DriveCameraMode.Chase)
            {
                Vector3 pivot = target.position + Vector3.up * pivotHeight;
                Vector3 flatForward = FlatForward();
                transform.position = pivot - flatForward * chaseDistance + Vector3.up * chaseHeight;
                transform.rotation = Quaternion.LookRotation((pivot + flatForward * lookAhead) - transform.position, Vector3.up);
            }
            else
            {
                Transform anchor = CurrentAnchor();
                if (anchor != null)
                {
                    transform.position = anchor.position;
                    transform.rotation = anchor.rotation;
                }
            }
            velocity = Vector3.zero;
        }
    }
}
