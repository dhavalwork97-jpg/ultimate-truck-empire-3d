using System.Collections.Generic;
using UnityEngine;
namespace UltimateTruckEmpire.Visuals
{
    public static class ProcMesh
    {
        private class S { public List<Vector3> v=new List<Vector3>(); public List<Vector3> n=new List<Vector3>(); public List<Vector2> uv=new List<Vector2>(); public List<int> t=new List<int>();
            public void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){var no=Vector3.Cross(b-a,c-a).normalized;int i=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);n.Add(no);n.Add(no);n.Add(no);n.Add(no);uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));uv.Add(new Vector2(0,1));t.Add(i);t.Add(i+1);t.Add(i+2);t.Add(i);t.Add(i+2);t.Add(i+3);}
            public Mesh Done(string name){var m=new Mesh{name=name};m.vertices=v.ToArray();m.normals=n.ToArray();m.uv=uv.ToArray();m.triangles=t.ToArray();m.RecalculateBounds();return m;}}
        public static Mesh Box(Vector3 s)=>Frustum(s.x,s.z,s.x,s.z,s.y);
        public static Mesh Box(float x,float y,float z)=>Box(new Vector3(x,y,z));
        public static Mesh Frustum(float bw,float bd,float tw,float td,float h){float y=h*.5f,bx=bw*.5f,bz=bd*.5f,tx=tw*.5f,tz=td*.5f;var s=new S();var b0=new Vector3(-bx,-y,-bz);var b1=new Vector3(bx,-y,-bz);var b2=new Vector3(bx,-y,bz);var b3=new Vector3(-bx,-y,bz);var t0=new Vector3(-tx,y,-tz);var t1=new Vector3(tx,y,-tz);var t2=new Vector3(tx,y,tz);var t3=new Vector3(-tx,y,tz);s.Q(b3,b2,b1,b0);s.Q(t0,t1,t2,t3);s.Q(b0,b1,t1,t0);s.Q(b2,b3,t3,t2);s.Q(b1,b2,t2,t1);s.Q(b3,b0,t0,t3);return s.Done("proc_frustum");}
        public static Mesh Cylinder(float r,float h,int seg,bool caps){seg=Mathf.Max(6,seg);float y=h*.5f;var s=new S();for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg,b=(i+1)*Mathf.PI*2/seg;var p0=new Vector3(Mathf.Cos(a)*r,-y,Mathf.Sin(a)*r);var p1=new Vector3(Mathf.Cos(b)*r,-y,Mathf.Sin(b)*r);var p2=new Vector3(Mathf.Cos(b)*r,y,Mathf.Sin(b)*r);var p3=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);s.Q(p0,p1,p2,p3);}if(caps){for(int i=0;i<seg;i++){float a=i*Mathf.PI*2/seg,b=(i+1)*Mathf.PI*2/seg;s.Q(new Vector3(0,y,0),new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r),new Vector3(Mathf.Cos(b)*r,y,Mathf.Sin(b)*r),new Vector3(0,y,0));}}return s.Done("proc_cylinder");}
        public static Mesh Extrude(Vector2[] p,float d){if(p==null||p.Length<3)return Box(Vector3.one*.01f);var s=new S();float z=d*.5f;for(int i=1;i<p.Length-1;i++){var a=p[0];var b=p[i];var c=p[i+1];s.Q(new Vector3(a.x,a.y,z),new Vector3(b.x,b.y,z),new Vector3(c.x,c.y,z),new Vector3(a.x,a.y,z));}for(int i=0;i<p.Length;i++){var a=p[i];var b=p[(i+1)%p.Length];s.Q(new Vector3(a.x,a.y,-z),new Vector3(b.x,b.y,-z),new Vector3(b.x,b.y,z),new Vector3(a.x,a.y,z));}return s.Done("proc_extrude");}
        public static Vector2[] CabSideProfile(float l,float h,float rake,float chamfer)=>new[]{new Vector2(-l*.5f,0),new Vector2(l*.5f,0),new Vector2(l*.5f,h-rake),new Vector2(l*.5f-rake,h),new Vector2(-l*.5f+chamfer,h),new Vector2(-l*.5f,h-chamfer)};
        public static void CabWindscreenSegment(float l,float h,float rake,out Vector2 bottom,out Vector2 top){bottom=new Vector2(l*.5f,h-rake);top=new Vector2(l*.5f-rake,h);}
        public static Vector2[] ChannelProfile(float h,float f,float t)=>new[]{new Vector2(0,-h*.5f),new Vector2(f,-h*.5f),new Vector2(f,-h*.5f+t),new Vector2(t,-h*.5f+t),new Vector2(t,h*.5f-t),new Vector2(f,h*.5f-t),new Vector2(f,h*.5f),new Vector2(0,h*.5f)};
        public static Vector2[] RectProfile(float w,float h,float c)=>new[]{new Vector2(-w*.5f+c,-h*.5f),new Vector2(w*.5f-c,-h*.5f),new Vector2(w*.5f,-h*.5f+c),new Vector2(w*.5f,h*.5f-c),new Vector2(w*.5f-c,h*.5f),new Vector2(-w*.5f+c,h*.5f),new Vector2(-w*.5f,h*.5f-c),new Vector2(-w*.5f,-h*.5f+c)};
        public static Vector2[] RingSector(float inner,float outer,float start,float end,int seg){var p=new List<Vector2>();for(int i=0;i<=seg;i++){float a=Mathf.Lerp(start,end,(float)i/seg)*Mathf.Deg2Rad;p.Add(new Vector2(Mathf.Cos(a)*outer,Mathf.Sin(a)*outer));}for(int i=seg;i>=0;i--){float a=Mathf.Lerp(start,end,(float)i/seg)*Mathf.Deg2Rad;p.Add(new Vector2(Mathf.Cos(a)*inner,Mathf.Sin(a)*inner));}return p.ToArray();}
    }
    public class MeshBuilder
    {
        private readonly List<Vector3> v=new List<Vector3>(); private readonly List<Vector3> n=new List<Vector3>(); private readonly List<Vector2> uv=new List<Vector2>(); private readonly Dictionary<int,List<int>> tris=new Dictionary<int,List<int>>();
        public int VertexCount=>v.Count;
        public void Add(Mesh m,Vector3 p,int slot)=>Add(m,p,Quaternion.identity,Vector3.one,slot);
        public void Add(Mesh m,Vector3 p,Quaternion r,int slot)=>Add(m,p,r,Vector3.one,slot);
        public void Add(Mesh m,Vector3 p,Quaternion r,Vector3 scale,int slot){if(m==null)return;int b=v.Count;var mv=m.vertices;var mn=m.normals;var mu=m.uv;for(int i=0;i<mv.Length;i++){v.Add(r*Vector3.Scale(mv[i],scale)+p);n.Add(mn!=null&&i<mn.Length?r*mn[i]:Vector3.up);uv.Add(mu!=null&&i<mu.Length?mu[i]:Vector2.zero);}if(!tris.TryGetValue(slot,out var list)){list=new List<int>();tris[slot]=list;}foreach(var x in m.triangles)list.Add(b+x);}
        public Mesh Build(string name,out int[] slots){var keys=new List<int>(tris.Keys);keys.Sort();slots=keys.ToArray();var m=new Mesh{name=name};if(v.Count>65000)m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.vertices=v.ToArray();m.normals=n.ToArray();m.uv=uv.ToArray();m.subMeshCount=slots.Length;for(int i=0;i<slots.Length;i++)m.SetTriangles(tris[slots[i]],i);m.RecalculateBounds();return m;}
        public GameObject Emit(string name,Transform parent,TruckMaterialLibrary.Palette p){int[] slots;var go=new GameObject(name);go.transform.SetParent(parent,false);var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=Build(name+"_mesh",out slots);var mr=go.AddComponent<MeshRenderer>();var mats=new Material[slots.Length];for(int i=0;i<slots.Length;i++)mats[i]=p.Get(slots[i]);mr.sharedMaterials=mats;return go;}
    }
}