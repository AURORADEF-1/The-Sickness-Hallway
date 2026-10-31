using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

[InitializeOnLoad]
public static class ThornbackCorridorPlaytest {
 const string Key="Sickness.ThornbackReviewRunning";
 static WoodlandMedicalSequence flow;static MedicalThornbackCrawl crawl;
 static double deadline;static float began;static int step,initialFrame,captures;static float maxGrip,peak;
 static readonly float[] times={.72f,3.02f,5.32f,7.62f,9.92f,12.22f,14.52f};
 static readonly StringBuilder report=new StringBuilder();
 static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../work/thornback-corridor/live-review"));
 static ThornbackCorridorPlaytest(){EditorApplication.playModeStateChanged+=Mode;}
 public static void BuildAndRun(){ThornbackCorridorReview.CreateVariant();Run();}
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/Woodland.unity");
  var inventory=Object.FindFirstObjectByType<WoodlandInventory>();if(inventory)inventory.persist=false;
  Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
 }
 static void Mode(PlayModeStateChange state){
  if(!SessionState.GetBool(Key,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Tick;}
 }
 static void Tick(){
  try{
   if(EditorApplication.timeSinceStartup>deadline)throw new System.Exception("Playtest timed out");
   if(!Application.isPlaying)return;
   if(step==0){flow=Object.FindFirstObjectByType<WoodlandMedicalSequence>();if(!flow||!flow.player)return;flow.player.inventory.persist=false;flow.powered=true;flow.StartCoroutine(flow.Enter());step=1;return;}
   if(step==1){if(!flow.inside||!flow.interior||flow.loading)return;var room=flow.interior;
    foreach(var h in room.hostiles)h.gameObject.SetActive(false);room.bleedingExile.gameObject.SetActive(false);room.collapseStarted=true;
    flow.player.TeleportTo(new Vector3(200,.08f,37),180);flow.player.enabled=false;flow.player.eye.transform.localRotation=Quaternion.identity;
    flow.player.whiteHeadlight=true;flow.player.battery=12;flow.player.ApplyTorchMode();flow.player.torch.enabled=true;
    room.jammedDoor.localRotation=Quaternion.Euler(0,-105,0);room.breached=true;room.BeginChase();
    crawl=room.thornback.GetComponent<MedicalThornbackCrawl>();began=Time.time;initialFrame=Time.frameCount;Directory.CreateDirectory(Folder);step=2;return;
   }
   float elapsed=Time.time-began;maxGrip=Mathf.Max(maxGrip,crawl.GripError);
   var samples=new float[256];AudioListener.GetOutputData(samples,0);foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
   if(captures<times.Length&&flow.interior.chaseTime>=times[captures]){Snap(flow.player.eye,"live-"+times[captures].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));captures++;}
   if(elapsed>7&&step==2){
    var p=flow.player;var rotation=p.eye.transform.rotation;p.eye.transform.LookAt(crawl.transform.position+Vector3.up*1.18f);p.rounds=30;
    bool fired=p.Fire();p.eye.transform.rotation=rotation;report.AppendLine("Actual rifle fired: "+fired);step=3;
   }
   if(flow.interior.chaseTime<15)return;
   report.AppendLine("Frames advanced: "+(Time.frameCount-initialFrame));report.AppendLine("Encounter time: "+flow.interior.chaseTime);report.AppendLine("Claw plants: "+crawl.ContactCount);report.AppendLine("Max grip error metres: "+maxGrip);report.AppendLine("Audio output peak: "+peak);report.AppendLine("Shot reactions: "+crawl.hitCount);report.AppendLine("Variant: "+crawl.visual.name);
   bool blood=false;foreach(var ps in crawl.GetComponentsInChildren<ParticleSystem>())if(ps.name.ToLowerInvariant().Contains("blood"))blood=true;
   report.AppendLine("Blood emitter present: "+blood);
   bool passed=Time.frameCount-initialFrame>30&&crawl.ContactCount>=3&&crawl.hitCount==1&&!blood&&maxGrip<.03f;
   report.AppendLine(passed?"MECHANICAL CHECK PASSED; inspect images and listen before visual/audio signoff":"MECHANICAL CHECK FAILED");
   File.WriteAllText(Path.Combine(Folder,"report.txt"),report.ToString());End(passed?0:2);
  }catch(System.Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());End(2);}
 }
 static void Snap(Camera c,string name){var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=c.targetTexture;var active=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),tex.EncodeToPNG());}finally{c.targetTexture=old;RenderTexture.active=active;Object.Destroy(tex);Object.Destroy(rt);}}
 static void End(int exitCode){EditorApplication.update-=Tick;SessionState.SetBool(Key,false);EditorApplication.Exit(exitCode);}
}
