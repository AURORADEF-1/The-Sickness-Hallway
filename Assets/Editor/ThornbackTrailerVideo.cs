using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Captures the authored cinematic through the game's camera and audio renderer.
// The director owns all actions: this harness never fires or advances its beats.
[InitializeOnLoad]
public static class ThornbackTrailerVideo {
 const string SessionKey="Sickness.ThornbackTrailerRunning";
 const int Width=1280,Height=720,Fps=24;
 static readonly float[] KeyTimes={.40f,2.45f,3f,3.2f,4.45f,6.35f,11.5f,16.1f,20.2f,22.5f,23.3f,26.05f,27.08f,28.6f,29.2f,29.75f,30.4f,33f,35.5f,43.5f};
 static readonly string[] KeyNames={"00a-lit-corridor-exile","00b-exile-torch-flicker","00bb-pre-vanish-blackout","00c-exile-vanished","00d-empty-corridor-flash","00e-masked-oh-fuck","01-follow-distant-mimic","02-backpack-no-spare-batteries","03-try-white-torch","04-white-torch-failed-red-remains","04a-red-only-after-white-failure","05-first-hip-shot","05a-ads-second-shot","05b-ads-final-shot","06-empty-first-click","06a-empty-second-click","06b-masked-empty-panic","07-back-away","07a-red-only-doorway-approach","08d-red-only-late-roar"};
 static readonly StringBuilder Report=new StringBuilder();
 static readonly StringBuilder ScrapeTrace=new StringBuilder("director_s,active,speed_m_s,contact,gain,distance_m,contact_x,contact_y,contact_z\n");
 static readonly StringBuilder ShakeTrace=new StringBuilder("director_s,roar_s,base_x,base_y,base_z,render_x,render_y,render_z,rotation_deg,fov_delta_deg,doorway_pixel_shift,restoration_error\n");
 static readonly float[] EventAges=new float[10];
 static readonly string[] EventNames={"Forward movement","Backpack checked","White torch attempted","White torch failed","Rifle volley","Empty rifle click","Retreat movement","Final roar","Black cut","Title visible"};
 static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../work/thornback-drag-8560/encounter-review"));
 static WoodlandMedicalSequence flow;
 static MedicalTrailerSequence director;
 static MedicalThornbackCrawl crawl;
 static MedicalThornbackEffects effects;
 static MediaEncoder encoder;
 static RenderTexture videoTarget;
 static Texture2D videoFrame;
 static int stage,lastFrame=-1,videoFrames,keyframeIndex,endingOtherAudioFrames,roarRenderFrames,roarMovedFrames,blackShakeFrames,litRoarFrames;
 static double deadline,lurePower,roarPower,themePower;
 static long lureSamples,roarSamples,themeSamples;
 static float peak,maxGrip,cameraRestoreError,firstThemeEnergyAge=-1,roarStartedAt=-1,blackStartedAt=-1;
 static bool doorwaySnap,gripsSnap,pullSnap,blackSnap,fadeSnap,titleSnap,themeObserved;
 static int runtimeErrors;static readonly StringBuilder RuntimeLog=new StringBuilder();
 static bool inkMiddleSnap,inkEndSnap;static float inkMiddleLength,inkEndLength;
 static float renderedRoarPositionPeak,renderedRoarRotationPeak,renderedRoarFovPeak,roarProjectionPeak,blackCameraPeak;
 static double blackMean,fadeMean,titleMean,titleRedRatio;
 static FieldSave originalInventory;
 static int originalCells,originalAmmo,originalRounds;
 static float originalBattery;
 static bool originalPersist;

