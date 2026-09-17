using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TruckPhysics : MonoBehaviour
    {
        [SerializeField] private float truckMass = 8000f;
        [SerializeField] private float downforce = 250f;
        [SerializeField] private float airDrag = 0.35f;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = truckMass;
            body.linearDamping = airDrag;
            body.angularDamping = 0.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void FixedUpdate()
        {
            float speed = body.linearVelocity.magnitude;
            body.AddForce(Vector3.down * speed * downforce, ForceMode.Force);
        }
    }
}
