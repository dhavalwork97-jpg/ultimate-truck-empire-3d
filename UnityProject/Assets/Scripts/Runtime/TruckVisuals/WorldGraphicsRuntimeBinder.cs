using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public sealed class WorldGraphicsRuntimeBinder : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install(){var go=new GameObject("World Graphics Binder");Object.DontDestroyOnLoad(go);go.AddComponent<WorldGraphicsRuntimeBinder>();}
        private void Start(){Build();}
        private void Build()
        {
            var root=new GameObject("World Graphics Polish");
            for(int z=-60;z<=60;z+=15){MakeBuilding(root.transform,new Vector3(-24,0,z),new Vector3(9,6+(z%30==0?5:0),8));MakeBuilding(root.transform,new Vector3(24,0,z+6),new Vector3(10,8+(z%45==0?6:0),9));}
            for(int z=-60;z<=60;z+=20){MakeTree(root.transform,new Vector3(-13,0,z));MakeTree(root.transform,new Vector3(13,0,z+8));MakeLamp(root.transform,new Vector3(-10,0,z));MakeLamp(root.transform,new Vector3(10,0,z+10));}
            MakeDepot(root.transform,new Vector3(34,0,34));
        }
        private static Material Mat(Color c){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);return m;}
        private static void MakeBuilding(Transform p,Vector3 pos,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="City Building";g.transform.SetParent(p,false);g.transform.position=pos+Vector3.up*size.y*.5f;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.10f,.13f,.17f));Destroy(g.GetComponent<Collider>());}
        private static void MakeTree(Transform p,Vector3 pos){var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.name="Roadside Tree";trunk.transform.SetParent(p,false);trunk.transform.position=pos+Vector3.up*1.5f;trunk.transform.localScale=new Vector3(.35f,1.5f,.35f);trunk.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.18f,.10f,.05f));Destroy(trunk.GetComponent<Collider>());var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.transform.SetParent(p,false);crown.transform.position=pos+Vector3.up*3.4f;crown.transform.localScale=Vector3.one*2.5f;crown.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.08f,.24f,.12f));Destroy(crown.GetComponent<Collider>());}
        private static void MakeLamp(Transform p,Vector3 pos){var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pole.name="Street Light";pole.transform.SetParent(p,false);pole.transform.position=pos+Vector3.up*3f;pole.transform.localScale=new Vector3(.10f,3f,.10f);pole.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.18f,.19f,.21f));Destroy(pole.GetComponent<Collider>());}
        private static void MakeDepot(Transform p,Vector3 pos){var yard=GameObject.CreatePrimitive(PrimitiveType.Cube);yard.name="Logistics Yard";yard.transform.SetParent(p,false);yard.transform.position=pos+Vector3.up*.08f;yard.transform.localScale=new Vector3(42,.16f,30);yard.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.16f,.17f,.18f));Destroy(yard.GetComponent<Collider>());var wh=GameObject.CreatePrimitive(PrimitiveType.Cube);wh.name="Warehouse";wh.transform.SetParent(p,false);wh.transform.position=pos+new Vector3(7,5,5);wh.transform.localScale=new Vector3(18,10,14);wh.GetComponent<Renderer>().sharedMaterial=Mat(new Color(.18f,.22f,.26f));Destroy(wh.GetComponent<Collider>());}
    }
}