using UnityEngine;

namespace UltimateTruckEmpire
{
    public sealed class TruckCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 4.2f, -9f);
        [SerializeField] private float positionSmooth = 7f;
        [SerializeField] private float rotationSmooth = 8f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-positionSmooth * Time.deltaTime));

            Vector3 lookPoint = target.position + Vector3.up * 1.2f + target.forward * 5f;
            Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
        }

        public void SetTarget(Transform value) => target = value;
    }
}
