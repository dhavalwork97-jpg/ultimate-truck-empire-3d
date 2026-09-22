using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public sealed class TrailerCargoModule : MonoBehaviour
    {
        [SerializeField] private Transform cargoSocket;
        [SerializeField] private GameObject loadedInstance;

        public GameObject LoadedInstance => loadedInstance;

        public void Initialize(TrailerDefinition definition)
        {
            cargoSocket = definition != null && definition.cargoSocket != null
                ? definition.cargoSocket
                : transform;
        }

        public bool Load(CargoDefinition cargo)
        {
            Unload();
            if (cargo == null || cargo.cargoPrefab == null) return false;

            Transform socket = cargoSocket != null ? cargoSocket : transform;
            loadedInstance = Object.Instantiate(cargo.cargoPrefab, socket);
            loadedInstance.transform.localPosition = cargo.localPosition;
            loadedInstance.transform.localEulerAngles = cargo.localRotation;
            loadedInstance.transform.localScale = cargo.localScale;
            return true;
        }

        private Transform FindSocket(string socketName)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i] != transform && string.Equals(transforms[i].name, socketName, System.StringComparison.OrdinalIgnoreCase))
                    return transforms[i];
            return null;
        }

        public void Unload()
        {
            if (loadedInstance != null)
                Object.Destroy(loadedInstance);
            loadedInstance = null;
        }
    }
}
