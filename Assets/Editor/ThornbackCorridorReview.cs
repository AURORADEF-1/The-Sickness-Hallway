using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

// Explicit commands only: importing this script never edits a scene or starts play.
public static class ThornbackCorridorReview {
 const string Scene="Assets/Scenes/MedicalResearch.unity";
 [MenuItem("The Sickness/Thornback/Create corridor model variant")]
 public static void CreateVariant(){
  if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode first");
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Woodland/Medical/Models/ThornbackCorrected.fbx");
  if(!source)throw new System.InvalidOperationException("Original Thornback FBX is not imported yet");
  const string plasterPath="Assets/Resources/Medical/ThornbackPlaster.mat";
  var plaster=AssetDatabase.LoadAssetAtPath<Material>(plasterPath);
  if(!plaster){plaster=new Material(Shader.Find("Universal Render Pipeline/Lit"));plaster.SetColor("_BaseColor",new Color(.30f,.28f,.24f));plaster.SetFloat("_Smoothness",.08f);plaster.SetFloat("_Cull",0);AssetDatabase.CreateAsset(plaster,plasterPath);}
  var holder=new GameObject("Temporary corridor variant authoring");
  try{
   var visual=Object.Instantiate(source,holder.transform).transform;
   var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Woodland/Medical/Thornback mottled skin.mat");
   foreach(var renderer in visual.GetComponentsInChildren<Renderer>())if(material)renderer.sharedMaterial=material;
   var crawl=holder.AddComponent<MedicalThornbackCrawl>();crawl.visual=visual;crawl.Prepare();
   visual.name="ThornbackCorridorVisual";
   PrefabUtility.SaveAsPrefabAsset(visual.gameObject,"Assets/Resources/Medical/ThornbackCorridorVisual.prefab");
   AssetDatabase.SaveAssets();Debug.Log("THORNBACK_VARIANT_SAVED: original FBX preserved; elongated rig and contact targets saved");
  }finally{Object.DestroyImmediate(holder);}
 }
 [MenuItem("The Sickness/Thornback/Capture corridor pose review")]
 public static void RebuildAndCapture(){CreateVariant();Capture();}
 public static void Capture(){
  if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play mode first");
  var scene=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Additive);
  GameObject cameraObject=null,lightObject=null;
  try{
   MedicalInterior room=null;foreach(var root in scene.GetRootGameObjects()){room=root.GetComponent<MedicalInterior>();if(room)break;}
   if(!room)throw new System.InvalidOperationException("MedicalInterior not found");
   room.jammedDoor.localRotation=Quaternion.Euler(0,-105,0);room.breached=true;
   room.thornback.gameObject.SetActive(true);room.thornback.localPosition=new Vector3(0,0,29.5f);room.thornback.localRotation=Quaternion.identity;
   var crawl=room.thornback.GetComponent<MedicalThornbackCrawl>();
   cameraObject=new GameObject("Review camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=room.transform.position+new Vector3(0,1.64f,35.8f);camera.transform.rotation=Quaternion.Euler(0,180,0);camera.fieldOfView=58;camera.nearClipPlane=.04f;camera.farClipPlane=30;camera.backgroundColor=Color.black;camera.clearFlags=CameraClearFlags.SolidColor;
   lightObject=new GameObject("Review torch");var light=lightObject.AddComponent<Light>();light.type=LightType.Spot;light.range=15;light.intensity=4;light.spotAngle=65;light.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
   string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../work/thornback-corridor/review"));Directory.CreateDirectory(folder);
   var trace=new System.Text.StringBuilder();
   foreach(float t in new[]{-1f,1.8f,3.6f,6.5f,9f,12f}){
    if(t>=0)crawl.Pose(t);trace.AppendLine("time="+t+" gripError="+crawl.GripError);
    foreach(var bone in crawl.visual.GetComponentsInChildren<Transform>())if(bone.name.Contains("arm")||bone.name.Contains("claw contact"))trace.AppendLine(bone.name+" position="+bone.position+" up="+bone.up+" local="+bone.localPosition);
    var rt=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,"pose-"+t.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png"),texture.EncodeToPNG());}
    finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);}
   }
   File.WriteAllText(Path.Combine(folder,"pose-metrics.txt"),trace.ToString());
   Debug.Log("THORNBACK_POSE_REVIEW: "+folder+"; stills only, not a live playtest");
  }finally{if(cameraObject)Object.DestroyImmediate(cameraObject);if(lightObject)Object.DestroyImmediate(lightObject);EditorSceneManager.CloseScene(scene,true);}
 }
}
