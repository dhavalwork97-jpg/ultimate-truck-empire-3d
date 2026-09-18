using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public static class TruckPresentationRuntimeBinder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("Truck Presentation Binder");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Binder>();
        }

        private sealed class Binder : MonoBehaviour
        {
            private bool done;
            private void Update()
            {
                if (done) return;
                var truck = FindFirstObjectByType<UltimateTruckEmpire.TruckController>();
                if (truck == null) return;
                done = true;
                if (truck.GetComponent<TruckVisuals>() == null)
                    TruckVisualAssembler.Apply(truck.gameObject, "nomad-aero", true);
                if (truck.GetComponent<TruckCockpit>() == null)
                    truck.gameObject.AddComponent<TruckCockpit>();
                var cam = Camera.main;
                if (cam != null)
                {
                    var old = cam.GetComponent<UltimateTruckEmpire.TruckCamera>();
                    if (old != null) old.enabled = false;
                    var rig = cam.GetComponent<TruckCameraRig>();
                    if (rig == null) rig = cam.gameObject.AddComponent<TruckCameraRig>();
                    rig.SetTarget(truck.transform);
                }
            }
        }
    }
}