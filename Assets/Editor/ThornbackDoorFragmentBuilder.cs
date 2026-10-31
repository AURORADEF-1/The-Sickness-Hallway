using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public static class ThornbackDoorFragmentBuilder {
 struct Vertex { public Vector3 p;public Vector2 uv;public Vertex(Vector3 position,Vector2 tex){p=position;uv=tex;}public static Vertex Mix(Vertex a,Vertex b,float t){return new Vertex(Vector3.Lerp(a.p,b.p,t),Vector2.Lerp(a.uv,b.uv,t));}}
 public static void Build(){
  EditorSceneManager.OpenScene("Assets/Scenes/MedicalResearch.unity");var room=Object.FindFirstObjectByType<MedicalInterior>();var leaf=room.jammedDoor.GetComponentInChildren<MeshFilter>();var original=leaf.sharedMesh;
  var pos=original.vertices;var uv=original.uv;var indices=original.triangles;var b=original.bounds;
  string folder="Assets/Resources/Medical/ThornbackDoorFragments";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
  var rng=new System.Random(34);var grid=new Vector2[8,3];
  for(int x=0;x<8;x++)for(int y=0;y<3;y++)grid[x,y]=new Vector2(Mathf.Lerp(b.min.x,b.max.x,x/7f)+(x>0&&x<7?(float)(rng.NextDouble()-.5)*b.size.x*.09f:0),Mathf.Lerp(b.min.y,b.max.y,y/2f)+(y>0&&y<2?(float)(rng.NextDouble()-.5)*b.size.y*.12f:0));
  int index=0;
  for(int x=0;x<7;x++)for(int y=0;y<2;y++){
   Vector2[] outline={grid[x,y],grid[x+1,y],grid[x+1,y+1],grid[x,y+1]};Vector2 center=Vector2.zero;foreach(var v in outline)center+=v;center/=4;
   Vector3 offset=new Vector3(center.x,center.y,b.center.z);var vertices=new List<Vector3>();var tex=new List<Vector2>();var faces=new List<int>();var caps=new List<int>();
   for(int i=0;i<indices.Length;i+=3){var poly=new List<Vertex>();for(int j=0;j<3;j++){int id=indices[i+j];poly.Add(new Vertex(pos[id],uv.Length==pos.Length?uv[id]:Vector2.zero));}
    for(int edge=0;edge<4&&poly.Count>0;edge++)poly=Clip(poly,outline[edge],outline[(edge+1)%4]);
    for(int j=1;j<poly.Count-1;j++){foreach(int k in new[]{0,j,j+1}){faces.Add(vertices.Count);vertices.Add(poly[k].p-offset);tex.Add(poly[k].uv);}}
   }
   // Exposed timber through the thickness; UVs on painted faces stay exactly as authored.
   for(int edge=0;edge<4;edge++){
    Vector2 a=outline[edge],c=outline[(edge+1)%4];int start=vertices.Count;
    vertices.Add(new Vector3(a.x,a.y,b.min.z)-offset);vertices.Add(new Vector3(c.x,c.y,b.min.z)-offset);vertices.Add(new Vector3(c.x,c.y,b.max.z)-offset);vertices.Add(new Vector3(a.x,a.y,b.max.z)-offset);
    for(int j=0;j<4;j++)tex.Add(Vector2.zero);caps.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
   }
   var mesh=new Mesh{name="Door fragment "+index};mesh.SetVertices(vertices);mesh.SetUVs(0,tex);mesh.subMeshCount=2;mesh.SetTriangles(faces,0);mesh.SetTriangles(caps,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
   string path=folder+"/fragment-"+index+++".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);
  }
  AssetDatabase.SaveAssets();Debug.Log("Built 14 door fragments with preserved source UVs from "+original.name+" bounds "+b);
 }
 static float Side(Vertex v,Vector2 a,Vector2 b){return (b.x-a.x)*(v.p.y-a.y)-(b.y-a.y)*(v.p.x-a.x);}
 static List<Vertex> Clip(List<Vertex> input,Vector2 a,Vector2 b){var result=new List<Vertex>();for(int i=0;i<input.Count;i++){var p=input[i];var q=input[(i+1)%input.Count];float dp=Side(p,a,b),dq=Side(q,a,b);if(dp>=0)result.Add(p);if((dp>=0)!=(dq>=0))result.Add(Vertex.Mix(p,q,dp/(dp-dq)));}return result;}
}