 static ThornbackTrailerVideo(){EditorApplication.playModeStateChanged+=ModeChanged;}
 public static void Run(){
  Application.logMessageReceived+=LogMessage;
  Directory.CreateDirectory(Folder);
  EditorSceneManager.OpenScene("Assets/Scenes/Woodland.unity");
  var inventory=Object.FindFirstObjectByType<WoodlandInventory>();if(inventory)inventory.persist=false;
  Application.runInBackground=true;
  SessionState.SetBool(SessionKey,true);
  EditorApplication.isPlaying=true;
 }
 static void LogMessage(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert){runtimeErrors++;RuntimeLog.AppendLine(message+"\n"+stack);}}
 static void ModeChanged(PlayModeStateChange state){
  if(!SessionState.GetBool(SessionKey,false)||state!=PlayModeStateChange.EnteredPlayMode)return;
  for(int i=0;i<EventAges.Length;i++)EventAges[i]=-1;
  deadline=EditorApplication.timeSinceStartup+780;
  EditorApplication.update+=Tick;
 }
 static void Tick(){
  try{
   if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Trailer recording exceeded the wall-clock timeout.");
   if(!Application.isPlaying)return;
   if(stage==0){
    flow=Object.FindFirstObjectByType<WoodlandMedicalSequence>();if(!flow||!flow.player)return;
    flow.player.inventory.persist=false;flow.powered=true;flow.StartCoroutine(flow.Enter());stage=1;return;
   }
   if(stage==1){
    if(!flow.inside||!flow.interior||flow.loading)return;
    var room=flow.interior;
    foreach(var hostile in room.hostiles)if(hostile)hostile.gameObject.SetActive(false);
    if(room.bleedingExile)room.bleedingExile.gameObject.SetActive(false);
    room.collapseStarted=true;
    flow.player.TeleportTo(new Vector3(200,.08f,39.2f),180);
    flow.player.eye.transform.localRotation=Quaternion.identity;
    room.jammedDoor.localRotation=Quaternion.Euler(0,-105,0);room.breached=true;
    var inventory=flow.player.inventory;
    originalInventory=inventory.data;originalCells=inventory.Count("torch_cell");originalAmmo=inventory.Count("556");
    originalRounds=flow.player.rounds;originalBattery=flow.player.battery;originalPersist=inventory.persist;
    Report.AppendLine("Initial room light count: "+(room.lights!=null?room.lights.Length:0));
    if(room.lights!=null)for(int i=0;i<room.lights.Length;i++){
     var light=room.lights[i];if(!light){Report.AppendLine("Initial room light "+i+": missing");continue;}
     Report.AppendLine("Initial room light "+i+": "+light.name+"; position="+V(light.transform.position)+"; type="+light.type+"; intensity="+N(light.intensity)+"; range="+N(light.range)+"; enabled="+light.enabled);
    }
    Time.captureDeltaTime=1f/Fps;
    encoder=new MediaEncoder(Path.Combine(Folder,"The Sickness - Cinematic Trailer.mp4"),
     new VideoTrackAttributes{width=Width,height=Height,frameRate=new MediaRational(Fps),includeAlpha=false,bitRateMode=VideoBitrateMode.High},
     new AudioTrackAttributes{sampleRate=new MediaRational(AudioSettings.outputSampleRate),channelCount=2,language="en"});
    videoTarget=new RenderTexture(Width,Height,24);
    videoFrame=new Texture2D(Width,Height,TextureFormat.RGBA32,false);
    AudioRenderer.Start();
    director=room.gameObject.AddComponent<MedicalTrailerSequence>();director.Configure(room);
    crawl=room.thornback.GetComponent<MedicalThornbackCrawl>();effects=crawl.GetComponent<MedicalThornbackEffects>();
    stage=2;return;
   }
   if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
   ObserveEvents();
   if(effects)ScrapeTrace.AppendLine(N(director.Age)+","+effects.ScrapeActive+","+N(effects.CurrentDragSpeed)+","+effects.CurrentDragContact+","+N(effects.CurrentScrapeGain)+","+N(effects.CurrentDragDistance)+","+Csv(effects.CurrentDragPoint));
   CaptureFrame(flow.player.eye);
   maxGrip=Mathf.Max(maxGrip,crawl.GripError);
   if(videoFrames<Fps*95&&(!flow.interior.finished||flow.interior.EndAge<Mathf.Max(18.3f,flow.interior.ThemeDuration+.65f)))return;
   CompleteReport();
  }catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"error.txt"),e.ToString());Finish(2);}
 }
 static void ObserveEvents(){
  Observe(0,director.ForwardDistance>.05f);Observe(1,director.BackpackChecked);
  Observe(2,director.WhiteTorchAttempted);Observe(3,director.TorchFailed);
  Observe(4,director.ShotCount>0);Observe(5,director.EmptyClickCount>0);
  Observe(6,director.RetreatDistance>.05f);Observe(7,effects&&effects.RoarPlayed);
  Observe(8,flow.interior.finished);Observe(9,flow.interior.TitleVisible);
  if(effects&&effects.RoarPlayed&&roarStartedAt<0)roarStartedAt=Time.time;
  if(flow.interior.finished&&blackStartedAt<0)blackStartedAt=Time.time;
  if(flow.interior.ThemePlaying)themeObserved=true;
 }
 static void Observe(int index,bool happened){if(happened&&EventAges[index]<0)EventAges[index]=director.Age;}
 static void CaptureFrame(Camera camera){
  var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
  var oldPosition=camera.transform.localPosition;var oldRotation=camera.transform.localRotation;float oldFov=camera.fieldOfView;
  var shake=camera.GetComponent<MedicalThornbackCameraImpulse>();
  bool duringRoar=effects&&effects.RoarPlayed&&!flow.interior.finished;
  float renderPosition=0,renderRotation=0,renderFov=0,projectionShift=0;
  Vector3 renderPose=oldPosition;
  try{
   camera.targetTexture=videoTarget;
   Vector3 landmark=flow.interior.jammedDoor.position+Vector3.up*1.4f;
   Vector3 baseProjection=camera.WorldToViewportPoint(landmark);
   // Camera.Render's standalone SRP path may omit begin/end callbacks. Own
   // this scope explicitly, before culling, and keep it through the render.
   if(shake)shake.BeginCapture();
   renderPose=camera.transform.localPosition;
   renderPosition=Vector3.Distance(oldPosition,renderPose);
   renderRotation=Quaternion.Angle(oldRotation,camera.transform.localRotation);
   renderFov=camera.fieldOfView-oldFov;
   Vector3 renderedProjection=camera.WorldToViewportPoint(landmark);
   projectionShift=new Vector2((renderedProjection.x-baseProjection.x)*Width,(renderedProjection.y-baseProjection.y)*Height).magnitude;
   if(duringRoar){
    roarRenderFrames++;if(renderPosition>.002f||renderRotation>.1f)roarMovedFrames++;
    renderedRoarPositionPeak=Mathf.Max(renderedRoarPositionPeak,renderPosition);
    renderedRoarRotationPeak=Mathf.Max(renderedRoarRotationPeak,renderRotation);
    renderedRoarFovPeak=Mathf.Max(renderedRoarFovPeak,Mathf.Abs(renderFov));
    roarProjectionPeak=Mathf.Max(roarProjectionPeak,projectionShift);
   }
   if(flow.interior.finished){blackCameraPeak=Mathf.Max(blackCameraPeak,renderPosition,renderRotation,Mathf.Abs(renderFov));if(shake&&shake.IsApplied)blackShakeFrames++;}
   camera.Render();
   RenderTexture.active=videoTarget;videoFrame.ReadPixels(new Rect(0,0,Width,Height),0,0);videoFrame.Apply();
   // Save adjacent illuminated onset frames: these make the doorway's actual
   // pixel displacement reviewable, rather than relying on impulse telemetry.
   if(duringRoar&&Time.time-effects.RoarStartedAt<.4f&&ImageMean(videoFrame)>.0015&&litRoarFrames<5){
    SaveFrame("08c-lit-roar-motion-"+litRoarFrames.ToString("00"));litRoarFrames++;
   }
   if(keyframeIndex<KeyTimes.Length&&director.Age>=KeyTimes[keyframeIndex]){SaveFrame(KeyNames[keyframeIndex]);keyframeIndex++;}
   if(crawl.Doorway&&crawl.Doorway.Active){
    if(!gripsSnap&&crawl.Doorway.Age>=.95f){gripsSnap=true;SaveFrame("08a-doorway-claws-grip");}
    if(!pullSnap&&crawl.Doorway.Age>=5.05f){pullSnap=true;SaveFrame("08b-doorway-head-pull");}
    if(!doorwaySnap&&crawl.Doorway.Age>=7.9f){doorwaySnap=true;SaveFrame("08-doorway-final-roar");}
   }
   if(flow.interior.finished){
    float age=flow.interior.EndAge;
    if(!blackSnap&&age>.08f&&!flow.interior.TitleVisible){blackSnap=true;blackMean=ImageMean(videoFrame);SaveFrame("09-ending-black-theme-starts");}
    if(!fadeSnap&&age>1.4f&&age<2){fadeSnap=true;fadeMean=ImageMean(videoFrame);SaveFrame("10-title-fades-from-black");}
    if(!titleSnap&&age>5.5f){
     titleSnap=true;titleMean=ImageMean(videoFrame);
     double red=0,other=0;foreach(var pixel in videoFrame.GetPixels32()){red+=pixel.r;other+=pixel.g+pixel.b;}
     titleRedRatio=red/Math.Max(1,other*.5);SaveFrame("11-red-title-coming-soon");
    }
    if(!inkMiddleSnap&&age>=11){inkMiddleSnap=true;inkMiddleLength=flow.interior.TitleInkLength;SaveFrame("12-title-ink-middle");}
    if(!inkEndSnap&&age>=flow.interior.ThemeDuration-.05f){inkEndSnap=true;inkEndLength=flow.interior.TitleInkLength;SaveFrame("13-title-ink-music-end");}
    if(age>.15f)foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
     if(source.isPlaying&&!source.mute&&source.volume>.001f&&source.name!=MedicalInterior.EndingThemeSourceName)endingOtherAudioFrames++;
   }
   if(!encoder.AddFrame(videoFrame))throw new Exception("Video encoder rejected a frame.");
  }finally{
   if(shake)shake.EndCapture();
   float restored=Mathf.Max(Vector3.Distance(oldPosition,camera.transform.localPosition),Quaternion.Angle(oldRotation,camera.transform.localRotation),Mathf.Abs(oldFov-camera.fieldOfView));
   cameraRestoreError=Mathf.Max(cameraRestoreError,restored);
   if(duringRoar)ShakeTrace.AppendLine(N(director.Age)+","+N(Time.time-effects.RoarStartedAt)+","+Csv(oldPosition)+","+Csv(renderPose)+","+N(renderRotation)+","+N(renderFov)+","+N(projectionShift)+","+N(restored));
   camera.targetTexture=oldTarget;RenderTexture.active=oldActive;
  }
  int count=AudioRenderer.GetSampleCountForCaptureFrame();
  if(count>0)using(var audio=new NativeArray<float>(count*2,Allocator.Temp)){
   if(AudioRenderer.Render(audio)){
    for(int i=0;i<audio.Length;i++){
     float sample=audio[i];peak=Mathf.Max(peak,Mathf.Abs(sample));
     if(director.Age>MedicalTrailerSequence.Prelude&&director.Age<MedicalTrailerSequence.Prelude+3.8f){lurePower+=sample*sample;lureSamples++;}
     if(effects&&effects.RoarPlayed&&!flow.interior.finished){roarPower+=sample*sample;roarSamples++;}
     if(flow.interior.finished){
      if(firstThemeEnergyAge<0&&Mathf.Abs(sample)>.002f)firstThemeEnergyAge=flow.interior.EndAge;
      themePower+=sample*sample;themeSamples++;
     }
    }
    encoder.AddSamples(audio);
   }
  }
  videoFrames++;
 }
 static void SaveFrame(string label){
  File.WriteAllBytes(Path.Combine(Folder,label+".png"),videoFrame.EncodeToPNG());
  Report.AppendLine("Keyframe "+label+": director="+N(director.Age)+"s, phase="+director.Phase+", recorded="+N(videoFrames/(float)Fps)+"s, doorway="+N(crawl.Doorway?crawl.Doorway.Age:-1)+"s");
 }
 static void CompleteReport(){
  var room=flow.interior;var mimic=crawl.Mimic;
  bool ordered=true;float previous=-1;
  for(int i=0;i<EventAges.Length;i++){
   Report.AppendLine(EventNames[i]+": "+N(EventAges[i])+"s");
   ordered&=EventAges[i]>=0&&EventAges[i]>=previous;previous=EventAges[i];
  }
  double lureRms=Math.Sqrt(lurePower/Math.Max(1,lureSamples)),roarRms=Math.Sqrt(roarPower/Math.Max(1,roarSamples)),themeRms=Math.Sqrt(themePower/Math.Max(1,themeSamples));
  Report.AppendLine("Event order correct: "+ordered);
  Report.AppendLine("Last director phase: "+director.Phase+"; director completed: "+director.Completed);
  Report.AppendLine("Actual shot count: "+director.ShotCount+"; empty-click count: "+director.EmptyClickCount+"; scene shot count: "+room.shotsDuringChase);
  Report.AppendLine("Forward distance: "+N(director.ForwardDistance)+"m; retreat distance: "+N(director.RetreatDistance)+"m");
  Report.AppendLine("Backpack checked: "+director.BackpackChecked+"; spare cells during check: "+director.SpareCellsDuringCheck);
  Report.AppendLine("Backpack unzip count: "+director.UnzipPlayCount+"; unzip at: "+N(director.UnzipAt)+"s; bag opened at: "+N(director.BackpackOpenedAt)+"s");
  Report.AppendLine("Panic voice count: "+director.PanicPlayCount+"; at: "+N(director.PanicAt)+"s; click at: "+N(director.ClickAt)+"s; duration: "+N(director.PanicDuration)+"s; completed: "+director.PanicFinished);
  var exile=director.CorridorExile;
  Report.AppendLine("NPC skinned meshes: "+exile.SkinnedRendererCount+"; visible frames: "+exile.VisibleFrames+"; flicker dips: "+exile.FlickerDipFrames+"; blacked out before disappearance: "+exile.DisappearedAfterLampBlackout+"; disappeared at: "+N(exile.VanishedAt));
  Report.AppendLine("Prelude NPC vanished: "+(exile&&exile.Vanished)+"; scream count: "+director.ScreamPlayCount+"; scream at: "+N(director.ScreamAt)+"; duration: "+N(director.ScreamDuration)+"; masked reaction count: "+director.ShockLinePlayCount+"; at: "+N(director.ShockLineAt)+"; duration: "+N(director.ShockLineDuration));
  Report.AppendLine("ADS shots: "+director.AdsShotCount+"; ADS frames: "+director.AdsFrames+"; second dry click: "+N(director.SecondClickAt)+"; ejected casings: "+director.EjectedCasings+"; audible casing impacts: "+director.CasingImpacts+"; peak ear ringing: "+N(director.PeakRinging));
  Report.AppendLine("Title ink strokes: "+room.TitleInkStreakCount+"; mid length: "+N(inkMiddleLength)+"; theme-end length: "+N(inkEndLength)+"; theme duration: "+N(room.ThemeDuration));
  Report.AppendLine("White torch attempted: "+director.WhiteTorchAttempted+"; white torch failed: "+director.TorchFailed);
  Report.AppendLine("Maximum muzzle light: "+N(director.MaxMuzzleLight)+"; failed white torch output: "+N(director.FailedTorchLight)+"; glass footstep count: "+director.FootstepCount);
  Report.AppendLine("Persistent faint red light: min="+N(director.RedLightMin)+"; max="+N(director.RedLightMax)+"; observed frames="+director.RedLightFrameCount+"; off frames before black cut="+director.RedLightOffFrames);
  Report.AppendLine("Original creature: "+crawl.visual.name+"; doorway complete: "+(crawl.Doorway&&crawl.Doorway.Complete)+"; max grip error: "+N(maxGrip)+"m");
  Report.AppendLine("Roar played: "+effects.RoarPlayed+"; roar duration: "+N(effects.RoarDuration)+"s; audible time before cut: "+N(blackStartedAt-roarStartedAt)+"s");
  Report.AppendLine("Voice initially hidden: "+mimic.VoiceStartedHidden+"; hidden renderer violations: "+mimic.HiddenRendererViolations+"; hidden collider violations: "+mimic.HiddenColliderViolations);
  Report.AppendLine("Distinct inhuman lure loaded: "+mimic.InhumanLureLoaded+"; inhuman reply loaded: "+mimic.InhumanReplyLoaded);
  Report.AppendLine("Post-shot reply count: "+mimic.PostShotPlayCount+"; reply complete: "+mimic.PostShotFinished);
  Report.AppendLine("Lure RMS (9-12.8s, before unzip): "+lureRms+"; roar RMS: "+roarRms+"; theme RMS: "+themeRms+"; full mix peak: "+N(peak));
  Report.AppendLine("Camera render restoration error: "+N(cameraRestoreError));
  Report.AppendLine("Rendered roar frames: "+roarRenderFrames+"; visibly displaced pose frames: "+roarMovedFrames+"; illuminated onset frames saved: "+litRoarFrames);
  Report.AppendLine("Actual rendered camera peaks: position="+N(renderedRoarPositionPeak)+"m; rotation="+N(renderedRoarRotationPeak)+"deg; FOV pressure="+N(renderedRoarFovPeak)+"deg; doorway projection shift="+N(roarProjectionPeak)+"px");
  Report.AppendLine("Black/title shake frames: "+blackShakeFrames+"; maximum camera shake after cut: "+N(blackCameraPeak));
  foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)){
   string name=source.name.ToLowerInvariant();if(name.Contains("mimic")||name.Contains("growl")||name.Contains("roar")||name.Contains("panic"))
    Report.AppendLine("Audio source: "+source.name+"; gain="+N(source.volume)+"; spatial="+N(source.spatialBlend)+"; clip="+(source.clip?source.clip.name:"none"));
  }
  Report.AppendLine("Black captured: "+blackSnap+"; mean: "+blackMean+"; fade captured: "+fadeSnap+"; mean: "+fadeMean+"; title captured: "+titleSnap+"; mean: "+titleMean+"; red ratio: "+titleRedRatio);
  Report.AppendLine("Theme observed: "+themeObserved+"; play count: "+room.ThemePlayCount+"; started at: "+N(room.ThemeStartedAt)+"s; first energy: "+N(firstThemeEnergyAge)+"s; other audible source frames: "+endingOtherAudioFrames);
  Report.AppendLine("Title alpha: "+N(room.TitleAlpha)+"; coming soon alpha: "+N(room.ComingSoonAlpha)+"; ending age: "+N(room.EndAge)+"s");
  Report.AppendLine("New IMG 8560 drag loaded: "+effects.SuppliedDragLoaded+"; pull starts: "+effects.ScrapePulls+"; scrape seconds: "+N(effects.ScrapingSeconds)+"; threshold floor drag seconds: "+N(effects.ThresholdDragSeconds)+"; silent rest frames: "+effects.ScrapeRestFrames+"; invalid scrape frames: "+effects.InvalidScrapeFrames);
  Report.AppendLine("Runtime error count: "+runtimeErrors);
  bool passed=effects.SuppliedDragLoaded&&effects.ScrapePulls>=2&&effects.ScrapingSeconds>2&&effects.ScrapeRestFrames>30&&effects.InvalidScrapeFrames==0&&effects.ThresholdDragSeconds>1&&runtimeErrors==0&&ordered&&director.Completed&&director.ShotCount==5&&director.EmptyClickCount==2&&room.shotsDuringChase==5
   &&director.ForwardDistance>1&&director.RetreatDistance>1&&director.BackpackChecked&&director.SpareCellsDuringCheck==0
   &&director.UnzipPlayCount==1&&director.UnzipAt>=0&&director.BackpackOpenedAt>director.UnzipAt+.4f
   &&director.PanicPlayCount==1&&director.PanicAt>director.SecondClickAt+.15f&&director.PanicFinished&&director.PanicDuration>0
   &&exile&&exile.Vanished&&exile.WasVisible&&exile.VisibleFrames>30&&exile.DisappearedAfterLampBlackout&&exile.FlickerDipFrames>0&&director.ScreamPlayCount==1&&director.ScreamAt>=3.4f&&director.ShockLinePlayCount==1&&director.ShockLineAt>=director.ScreamAt+director.ScreamDuration&&director.ShockLineFinished
   &&director.AdsShotCount==4&&director.AdsFrames>40&&director.EjectedCasings==5&&director.CasingImpacts==5&&director.PeakRinging>.4f
   &&inkMiddleSnap&&inkEndSnap&&room.TitleInkStreakCount>15&&inkEndLength>inkMiddleLength+30&&room.EndAge>room.ThemeDuration
   &&director.WhiteTorchAttempted&&director.TorchFailed&&director.MaxMuzzleLight>0&&director.FailedTorchLight==0&&keyframeIndex==KeyTimes.Length&&doorwaySnap&&gripsSnap&&pullSnap
   &&director.RedLightFrameCount>800&&director.RedLightOffFrames==0&&director.RedLightMin>0&&director.RedLightMax>director.RedLightMin*1.3f&&director.RedLightMax<5
   &&maxGrip<.03f&&crawl.Doorway&&crawl.Doorway.Complete&&effects.RoarPlayed&&effects.RoarDuration>=6.5f
   &&blackStartedAt-roarStartedAt>=effects.RoarDuration-.10f&&mimic.VoiceStartedHidden&&mimic.HiddenRendererViolations==0&&mimic.HiddenColliderViolations==0
   &&mimic.InhumanLureLoaded&&mimic.InhumanReplyLoaded&&mimic.PostShotPlayCount==1&&mimic.PostShotFinished
   &&roarRms>lureRms*2&&peak<.99f&&cameraRestoreError<.0001f
   &&roarRenderFrames>100&&roarMovedFrames>roarRenderFrames*.7f&&renderedRoarPositionPeak>.025f&&renderedRoarRotationPeak>1.5f&&renderedRoarFovPeak>1.5f&&roarProjectionPeak>10&&litRoarFrames>=2
   &&blackShakeFrames==0&&blackCameraPeak<.0001f
   &&blackSnap&&blackMean<.001&&fadeSnap&&fadeMean>0&&fadeMean<titleMean&&titleSnap&&titleMean>.002&&titleRedRatio>4
   &&themeObserved&&room.ThemePlayCount==1&&room.ThemeStartedAt<.05f&&firstThemeEnergyAge>=0&&firstThemeEnergyAge<.2f&&themeRms>.02&&endingOtherAudioFrames==0
   &&room.TitleAlpha>.99f&&room.ComingSoonAlpha>.99f&&room.EndAge>=18.3f;
  // Cleanup is deliberately after the last encoded frame. Verify the temporary
  // empty backpack and last round did not replace the player's original data.
  Object.DestroyImmediate(director);
  var inventory=flow.player.inventory;
  bool restored=object.ReferenceEquals(inventory.data,originalInventory)&&inventory.Count("torch_cell")==originalCells&&inventory.Count("556")==originalAmmo
   &&flow.player.rounds==originalRounds&&Mathf.Abs(flow.player.battery-originalBattery)<.001f&&inventory.persist==originalPersist;
  Report.AppendLine("Original inventory restored after trailer cleanup: "+restored+"; cells="+inventory.Count("torch_cell")+"/"+originalCells+"; spare rounds="+inventory.Count("556")+"/"+originalAmmo+"; loaded rounds="+flow.player.rounds+"/"+originalRounds+"; battery="+N(flow.player.battery)+"/"+N(originalBattery)+"; persistence="+inventory.persist+"/"+originalPersist);
  passed&=restored;
  Report.AppendLine(passed?"MECHANICAL CHECK PASSED; inspect the recorded frames and audio before visual/audio signoff.":"MECHANICAL CHECK FAILED; inspect event and measurement evidence above.");
  File.WriteAllText(Path.Combine(Folder,"report.txt"),Report.ToString());File.WriteAllText(Path.Combine(Folder,"roar-camera-render-trace.csv"),ShakeTrace.ToString());File.WriteAllText(Path.Combine(Folder,"scrape-motion-trace.csv"),ScrapeTrace.ToString());Finish(passed?0:2);
 }
 static string N(float value)=>value.ToString("0.0000",CultureInfo.InvariantCulture);
 static string Csv(Vector3 value)=>N(value.x)+","+N(value.y)+","+N(value.z);
 static string V(Vector3 value)=>"("+N(value.x)+", "+N(value.y)+", "+N(value.z)+")";
 static double ImageMean(Texture2D image){double total=0;var pixels=image.GetPixels32();foreach(var pixel in pixels)total+=pixel.r+pixel.g+pixel.b;return total/(pixels.Length*3.0*255);}
 static void Finish(int code){
  if(encoder!=null){AudioRenderer.Stop();encoder.Dispose();encoder=null;}
  Time.captureDeltaTime=0;
  if(videoTarget)Object.DestroyImmediate(videoTarget);if(videoFrame)Object.DestroyImmediate(videoFrame);
  Directory.CreateDirectory(Folder);File.AppendAllText(Path.Combine(Folder,"report.txt"),"\nVideo frames: "+videoFrames+"\nDuration seconds: "+N(videoFrames/(float)Fps)+"\nAudio peak: "+N(peak)+"\n");
  Application.logMessageReceived-=LogMessage;if(runtimeErrors>0)File.WriteAllText(Path.Combine(Folder,"runtime-errors.txt"),RuntimeLog.ToString());
  EditorApplication.update-=Tick;SessionState.SetBool(SessionKey,false);EditorApplication.Exit(code);
 }
}
