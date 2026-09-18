using System.Collections;
using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public sealed class TruckVisualRuntimeBinder:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install(){var go=new GameObject("Truck Visual Runtime Binder");Object.DontDestroyOnLoad(go);go.AddComponent<TruckVisualRuntimeBinder>();}
        private IEnumerator Start(){yield return null;yield return new WaitForSeconds(.1f);ApplyAll();}
        private void ApplyAll(){var trucks=FindObjectsByType<UltimateTruckEmpire.TruckController>(FindObjectsSortMode.None);foreach(var t in trucks){if(t==null)continue;if(t.GetComponent<TruckVisuals>()!=null)continue;TruckVisualAssembler.Apply(t.gameObject,"nomad-aero",true);}}
    }
}