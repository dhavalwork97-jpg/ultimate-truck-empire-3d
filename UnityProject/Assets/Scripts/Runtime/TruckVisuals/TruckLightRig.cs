using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public class TruckLightRig:MonoBehaviour
    {
        public Light[] headlights; public Light[] cabinLights; public Renderer[] brakeLenses,indicatorLenses;
        public void Build(Transform root,TruckMaterialLibrary.Palette p){var h1=MakeLight(root,"Headlight_L",new Vector3(-.72f,1.05f,2.9f));var h2=MakeLight(root,"Headlight_R",new Vector3(.72f,1.05f,2.9f));headlights=new[]{h1,h2};var c1=MakeLight(root,"CabLight_L",new Vector3(-.55f,1.7f,-.2f));var c2=MakeLight(root,"CabLight_R",new Vector3(.55f,1.7f,-.2f));cabinLights=new[]{c1,c2};}
        private static Light MakeLight(Transform root,string name,Vector3 pos){var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=pos;var l=go.AddComponent<Light>();l.type=LightType.Spot;l.range=28f;l.spotAngle=42f;l.intensity=2.2f;l.enabled=false;return l;}
        public void SetHeadlights(bool on){if(headlights==null)return;foreach(var l in headlights)if(l)l.enabled=on;}
        public void SetCabin(bool on){if(cabinLights==null)return;foreach(var l in cabinLights)if(l)l.enabled=on;}
    }
}