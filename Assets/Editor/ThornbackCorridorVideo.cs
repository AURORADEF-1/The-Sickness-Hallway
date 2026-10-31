using UnityEngine;
using UnityEditor.Media;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

[InitializeOnLoad]
public static class ThornbackCorridorVideo {
 const string Key="Sickness.ThornbackVideoRunning";
 static MediaEncoder encoder; static RenderTexture videoRT; static Texture2D videoFrame; static int lastFrame=-1, videoFrames;
 static WoodlandMedicalSequence flow;static MedicalThornbackCrawl crawl;
 static double deadline;static float began;static int step,initialFrame,captures;static float maxGrip,peak,maxCameraRestoreError;
 static double lureSum,roarSum,themeSum;static long lureSamples,roarSamples,themeSamples;static float blackPeak;static bool blackSnap,titleSnap,fadeSnap;static double blackPixelMean,titlePixelMean,fadePixelMean,titleRedRatio;static int endingAudioViolations;static float firstThemeEnergyAge=-1;
 static readonly float[] times={.72f,3.02f,5.32f,7.62f,9.92f,12.22f,14.52f};
 static readonly StringBuilder report=new StringBuilder();
 static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../work/thornback-theme-ending/encounter-review"));
 static ThornbackCorridorVideo(){EditorApplication.playModeStateChanged+=Mode;}
 public static void BuildAndRun(){ThornbackDoorFragmentBuilder.Build();
  string path="Assets/Resources/Medical/ThornbackSpark.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",new Color(4,2.4f,.6f));EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();Run();}
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/Woodland.unity");
  var inventory=Object.FindFirstObjectByType<WoodlandInventory>();if(inventory)inventory.persist=false;
  Application.runInBackground=true;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
 }
 static void Mode(PlayModeStateChange state){
  if(!SessionState.GetBool(Key,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode){deadline=EditorApplication.timeSinceStartup+600;EditorApplication.update+=Tick;}
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
    Directory.CreateDirectory(Folder); Time.captureDeltaTime=1f/24;
    encoder=new MediaEncoder(Path.Combine(Folder,"The Sickness - Thornback Corridor.mp4"),new VideoTrackAttributes{width=1280,height=720,frameRate=new MediaRational(24),includeAlpha=false,bitRateMode=UnityEditor.VideoBitrateMode.High},new AudioTrackAttributes{sampleRate=new MediaRational(AudioSettings.outputSampleRate),channelCount=2,language="en"});
    videoRT=new RenderTexture(1280,720,24);videoFrame=new Texture2D(1280,720,TextureFormat.RGBA32,false);AudioRenderer.Start();
    crawl=room.thornback.GetComponent<MedicalThornbackCrawl>();began=Time.time;initialFrame=Time.frameCount;Directory.CreateDirectory(Folder);step=2;return;
   }
   if(lastFrame==Time.frameCount)return; lastFrame=Time.frameCount;
   CaptureVideo(flow.player.eye);
   foreach(var acoustic in crawl.GetComponentsInChildren<MedicalCorridorAcoustics>())if(acoustic.name=="Claws dragging on masonry"&&acoustic.Distance>0){minGain=Mathf.Min(minGain,acoustic.DistanceGain);maxGain=Mathf.Max(maxGain,acoustic.DistanceGain);}
   float elapsed=Time.time-began;maxGrip=Mathf.Max(maxGrip,crawl.GripError);
   var samples=new float[256];AudioListener.GetOutputData(samples,0);foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
   if(captures<times.Length&&flow.interior.chaseTime>=times[captures]){Snap(flow.player.eye,"live-"+times[captures].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));captures++;}
   if(crawl.PresentationTime>7&&step==2){
    var p=flow.player;var rotation=p.eye.transform.rotation;p.eye.transform.LookAt(crawl.transform.position+Vector3.up*1.18f);p.rounds=30;
    bool fired=p.Fire();p.eye.transform.rotation=rotation;report.AppendLine("Actual rifle fired: "+fired);step=3;
   }
   if(videoFrames<24*75&&(!flow.interior.finished||flow.interior.EndAge<18.3f))return;
   report.AppendLine("Frames advanced: "+(Time.frameCount-initialFrame));report.AppendLine("Encounter time: "+flow.interior.chaseTime);report.AppendLine("Claw plants: "+crawl.ContactCount);report.AppendLine("Max grip error metres: "+maxGrip);report.AppendLine("Audio output peak: "+peak);report.AppendLine("Shot reactions: "+crawl.hitCount);report.AppendLine("Variant: "+crawl.visual.name);
   bool blood=false;foreach(var ps in crawl.GetComponentsInChildren<ParticleSystem>())if(ps.name.ToLowerInvariant().Contains("blood"))blood=true;
   report.AppendLine("Blood emitter present: "+blood);
   var broken=flow.interior.thornback.GetComponent<MedicalThornbackDoorBreak>();report.AppendLine("Door broken: "+(broken&&broken.Broken));report.AppendLine("Door fragments: "+(broken?broken.FragmentCount:0));report.AppendLine("Legacy vocal loop playing: "+flow.interior.threatAudio.isPlaying);
   var damage=flow.interior.thornback.GetComponent<MedicalElectricalDamage>();var breath=flow.player.GetComponent<MedicalPanicBreathing>();
   report.AppendLine("Settled fragments: "+broken.SettledCount);float highest=0;foreach(var plank in Object.FindObjectsByType<MedicalTimberSettler>(FindObjectsSortMode.None))highest=Mathf.Max(highest,plank.GetComponent<Collider>().bounds.max.y-flow.interior.transform.position.y);report.AppendLine("Highest resting timber top: "+highest);report.AppendLine("Approach distance gain: "+minGain+" -> "+maxGain);report.AppendLine("Wire shorts: "+damage.WireCount+" Ceiling shorts: "+damage.CeilingCount);report.AppendLine("Maximum panic fog: "+breath.MaxFog);
   var surfaces=flow.interior.GetComponent<MedicalDoorwaySurfaceDetail>();
   report.AppendLine("Header sealed="+surfaces.HeaderSealed+" bounds="+surfaces.HeaderBounds+" surfaces="+surfaces.SurfaceCount+" claw impacts="+surfaces.ImpactCount+" gouges="+surfaces.GougeCount+" shavings="+surfaces.ShavingCount+" settled="+surfaces.SettledShavings);
   var foley=crawl.GetComponent<MedicalThornbackEffects>();report.AppendLine("Roar played="+foley.RoarPlayed+" duration="+foley.RoarDuration+" shake peak="+foley.RoarCameraPeak+" camera restore error="+maxCameraRestoreError);report.AppendLine("Scraping seconds: "+foley.ScrapingSeconds+" Invalid scrape frames: "+foley.InvalidScrapeFrames+" Jaw snaps: "+crawl.JawSnaps);
   report.AppendLine("Claw thuds: "+foley.PlantThuds+" Pull crumbles: "+foley.PullCrumbles);
   report.AppendLine("Doorway beat="+crawl.Doorway.Active+" back steps="+crawl.Doorway.BackSteps+" roar="+crawl.Doorway.Roared+" left="+crawl.Doorway.LeftTarget+" right="+crawl.Doorway.RightTarget);
   report.AppendLine("Hidden lead-in NPC voices silenced="+hiddenDialogueSilenced+" no spoiler prompt="+hiddenPromptClear);var mimic=crawl.Mimic;report.AppendLine("Mimic started="+mimic.Started+" started hidden="+mimic.VoiceStartedHidden+" first pitch="+mimic.FirstVoicePitch+" hidden collider violations="+mimic.HiddenColliderViolations+" voice ended="+mimic.VoiceEnded+" lowest pitch="+mimic.LowestPitch+" max distortion="+mimic.MaximumDistortion+" max breath="+mimic.MaximumBreath+" hidden renderer violations="+mimic.HiddenRendererViolations);
   double lureRms=System.Math.Sqrt(lureSum/System.Math.Max(1,lureSamples)),roarRms=System.Math.Sqrt(roarSum/System.Math.Max(1,roarSamples));
   report.AppendLine("Lure RMS="+lureRms+" Roar RMS="+roarRms+" Contrast dB="+(20*System.Math.Log10(roarRms/System.Math.Max(.000001,lureRms)))+" theme audio peak="+blackPeak);
   report.AppendLine("Black captured="+blackSnap+" black mean="+blackPixelMean+" title captured="+titleSnap+" title mean="+titlePixelMean+" title visible="+flow.interior.TitleVisible+" end age="+flow.interior.EndAge+" voice gain="+mimic.MaximumVoiceGain+" voice reverb decay="+mimic.VoiceReverbDecay);
   double themeRms=System.Math.Sqrt(themeSum/System.Math.Max(1,themeSamples));
   report.AppendLine("Ending theme count="+flow.interior.ThemePlayCount+" started at="+flow.interior.ThemeStartedAt+" duration="+flow.interior.ThemeDuration+" first energy age="+firstThemeEnergyAge+" theme RMS="+themeRms+" other audible source frames="+endingAudioViolations);
   report.AppendLine("Red title ratio="+titleRedRatio+" title fade captured="+fadeSnap+" fade mean="+fadePixelMean+" title alpha="+flow.interior.TitleAlpha+" coming soon alpha="+flow.interior.ComingSoonAlpha);
   float replyDelay=mimic.PostShotStartedAt-mimic.PostShotRequestedAt;
   report.AppendLine("Post-shot reply count="+mimic.PostShotPlayCount+" replaced lure="+mimic.PostShotReplacedLure+" delay="+replyDelay+" finished="+mimic.PostShotFinished+" started at="+mimic.PostShotStartedAt);
   report.AppendLine("Hello underlay play count="+mimic.UnderlayPlayCount+" started hidden="+mimic.UnderlayStartedHidden+" overlap frames="+mimic.UnderlayOverlapFrames+" maximum gain="+mimic.MaximumUnderlayGain);
   bool passed=mimic.UnderlayPlayCount==1&&mimic.UnderlayStartedHidden&&mimic.UnderlayOverlapFrames>20&&mimic.MaximumUnderlayGain>.10f&&mimic.MaximumUnderlayGain<=.14f&&mimic.PostShotPlayCount==1&&mimic.PostShotReplacedLure&&mimic.PostShotFinished&&replyDelay>=.40f&&replyDelay<.55f&&blackSnap&&titleSnap&&blackPixelMean<.001&&titlePixelMean>.002&&blackPeak>.05&&blackPeak<.8&&themeRms>.02&&flow.interior.ThemePlayCount==1&&flow.interior.ThemeStartedAt<.05f&&flow.interior.ThemeDuration>17&&firstThemeEnergyAge>=0&&firstThemeEnergyAge<.15f&&endingAudioViolations==0&&fadeSnap&&fadePixelMean>0&&fadePixelMean<titlePixelMean&&titleRedRatio>4&&flow.interior.TitleAlpha>.99f&&flow.interior.ComingSoonAlpha>.99f&&flow.interior.TitleVisible&&mimic.MaximumVoiceGain<=.27f&&mimic.VoiceReverbDecay>=4&&roarRms>lureRms*4&&hiddenDialogueSilenced&&hiddenPromptClear&&mimic.Started&&mimic.VoiceStartedHidden&&Mathf.Abs(mimic.FirstVoicePitch-1)<.01f&&mimic.HiddenColliderViolations==0&&mimic.VoiceEnded&&mimic.LowestPitch<.75f&&mimic.MaximumDistortion>.15f&&mimic.MaximumBreath>.06f&&mimic.HiddenRendererViolations==0&&surfaces.HeaderSealed&&surfaces.ImpactCount==2&&surfaces.GougeCount==8&&surfaces.ShavingCount==36&&surfaces.SettledShavings==36&&surfaces.HighestSettledShaving<.02f&&foley.RoarPlayed&&foley.RoarDuration>=6.5f&&foley.RoarCameraPeak>.02f&&maxCameraRestoreError<.0001f&&peak<.99f&&crawl.Doorway.Complete&&crawl.Doorway.BackSteps>=2&&crawl.Doorway.Roared&&foley.PlantThuds==crawl.ContactCount&&foley.PullCrumbles>0&&foley.ScrapingSeconds>0&&foley.InvalidScrapeFrames==0&&highest<.35f&&maxGain>minGain*1.4f&&broken.SettledCount==14&&damage.WireCount>0&&damage.CeilingCount>0&&breath.MaxFog>0&&breath.MaxFog<=.046f&&broken&&broken.Broken&&broken.FragmentCount==14&&!flow.interior.threatAudio.isPlaying&&Time.frameCount-initialFrame>30&&crawl.ContactCount>=3&&crawl.hitCount==1&&!blood&&maxGrip<.03f;
   report.AppendLine(passed?"MECHANICAL CHECK PASSED; inspect images and listen before visual/audio signoff":"MECHANICAL CHECK FAILED");
   File.WriteAllText(Path.Combine(Folder,"report.txt"),report.ToString());End(passed?0:2);
  }catch(System.Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());End(2);}
 }
 static float minGain=1,maxGain;static bool hiddenSnap;static bool hiddenDialogueSilenced,hiddenPromptClear;static int doorwayCaptures;static int breakSnaps;static bool settledSnap;static int sparksSnaps;
 static void CaptureVideo(Camera c){
 if(crawl&&crawl.Mimic&&crawl.Mimic.IsHidden&&!hiddenSnap&&flow.interior.chaseTime>2){hiddenDialogueSilenced=true;foreach(var voice in flow.interior.GetComponentsInChildren<MedicalVoice>(true))hiddenDialogueSilenced&=voice.SilencedForEncounter;hiddenPromptClear=string.IsNullOrEmpty(flow.interior.Prompt());Snap(c,"mimic-before-reveal");var lamp=flow.player.torch;var color=lamp.color;float power=lamp.intensity;lamp.color=Color.white;lamp.intensity=35;Snap(c,"diagnostic-hidden-monster");lamp.color=color;lamp.intensity=power;hiddenSnap=true;}
 if(crawl&&crawl.Doorway&&crawl.Doorway.Active&&doorwayCaptures<7&&crawl.Doorway.Age>new float[]{.25f,2.2f,4.5f,7.9f,9.5f,12,16.5f}[doorwayCaptures]){Snap(c,"doorway-"+doorwayCaptures);var lamp=flow.player.torch;var oldColor=lamp.color;float oldPower=lamp.intensity;lamp.color=Color.white;lamp.intensity=35;Snap(c,"diagnostic-grip-"+doorwayCaptures);
 if(doorwayCaptures==2){var pose=c.transform.position;var rotation=c.transform.rotation;bool rifle=flow.player.rifle.gameObject.activeSelf;flow.player.rifle.gameObject.SetActive(false);c.transform.position=new Vector3(199.85f,1.58f,35.15f);c.transform.LookAt(new Vector3(199.25f,1.40f,34.20f));Snap(c,"diagnostic-claw-gouges");c.transform.position=new Vector3(199.85f,1.15f,35.1f);c.transform.LookAt(new Vector3(199.3f,.05f,34.45f));Snap(c,"diagnostic-settled-shavings");c.transform.position=pose;c.transform.rotation=rotation;flow.player.rifle.gameObject.SetActive(rifle);}
 lamp.color=oldColor;lamp.intensity=oldPower;doorwayCaptures++;}
 var damage=flow.interior.thornback.GetComponent<MedicalElectricalDamage>();if(damage&&damage.LastContact>0&&Time.time-damage.LastContact<.18f&&sparksSnaps<5){Snap(c,"spark-"+sparksSnaps++);}
 var door=flow.interior.thornback.GetComponent<MedicalThornbackDoorBreak>();if(door&&door.Broken&&door.FlashAge>3&&!settledSnap){Snap(c,"settled-door");var saved=c.transform.rotation;var rifleActive=flow.player.rifle.gameObject.activeSelf;flow.player.rifle.gameObject.SetActive(false);c.transform.LookAt(flow.interior.jammedDoor.position+Vector3.forward*.35f);Snap(c,"floor-inspection");c.transform.rotation=saved;flow.player.rifle.gameObject.SetActive(rifleActive);settledSnap=true;}
 if(door&&door.Broken&&breakSnaps<3&&door.FlashAge>=breakSnaps*.08f){Snap(c,"door-break-"+breakSnaps);breakSnaps++;}
 var old=c.targetTexture;var active=RenderTexture.active;
 var posBefore=c.transform.localPosition;var rotBefore=c.transform.localRotation;float fovBefore=c.fieldOfView;
 try {c.targetTexture=videoRT;c.Render();maxCameraRestoreError=Mathf.Max(maxCameraRestoreError,Vector3.Distance(posBefore,c.transform.localPosition),Quaternion.Angle(rotBefore,c.transform.localRotation),Mathf.Abs(fovBefore-c.fieldOfView));RenderTexture.active=videoRT;videoFrame.ReadPixels(new Rect(0,0,1280,720),0,0);videoFrame.Apply();
 if(flow.interior.finished){
  if(!blackSnap&&flow.interior.EndAge>.08f&&!flow.interior.TitleVisible){blackSnap=true;blackPixelMean=ImageMean(videoFrame);File.WriteAllBytes(Path.Combine(Folder,"ending-black.png"),videoFrame.EncodeToPNG());}
  if(!fadeSnap&&flow.interior.EndAge>1.4f&&flow.interior.EndAge<2){fadeSnap=true;fadePixelMean=ImageMean(videoFrame);File.WriteAllBytes(Path.Combine(Folder,"ending-title-fade.png"),videoFrame.EncodeToPNG());}
  if(!titleSnap&&flow.interior.EndAge>5.5f){titleSnap=true;titlePixelMean=ImageMean(videoFrame);double red=0,other=0;foreach(var px in videoFrame.GetPixels32()){red+=px.r;other+=px.g+px.b;}titleRedRatio=red/System.Math.Max(1,other*.5);File.WriteAllBytes(Path.Combine(Folder,"ending-title.png"),videoFrame.EncodeToPNG());}
  if(flow.interior.EndAge>.15f)foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))if(source.isPlaying&&!source.mute&&source.volume>.001f&&source.name!=MedicalInterior.EndingThemeSourceName)endingAudioViolations++;
 }
 if(!encoder.AddFrame(videoFrame))throw new System.Exception("Video frame rejected");}
 finally{c.targetTexture=old;RenderTexture.active=active;}
 int count=AudioRenderer.GetSampleCountForCaptureFrame();if(count>0){using(var data=new NativeArray<float>(count*2,Allocator.Temp)){if(AudioRenderer.Render(data)){var fx=crawl.GetComponent<MedicalThornbackEffects>();for(int i=0;i<data.Length;i++){
  float sample=data[i];peak=Mathf.Max(peak,Mathf.Abs(sample));
  if(flow.interior.chaseTime<5){lureSum+=sample*sample;lureSamples++;}
  if(fx&&fx.RoarPlayed&&!flow.interior.finished){roarSum+=sample*sample;roarSamples++;}
  if(flow.interior.finished){
   if(firstThemeEnergyAge<0&&Mathf.Abs(sample)>.002f)firstThemeEnergyAge=flow.interior.EndAge;
   if(flow.interior.EndAge>.12f){blackPeak=Mathf.Max(blackPeak,Mathf.Abs(sample));themeSum+=sample*sample;themeSamples++;}
  }
 }encoder.AddSamples(data);}}}
 videoFrames++;
 }
 static double ImageMean(Texture2D tex){double total=0;var pixels=tex.GetPixels32();foreach(var px in pixels)total+=px.r+px.g+px.b;return total/(pixels.Length*3.0*255);}
 static void Snap(Camera c,string name){var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=c.targetTexture;var active=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),tex.EncodeToPNG());}finally{c.targetTexture=old;RenderTexture.active=active;Object.Destroy(tex);Object.Destroy(rt);}}
 static void End(int exitCode){if(encoder!=null){AudioRenderer.Stop();encoder.Dispose();encoder=null;Time.captureDeltaTime=0;File.AppendAllText(Path.Combine(Folder,"report.txt"),"\nVideo frames: "+videoFrames+"\nAudio peak: "+peak);}EditorApplication.update-=Tick;SessionState.SetBool(Key,false);EditorApplication.Exit(exitCode);}
}
