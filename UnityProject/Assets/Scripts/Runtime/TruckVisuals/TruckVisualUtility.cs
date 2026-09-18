using System.Collections.Generic;
using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public struct WheelPlacement { public Vector3 localPosition; public float radius,width; public bool dual,isLeft; public WheelPlacement(Vector3 p,float r,float w,bool d,bool l){localPosition=p;radius=r;width=w;dual=d;isLeft=l;} }
    public static class TruckVisualUtility
    {
        public static GameObject CreateWheelObject(WheelPlacement w,Transform parent,TruckMaterialLibrary.Palette p,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=w.localPosition;var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=ProcMesh.Cylinder(w.radius,w.width,18,true);var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=p.Get(TruckMaterialLibrary.Rubber);return go;}
        public static void StripColliders(GameObject root){if(root==null)return;foreach(var c in root.GetComponentsInChildren<Collider>(true))if(!(c is WheelCollider))SafeDestroy(c);}
        public static void SetShadowCasting(GameObject root,bool cast){if(root==null)return;var mode=cast?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=mode;}
        public static void SafeDestroy(Object o){if(o==null)return;if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
    }
}