using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    [DisallowMultipleComponent]
    public sealed class TruckVisualApplier:MonoBehaviour
    {
        [SerializeField] private string modelId="nomad-aero";
        [SerializeField] private bool applyOnAwake=true;
        private void Awake(){if(applyOnAwake)Apply();}
        [ContextMenu("Apply Truck Visuals")]
        public void Apply(){TruckVisualAssembler.Apply(gameObject,modelId,true);}
        public void ApplyModel(string id){modelId=id;TruckVisualAssembler.Apply(gameObject,id,true);}
    }
}