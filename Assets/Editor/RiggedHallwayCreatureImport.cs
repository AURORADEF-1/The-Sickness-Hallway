using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
public static class RiggedHallwayCreatureImport {
 [Serializable] public class Data {public Node[] nodes;public Mat[] materials;public Geo[] meshes;}
 [Serializable] public class Node {public string name;public int parent;public float[] position;}
 [Serializable] public class Mat {public string name;public float[] color;public float metal,rough;}
 [Serializable] public class Geo {public string name;public float[] vertices,normals,weights;public int[] joints,indices;public int material;}
 public static void BuildAndTest(){Build();ThornbackCorridorVideo.Run();}
 public static void Build(){
  var d=JsonUtility.FromJson<Data>(File.ReadAllText("Assets/Editor/CreatureImport/creature.json"));
  const string dir="Assets/Resources/Medical/RiggedHallway";Directory.CreateDirectory(dir);AssetDatabase.Refresh();
  var root=new GameObject("ThornbackRiggedHallway");
  try{
   var bones=new Transform[d.nodes.Length];
   for(int i=0;i<bones.Length;i++){var n=d.nodes[i];bones[i]=new GameObject(n.name).transform;bones[i].SetParent(n.parent<0?root.transform:bones[n.parent],false);bones[i].localPosition=new Vector3(-n.position[0],n.position[1],n.position[2]);}
   var skinMap=SkinMap(dir);
   var mats=new Material[d.materials.Length];
   for(int i=0;i<mats.Length;i++){var m=d.materials[i];string path=dir+"/"+m.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",new Color(m.color[0],m.color[1],m.color[2],m.color[3]));mat.SetFloat("_Metallic",m.metal);mat.SetFloat("_Smoothness",1-m.rough);mat.SetFloat("_Cull",0);if(m.name=="skin"||m.name=="sinew"){mat.SetTexture("_BaseMap",skinMap);mat.SetFloat("_Smoothness",.30f);}if(m.name=="eyes"){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(.13f,.17f,.065f));}mats[i]=mat;EditorUtility.SetDirty(mat);}
   foreach(var g in d.meshes){var mesh=new Mesh{name=g.name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;int count=g.vertices.Length/3;var vertices=new Vector3[count];var normals=new Vector3[count];var weights=new BoneWeight[count];
    for(int i=0;i<count;i++){vertices[i]=new Vector3(-g.vertices[i*3],g.vertices[i*3+1],g.vertices[i*3+2]);normals[i]=new Vector3(-g.normals[i*3],g.normals[i*3+1],g.normals[i*3+2]);weights[i]=new BoneWeight{boneIndex0=g.joints[i*4],boneIndex1=g.joints[i*4+1],boneIndex2=g.joints[i*4+2],boneIndex3=g.joints[i*4+3],weight0=g.weights[i*4],weight1=g.weights[i*4+1],weight2=g.weights[i*4+2],weight3=g.weights[i*4+3]};}
    for(int i=0;i<g.indices.Length;i+=3){int tmp=g.indices[i];g.indices[i]=g.indices[i+2];g.indices[i+2]=tmp;}
    mesh.vertices=vertices;mesh.normals=normals;mesh.uv=vertices.Select(v=>new Vector2(v.x*2.3f+v.z*.6f,v.y*2.3f)).ToArray();mesh.triangles=g.indices;mesh.boneWeights=weights;mesh.bindposes=bones.Select(t=>t.worldToLocalMatrix*root.transform.localToWorldMatrix).ToArray();mesh.RecalculateBounds();
    string path=dir+"/"+g.name+".asset";var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(stored){EditorUtility.CopySerialized(mesh,stored);UnityEngine.Object.DestroyImmediate(mesh);mesh=stored;}else AssetDatabase.CreateAsset(mesh,path);
    var go=new GameObject(g.name);go.transform.SetParent(root.transform,false);var r=go.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=mesh;r.sharedMaterial=mats[g.material];r.bones=bones;r.rootBone=bones[0];r.updateWhenOffscreen=true;
   }
   PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Medical/ThornbackRiggedHallway.prefab");AssetDatabase.SaveAssets();Debug.Log("RIGGED_HALLWAY_IMPORTED bones="+bones.Length+" meshes="+d.meshes.Length);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static Texture2D SkinMap(string dir){
  const int size=512;var t=new Texture2D(size,size,TextureFormat.RGBA32,true);t.name="Mottled scarred hide";t.wrapMode=TextureWrapMode.Repeat;
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   float u=x/(float)size,v=y/(float)size;
   float broad=Mathf.PerlinNoise(u*9+11,v*9+23),grain=Mathf.PerlinNoise(u*120+3,v*120+7);
   float crease=Mathf.Pow(1-Mathf.Abs(Mathf.Sin(u*70+Mathf.PerlinNoise(u*6,v*6)*9+v*13)),14)*.16f;
   float shade=Mathf.Clamp01(.6f+broad*.32f+grain*.12f-crease);
   t.SetPixel(x,y,new Color(shade,shade*.94f,shade*.88f,1));
  }
  t.Apply();string path=dir+"/MottledHide.asset";var stored=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  if(stored){EditorUtility.CopySerialized(t,stored);UnityEngine.Object.DestroyImmediate(t);return stored;}AssetDatabase.CreateAsset(t,path);return t;
 }

}
