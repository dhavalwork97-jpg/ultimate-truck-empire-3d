using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public sealed class TruckCameraRig:MonoBehaviour
    {
        public Transform target; public Camera cam; public int mode; public float distance=8f,height=3.5f;
        private void Awake(){cam=GetComponent<Camera>()??Camera.main;if(target==null){var t=FindFirstObjectByType<UltimateTruckEmpire.TruckController>();if(t)target=t.transform;}}
        private void LateUpdate(){if(target==null)return;if(Input.GetKeyDown(KeyCode.C))mode=(mode+1)%4;Vector3 p;Vector3 look=target.position+Vector3.up*1.4f;if(mode==0){p=target.TransformPoint(new Vector3(0,height,-distance));}else if(mode==1){p=target.TransformPoint(new Vector3(0,2.0f,-3.2f));}else if(mode==2){p=target.TransformPoint(new Vector3(0,1.9f,1.2f));}else{p=target.TransformPoint(new Vector3(0,1.65f,.2f));look=target.position+target.forward*12f+Vector3.up*1.5f;}transform.position=Vector3.Lerp(transform.position,p,1f-Mathf.Exp(-8f*Time.deltaTime));transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look-transform.position,Vector3.up),1f-Mathf.Exp(-10f*Time.deltaTime));}
        public void SetTarget(Transform t){target=t;}
    }
}