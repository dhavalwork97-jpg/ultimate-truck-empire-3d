using UnityEngine;

namespace UltimateTruckEmpire
{
    public sealed class TrafficVehicle : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float routeLength = 150f;
        private Vector3 start;

        private void Awake()
        {
            start = transform.position;
        }

        private void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
            if (Vector3.Distance(start, transform.position) > routeLength)
                transform.position = start;
        }
    }
}
