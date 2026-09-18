using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public class TruckWheelVisualSync:MonoBehaviour
    {
        public WheelCollider[] wheelColliders;
        public Transform[] wheelVisuals;
        private void LateUpdate(){if(wheelColliders==null||wheelVisuals==null)return;int n=Mathf.Min(wheelColliders.Length,wheelVisuals.Length);for(int i=0;i<n;i++){if(wheelColliders[i]==null||wheelVisuals[i]==null)continue;wheelColliders[i].GetWorldPose(out var p,out var r);wheelVisuals[i].position=p;wheelVisuals[i].rotation=r;}}
        public void Bind(WheelCollider[] c,Transform[] v){wheelColliders=c;wheelVisuals=v;}
    }
}