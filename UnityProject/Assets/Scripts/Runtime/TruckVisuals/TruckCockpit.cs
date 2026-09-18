using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public sealed class TruckCockpit:MonoBehaviour
    {
        [SerializeField] private bool buildOnStart=true;
        private void Start(){if(buildOnStart)Build();}
        [ContextMenu("Build Cockpit")]
        public void Build(){if(transform.Find("ProceduralCockpit")!=null)return;var root=new GameObject("ProceduralCockpit");root.transform.SetParent(transform,false);Make(root.transform,"Dashboard",new Vector3(0,1.25f,1.35f),new Vector3(2.1f,.55f,.35f));Make(root.transform,"SteeringWheel",new Vector3(-.62f,1.35f,1.0f),new Vector3(.55f,.10f,.55f),true);Make(root.transform,"Seat",new Vector3(0,.75f,.2f),new Vector3(1.0f,1.0f,1.0f));}
        private static void Make(Transform p,string n,Vector3 pos,Vector3 scale,bool cylinder=false){var g=GameObject.CreatePrimitive(cylinder?PrimitiveType.Cylinder:PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;var r=g.GetComponent<Renderer>();var m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",new Color(.035f,.04f,.05f));if(m.HasProperty("_Color"))m.SetColor("_Color",new Color(.035f,.04f,.05f));r.sharedMaterial=m;foreach(var c in g.GetComponents<Collider>())Destroy(c);if(cylinder)g.transform.localRotation=Quaternion.Euler(90,0,0);}
    }
}