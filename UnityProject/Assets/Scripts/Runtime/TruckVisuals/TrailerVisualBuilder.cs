using System.Collections.Generic;
using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public class TrailerVisualBuilder
    {
        private TrailerVisualSpec s; private TruckMaterialLibrary.Palette p;
        public TrailerVisualBuilder(TrailerVisualSpec spec,TruckMaterialLibrary.Palette palette){this.s=spec;this.p=palette;}
        public TruckVisuals Build(GameObject root)
        {
            var v=root.GetComponent<TruckVisuals>()??root.AddComponent<TruckVisuals>();v.ClearGenerated();v.isTrailer=true;v.specId=s.id;var g=new GameObject("TrailerVisuals");g.transform.SetParent(root.transform,false);v.bodyRoot=g;
            var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.name="DryVanBody";body.transform.SetParent(g.transform,false);body.transform.localPosition=new Vector3(0,s.floorY+s.boxHeight*.5f,0);body.transform.localScale=new Vector3(s.boxWidth,s.boxHeight,s.boxLength);body.GetComponent<Renderer>().sharedMaterial=p.Get(TruckMaterialLibrary.TrailerSkin);TruckVisualUtility.StripColliders(body);v.Track(body);
            var rear=GameObject.CreatePrimitive(PrimitiveType.Cube);rear.name="RearDoors";rear.transform.SetParent(g.transform,false);rear.transform.localPosition=new Vector3(0,s.floorY+s.boxHeight*.5f,-s.boxLength*.5f-.02f);rear.transform.localScale=new Vector3(s.boxWidth*.92f,s.boxHeight*.9f,.05f);rear.GetComponent<Renderer>().sharedMaterial=p.Get(TruckMaterialLibrary.Aluminium);TruckVisualUtility.StripColliders(rear);v.Track(rear);
            var wheels=new List<Transform>();for(int a=0;a<s.axleCount;a++){float z=s.rearAxleZ+a*s.axleSpacing;foreach(float x in new[]{-s.trackWidth*.5f,s.trackWidth*.5f}){var w=TruckVisualUtility.CreateWheelObject(new WheelPlacement(new Vector3(x,s.wheelRadius,z),s.wheelRadius,s.wheelWidth,s.dualWheels,x<0),g.transform,p,"TrailerWheel");wheels.Add(w.transform);v.Track(w);}}v.wheelVisuals=wheels.ToArray();var anchor=new GameObject("KingpinAnchor");anchor.transform.SetParent(g.transform,false);anchor.transform.localPosition=new Vector3(0,0,s.kingpinZ);v.fifthWheelAnchor=anchor.transform;return v;
        }
    }
}