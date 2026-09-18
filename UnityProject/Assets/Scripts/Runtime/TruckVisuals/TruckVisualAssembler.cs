using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public static class TruckVisualAssembler
    {
        public static TruckVisuals Apply(GameObject root,string modelId){return Apply(root,TruckVisualPresets.Resolve(modelId),true);}
        public static TruckVisuals Apply(GameObject root,string modelId,Color paint){var s=TruckVisualPresets.Resolve(modelId);s.paint=paint;return Apply(root,s,true);}
        public static TruckVisuals Apply(GameObject root,TruckVisualSpec spec,bool hidePlaceholderRenderers){if(root==null)return null;var p=TruckMaterialLibrary.Create(spec);if(hidePlaceholderRenderers)Hide(root);return new TruckBodyBuilder(spec,p).Build(root);}
        public static TruckVisuals ApplyTrailer(GameObject root,TrailerVisualSpec spec){if(root==null)return null;var p=TruckMaterialLibrary.Create(TruckVisualPresets.Resolve("nomad-aero"));return new TrailerVisualBuilder(spec??new TrailerVisualSpec(),p).Build(root);}
        private static void Hide(GameObject root){foreach(var r in root.GetComponentsInChildren<Renderer>(true)){if(r.GetComponent<TruckVisuals>()!=null)continue;r.enabled=false;}}
    }
}