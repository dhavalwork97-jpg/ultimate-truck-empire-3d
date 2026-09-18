using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public static class TruckMaterialLibrary
    {
        public const int Paint=0, PaintAccent=1, Chassis=2, Rubber=3, Glass=4, Chrome=5, Aluminium=6, PlasticBlack=7, Grille=8, LensClear=9, LensAmber=10, LensRed=11, TrailerSkin=12;
        public class Palette { public Material[] Materials=new Material[13]; public Material Get(int i)=>Materials[Mathf.Clamp(i,0,Materials.Length-1)]; }
        public static Palette Create(TruckVisualSpec spec){var p=new Palette();p.Materials[Paint]=Mat(spec.paint,.65f);p.Materials[PaintAccent]=Mat(spec.accent,.5f);p.Materials[Chassis]=Mat(new Color(.06f,.07f,.08f),.25f);p.Materials[Rubber]=Mat(new Color(.015f,.015f,.018f),.05f);p.Materials[Glass]=Mat(new Color(.03f,.09f,.13f),.8f);p.Materials[Chrome]=Mat(new Color(.55f,.58f,.62f),.9f);p.Materials[Aluminium]=Mat(new Color(.45f,.47f,.50f),.65f);p.Materials[PlasticBlack]=Mat(new Color(.025f,.03f,.035f),.2f);p.Materials[Grille]=Mat(new Color(.04f,.045f,.05f),.35f);p.Materials[LensClear]=Mat(new Color(1f,.95f,.75f),.9f);p.Materials[LensAmber]=Mat(new Color(1f,.35f,.04f),.65f);p.Materials[LensRed]=Mat(new Color(.9f,.03f,.02f),.6f);p.Materials[TrailerSkin]=Mat(new Color(.82f,.84f,.86f),.45f);return p;}
        public static Material MakeLens(Color c)=>Mat(c,1f);
        private static Material Mat(Color c,float smooth){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);if(m.HasProperty("_Color"))m.SetColor("_Color",c);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return m;}
    }
}