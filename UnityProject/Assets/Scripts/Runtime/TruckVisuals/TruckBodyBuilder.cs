using System.Collections.Generic;
using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public class TruckBodyBuilder
    {
        private TruckVisualSpec s; private TruckMaterialLibrary.Palette p;
        public TruckBodyBuilder(TruckVisualSpec spec,TruckMaterialLibrary.Palette palette){s=spec;p=palette;}
        public TruckVisuals Build(GameObject root)
        {
            var v=root.GetComponent<TruckVisuals>()??root.AddComponent<TruckVisuals>();v.ClearGenerated();v.specId=s.id;
            var r=new GameObject("ProceduralTruckBody");r.transform.SetParent(root.transform,false);v.bodyRoot=r;
            var cab=GameObject.CreatePrimitive(PrimitiveType.Cube);cab.name="Cab";cab.transform.SetParent(r.transform,false);cab.transform.localPosition=new Vector3(0,s.frameTopY+s.cabHeight*.5f,s.frameFrontZ-s.cabLength*.5f);cab.transform.localScale=new Vector3(s.cabWidth,s.cabHeight,s.cabLength);cab.GetComponent<Renderer>().sharedMaterial=p.Get(TruckMaterialLibrary.Paint);TruckVisualUtility.StripColliders(cab);v.Track(cab);
            var roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.name="Roof";roof.transform.SetParent(r.transform,false);roof.transform.localPosition=new Vector3(0,s.frameTopY+s.cabHeight+.06f,s.frameFrontZ-s.cabLength*.55f);roof.transform.localScale=new Vector3(s.cabWidth*.92f,.12f,s.cabLength*.86f);roof.GetComponent<Renderer>().sharedMaterial=p.Get(TruckMaterialLibrary.Paint);TruckVisualUtility.StripColliders(roof);v.Track(roof);
            var glass=GameObject.CreatePrimitive(PrimitiveType.Cube);glass.name="Windshield";glass.transform.SetParent(r.transform,false);glass.transform.localPosition=new Vector3(0,s.frameTopY+s.cabHeight*.60f,s.frameFrontZ+.025f);glass.transform.localScale=new Vector3(s.cabWidth*.82f,s.cabHeight*.42f,.035f);glass.GetComponent<Renderer>().sharedMaterial=p.Get(TruckMaterialLibrary.Glass);TruckVisualUtility.StripColliders(glass);v.Track(glass);
            var bumper=GameObject.CreatePrimitive(PrimitiveType.Cube);bumper.name="Bumper";bumper.transform.SetParent(r.transform,false);bumper.transform.localPosition=new Vector3(0,s.frameTopY*.55f,s.frameFrontZ+s.bumperDepth*.55f);bumper.transform.localScale=new Vector3(s.cabWidth*1.04f,s.bumperHeight,s.bumperDepth);bumper.GetComponent<Renderer>().sharedMaterial=p.Get(s.chromeBumper?TruckMaterialLibrary.Chrome:TruckMaterialLibrary.PaintAccent);TruckVisualUtility.StripColliders(bumper);v.Track(bumper);
            var grille=GameObject.CreatePrimitive(PrimitiveType.Cube);grille.name="Grille";grille.transform.SetParent(r.transform,false);grille.transform.localPosition=new Vector3(0,s.frameTopY+s.bumperHeight*.55f,s.frameFrontZ+s.bumperDepth*.5f+.01f);grille.transform.localScale=new Vector3(s.cabWidth*.62f,.62f,.035f);grille.GetComponent<Renderer>().sharedMaterial=p.Get(TruckMaterialLibrary.Grille);TruckVisualUtility.StripColliders(grille);v.Track(grille);
            var wheels=new List<Transform>();var positions=new List<WheelPlacement>();positions.Add(new WheelPlacement(new Vector3(-s.trackWidth*.5f, s.wheelRadius, s.frontAxleZ),s.wheelRadius,s.wheelWidth,false,true));positions.Add(new WheelPlacement(new Vector3(s.trackWidth*.5f,s.wheelRadius,s.frontAxleZ),s.wheelRadius,s.wheelWidth,false,false));for(int a=0;a<s.rearAxleCount;a++){float z=s.rearAxleZ-a*s.rearAxleSpacing;positions.Add(new WheelPlacement(new Vector3(-s.trackWidth*.5f,s.wheelRadius,z),s.wheelRadius,s.wheelWidth,s.dualRearWheels,true));positions.Add(new WheelPlacement(new Vector3(s.trackWidth*.5f,s.wheelRadius,z),s.wheelRadius,s.wheelWidth,s.dualRearWheels,false));}
            var wg=new GameObject("WheelVisuals");wg.transform.SetParent(r.transform,false);for(int i=0;i<positions.Count;i++){var w=TruckVisualUtility.CreateWheelObject(positions[i],wg.transform,p,"Wheel_"+i);wheels.Add(w.transform);v.Track(w);}
            v.wheelVisuals=wheels.ToArray();var lights=root.GetComponent<TruckLightRig>()??root.AddComponent<TruckLightRig>();lights.Build(root.transform,p);v.lightRig=lights;return v;
        }
    }
}