using UnityEngine;
using System.Collections.Generic;

// Opt-in trailer performance. It is never attached by the normal playable demo.
// The real inventory, rifle shot, empty click, creature and ending are retained.
[DefaultExecutionOrder(1500)]
public class MedicalTrailerSequence : MonoBehaviour {
 public const float Prelude=9f,PackTime=Prelude+4.6f,WhiteTorchTime=Prelude+10.7f,FailureTime=Prelude+13f,ShotTime=Prelude+17f,ClickTime=Prelude+20.1f;
 public static readonly float[] ShotTimes={ShotTime,Prelude+18f,Prelude+18.5f,Prelude+19.05f,Prelude+19.55f};
 public const float SecondClickTime=Prelude+20.65f,RetreatTime=Prelude+21.3f;
 public float EncounterAge=>Mathf.Max(0,Age-Prelude);
 public MedicalTrailerCorridorExile CorridorExile{get;private set;}
 public int ScreamPlayCount{get;private set;}public float ScreamAt{get;private set;}=-1;
 public int ShockLinePlayCount{get;private set;}public float ShockLineAt{get;private set;}=-1;
 public float ShockLineDuration=>shockVoice&&shockVoice.clip?shockVoice.clip.length:0;
 public float ScreamDuration=>npcScream&&npcScream.clip?npcScream.clip.length:0;
 public bool ShockLineFinished=>ShockLinePlayCount==1&&Age>ShockLineAt+ShockLineDuration;
 public float SecondClickAt{get;private set;}=-1;
 public int AdsShotCount{get;private set;}public int AdsFrames{get;private set;}
 public float PeakRinging{get;private set;}
 public int EjectedCasings=>p&&p.combatPresentation?p.combatPresentation.EjectedCount-initialCasings:0;
 public int CasingImpacts=>p&&p.combatPresentation?p.combatPresentation.CasingImpactCount-initialImpacts:0;
 public float Age{get;private set;}
 public string Phase{get;private set;}="Listening";
 public int ShotCount{get;private set;}public int EmptyClickCount{get;private set;}
 public float ForwardDistance{get;private set;}public float RetreatDistance{get;private set;}
 public bool WhiteTorchAttempted{get;private set;}public bool TorchFailed{get;private set;}
 public bool BackpackChecked{get;private set;}public int SpareCellsDuringCheck{get;private set;}=-1;
 public int PanicPlayCount{get;private set;}public float PanicAt{get;private set;}=-1;
 public float PanicDuration=>panicVoice&&panicVoice.clip?panicVoice.clip.length:0;
 public bool PanicFinished=>PanicPlayCount==1&&Age>=PanicAt+PanicDuration;
 public int UnzipPlayCount{get;private set;}public float UnzipAt{get;private set;}=-1;
 public float BackpackOpenedAt{get;private set;}=-1;
 public float ShotAt{get;private set;}=-1;public float ClickAt{get;private set;}=-1;
 public float MaxMuzzleLight{get;private set;}public float FailedTorchLight{get;private set;}
 public float RedLightMin{get;private set;}=float.MaxValue;public float RedLightMax{get;private set;}
 public int RedLightFrameCount{get;private set;}public int RedLightOffFrames{get;private set;}
 public int FootstepCount{get;private set;}public bool Completed=>room&&room.finished;
 MedicalInterior room;WoodlandPlayer p;MedicalThornbackCrawl crawl;MedicalTrailerPackInspection inspection;
 MedicalLowBattery lowBattery;WoodlandDemoControls controls;CharacterController body;
 bool configured,stopped,packStarted;int nextShot,nextClick,initialCasings,initialImpacts;bool visorWasEnabled;float originalExposure,originalFog;
 bool playerWasEnabled,controlsWereEnabled,lowWasEnabled,wasLocked,oldWhite,oldTorchEnabled,oldPersist;
 public bool ControlsOriginallyEnabled=>controlsWereEnabled;
 float oldBattery,oldTorchIntensity,oldTorchRange,oldFlashIntensity,oldFlashRange;int oldRounds;
 Vector3 oldEye;Quaternion oldEyeRotation;float oldFov;
 Transform torchHand,torchSleeve;AudioSource movementAudio,panicVoice,packFoley,shockVoice,npcScream;Light shotBounce;AudioClip glass;
 readonly List<Canvas> hiddenCanvases=new List<Canvas>();
 float forwardApplied,retreatApplied,stepTravel,shotAge=-1;
 Light viewFlash;MaterialPropertyBlock preludeLamps;
 Light thresholdLamp,whiteAttempt,whiteAttemptSpill;GameObject thresholdFixture;Material fixtureMaterial;Renderer torchHandRenderer;MeshFilter torchHandMesh;Vector3[] torchHandVertices;

 public void Configure(MedicalInterior interior){
  if(configured)return;configured=true;preludeLamps=new MaterialPropertyBlock();room=interior;p=room.owner.player;body=p.GetComponent<CharacterController>();
  oldPersist=p.inventory.persist;p.inventory.persist=false;playerWasEnabled=p.enabled;wasLocked=p.cinematicLocked;oldRounds=p.rounds;oldBattery=p.battery;oldWhite=p.whiteHeadlight;
  oldTorchEnabled=p.torch.enabled;oldTorchIntensity=p.torch.intensity;oldTorchRange=p.torch.range;
  oldEye=p.eye.transform.localPosition;oldEyeRotation=p.eye.transform.localRotation;oldFov=p.eye.fieldOfView;
  oldFlashIntensity=p.muzzleFlash.intensity;oldFlashRange=p.muzzleFlash.range;
  controls=Object.FindFirstObjectByType<WoodlandDemoControls>();if(controls){controlsWereEnabled=controls.enabled;controls.enabled=false;}
  lowBattery=p.GetComponent<MedicalLowBattery>();if(lowBattery){lowWasEnabled=lowBattery.enabled;lowBattery.enabled=false;}
  foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.enabled){hiddenCanvases.Add(canvas);canvas.enabled=false;}
  p.enabled=false;p.cinematicLocked=true;p.rounds=ShotTimes.Length;p.battery=3;p.whiteHeadlight=false;p.ApplyTorchMode();p.torch.enabled=true;
  Cursor.visible=false;Cursor.lockState=CursorLockMode.Locked;
  var sound=new GameObject("Trailer boots on broken glass");sound.transform.SetParent(p.transform,false);movementAudio=sound.AddComponent<AudioSource>();movementAudio.playOnAwake=false;
  glass=Resources.Load<AudioClip>("Medical/ThornbackFX/glass-step");MedicalCorridorAcoustics.Setup(movementAudio,p.eye.transform);movementAudio.volume=.29f;
  var bounce=new GameObject("Trailer muzzle reflected light");bounce.transform.SetParent(p.transform,false);shotBounce=bounce.AddComponent<Light>();shotBounce.type=LightType.Point;shotBounce.color=new Color(1,.66f,.34f);shotBounce.range=13;shotBounce.shadows=LightShadows.Hard;shotBounce.enabled=false;
  // Directional flash lights the hall from the muzzle; a weak nearby bounce
  // reaches the rifle without exposing its close-up surfaces like a floodlight.
  shotBounce.type=LightType.Spot;shotBounce.spotAngle=150;shotBounce.innerSpotAngle=85;
  var reflection=new GameObject("Rifle reflected muzzle flash");reflection.transform.SetParent(p.eye.transform,false);reflection.transform.localPosition=new Vector3(.12f,.1f,.85f);viewFlash=reflection.AddComponent<Light>();viewFlash.type=LightType.Point;viewFlash.color=shotBounce.color;viewFlash.range=1.1f;viewFlash.shadows=LightShadows.None;viewFlash.enabled=false;
  var dialogue=new GameObject("Player empty rifle whisper");dialogue.transform.SetParent(p.eye.transform,false);panicVoice=dialogue.AddComponent<AudioSource>();panicVoice.playOnAwake=false;panicVoice.spatialBlend=0;panicVoice.priority=8;panicVoice.volume=.68f;
  panicVoice.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/player-masked-empty-panic");if(panicVoice.clip)panicVoice.clip.LoadAudioData();
  var voiceLow=dialogue.AddComponent<AudioLowPassFilter>();voiceLow.cutoffFrequency=4100;
  var voiceRoom=dialogue.AddComponent<AudioReverbFilter>();voiceRoom.reverbPreset=AudioReverbPreset.User;voiceRoom.dryLevel=-100;voiceRoom.room=-3000;voiceRoom.roomHF=-3000;voiceRoom.decayTime=.28f;voiceRoom.decayHFRatio=.45f;voiceRoom.reflectionsLevel=-2200;voiceRoom.reflectionsDelay=.035f;voiceRoom.reverbLevel=-3200;voiceRoom.reverbDelay=.065f;
  var zip=new GameObject("Backpack unzip before opening");zip.transform.SetParent(p.transform,false);packFoley=zip.AddComponent<AudioSource>();packFoley.playOnAwake=false;packFoley.spatialBlend=0;packFoley.volume=.38f;packFoley.priority=30;packFoley.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/backpack-unzip");if(packFoley.clip)packFoley.clip.LoadAudioData();
  // Separate LED channels: the weak red emitter remains alive while the
  // high-draw white channel sputters and fails during the attempted switch.
  var white=new GameObject("Trailer failing white headtorch channel");white.transform.SetParent(p.torch.transform,false);whiteAttempt=white.AddComponent<Light>();whiteAttempt.type=LightType.Spot;whiteAttempt.spotAngle=p.torch.spotAngle;whiteAttempt.innerSpotAngle=p.torch.innerSpotAngle;whiteAttempt.cookie=p.torch.cookie;whiteAttempt.cullingMask=p.torch.cullingMask;whiteAttempt.shadows=p.torch.shadows;whiteAttempt.color=new Color(1,.95f,.85f);whiteAttempt.range=30;whiteAttempt.enabled=false;
  var whiteFlood=new GameObject("Trailer white reflector spill");whiteFlood.transform.SetParent(white.transform,false);whiteAttemptSpill=whiteFlood.AddComponent<Light>();whiteAttemptSpill.type=LightType.Spot;whiteAttemptSpill.spotAngle=95;whiteAttemptSpill.innerSpotAngle=15;whiteAttemptSpill.color=whiteAttempt.color;whiteAttemptSpill.range=8;whiteAttemptSpill.enabled=false;
  var shock=new GameObject("Player inside-mask shocked whisper");shock.transform.SetParent(p.eye.transform,false);shockVoice=shock.AddComponent<AudioSource>();shockVoice.playOnAwake=false;shockVoice.spatialBlend=0;shockVoice.priority=8;shockVoice.volume=.74f;shockVoice.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/player-masked-oh-fuck");if(shockVoice.clip)shockVoice.clip.LoadAudioData();shock.AddComponent<AudioLowPassFilter>().cutoffFrequency=4100;
  var scream=new GameObject("Vanished corridor exile scream");scream.transform.SetParent(room.transform,false);scream.transform.position=new Vector3(room.transform.position.x,room.transform.position.y+1.6f,room.jammedDoor.position.z-.4f);npcScream=scream.AddComponent<AudioSource>();npcScream.playOnAwake=false;npcScream.priority=12;npcScream.volume=.8f;npcScream.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/npc-disappear-scream");if(npcScream.clip)npcScream.clip.LoadAudioData();MedicalCorridorAcoustics.Setup(npcScream,p.eye.transform);npcScream.minDistance=4;npcScream.maxDistance=26;
  var screamReverb=npcScream.GetComponent<AudioReverbFilter>();screamReverb.decayTime=3.7f;screamReverb.dryLevel=-300;screamReverb.room=-900;screamReverb.reverbLevel=-500;
  if(p.visorEffects){visorWasEnabled=p.visorEffects.enabled;originalExposure=p.visorEffects.exposure;originalFog=p.visorEffects.fog;p.visorEffects.enabled=false;}
  if(p.combatPresentation){initialCasings=p.combatPresentation.EjectedCount;initialImpacts=p.combatPresentation.CasingImpactCount;}
  MakeThresholdFixture();
  room.BeginChase();room.chaseTime=-Prelude;crawl=room.thornback.GetComponent<MedicalThornbackCrawl>();
  CorridorExile=gameObject.AddComponent<MedicalTrailerCorridorExile>();CorridorExile.Configure(room,p);
 }
 void Update(){
  if(!configured||stopped||!room||room.finished)return;
  float dt=Time.deltaTime;Age+=dt;p.cinematicLocked=true;float scene=Age-Prelude;
  if(CorridorExile)CorridorExile.Tick(Age);
  if(Age>=3.4f&&ScreamPlayCount==0&&npcScream&&npcScream.clip){npcScream.Play();ScreamAt=Age;ScreamPlayCount++;}
  if(ScreamPlayCount==1&&Age>=Mathf.Max(6.1f,ScreamAt+ScreamDuration+.2f)&&ShockLinePlayCount==0&&shockVoice&&shockVoice.clip){shockVoice.Play();ShockLineAt=Age;ShockLinePlayCount++;}
  if(p.visorEffects){p.visorEffects.Advance(dt,false);PeakRinging=Mathf.Max(PeakRinging,p.visorEffects.RingIntensity);}
  if(crawl.Doorway)crawl.Doorway.PlayerMotionEnabled=false;
  float forward=1.75f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,4.15f,scene));
  if(forward>forwardApplied){float moved=Move(p.transform.forward*(forward-forwardApplied));ForwardDistance+=moved;forwardApplied=forward;}
  if(Age>=PackTime-.60f&&UnzipPlayCount==0&&packFoley&&packFoley.clip){packFoley.Play();UnzipPlayCount++;UnzipAt=Age;}
  if(Age>=PackTime&&!packStarted){packStarted=true;inspection=gameObject.AddComponent<MedicalTrailerPackInspection>();if(inspection.Begin(p))BackpackOpenedAt=Age;}
  if(inspection){inspection.Tick(Age-PackTime);if(inspection.Opened){BackpackChecked=true;SpareCellsDuringCheck=p.inventory.Count("torch_cell");}}
  if(Age>=WhiteTorchTime&&!WhiteTorchAttempted){WhiteTorchAttempted=true;}
  if(Age>=FailureTime){TorchFailed=true;p.battery=0;}
  if(nextShot<ShotTimes.Length&&Age>=ShotTimes[nextShot]){
   p.cinematicLocked=false;p.AlignMuzzle();bool fired=p.Fire();p.cinematicLocked=true;
   if(fired){ShotCount++;if(p.adsRifle&&p.adsRifle.gameObject.activeSelf)AdsShotCount++;if(ShotAt<0)ShotAt=Age;shotAge=Age;nextShot++;}
  }
  float clickDue=nextClick==0?ClickTime:SecondClickTime;
  if(nextClick<2&&Age>=clickDue){
   p.cinematicLocked=false;int rounds=p.rounds;bool fired=p.Fire();p.cinematicLocked=true;
   if(!fired&&rounds==0&&p.emptyClick){EmptyClickCount++;if(ClickAt<0)ClickAt=Age;else SecondClickAt=Age;nextClick++;}
  }
  if(EmptyClickCount==2&&Age>=SecondClickAt+.24f&&PanicPlayCount==0&&panicVoice&&panicVoice.clip){panicVoice.Play();PanicAt=Age;PanicPlayCount++;}
  if(crawl.Mimic){
   float duck=1;
   if(PanicPlayCount>0&&!PanicFinished){float elapsed=Age-PanicAt;float inOut=Mathf.SmoothStep(0,1,elapsed/.08f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(PanicDuration-.22f,PanicDuration,elapsed)));duck=Mathf.Lerp(1,.62f,inOut);}
   crawl.Mimic.PlayerDialogueGain=duck;
  }
  float retreat=1.15f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(RetreatTime,RetreatTime+4.8f,Age));
  if(crawl.Doorway&&crawl.Doorway.Active)retreat+=.9f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,7.4f,crawl.Doorway.Age));
  if(retreat>retreatApplied){float moved=Move(-p.transform.forward*(retreat-retreatApplied));RetreatDistance+=moved;retreatApplied=retreat;}
  Phase=Age<3.1f?"A light ahead":Age<6.1f?"The exile vanishes":Age<Prelude?"Inside-mask reaction":Age<PackTime?"Following the distant mimic":Age<Prelude+10.4f?"Searching the empty battery pocket":Age<FailureTime?"Trying the white headtorch":Age<ShotTime?"White light failed — faint red flicker":Age<ShotTime+.35f?"Hip-fired warning shot":Age<ClickTime?"Aimed rifle shots":Age<RetreatTime?"Empty click, click — panic":crawl.Doorway&&crawl.Doorway.Active?"Backing away from the doorway":"Retreating";
 }
 float Move(Vector3 delta){
  Vector3 before=p.transform.position;
  if(body&&body.enabled)body.Move(delta+Vector3.down*.02f);else p.transform.position+=delta;
  Vector3 change=p.transform.position-before;change.y=0;float length=change.magnitude;stepTravel+=length;
  if(stepTravel>.48f){stepTravel-=.48f;FootstepCount++;movementAudio.transform.position=p.transform.position+Vector3.up*.08f;if(glass){movementAudio.pitch=1;movementAudio.PlayOneShot(glass);}}
  return length;
 }
 void LateUpdate(){
  if(!configured||stopped||!p||!room)return;
  if(room.finished){Phase="Theme and red title";foreach(var lamp in p.torch.GetComponentsInChildren<Light>())lamp.enabled=false;if(shotBounce)shotBounce.enabled=false;if(viewFlash)viewFlash.enabled=false;if(thresholdLamp)thresholdLamp.enabled=false;HideHands();return;}
  float scene=Age-Prelude;bool inBag=p.fieldKit&&p.fieldKit.Busy;
  if(!inBag){
   float walking=scene>.65f&&scene<4.15f||Age>RetreatTime&&scene<34?1:0;
   float bob=Mathf.Sin((ForwardDistance+RetreatDistance)*Mathf.PI/.48f)*.012f*walking;
   float aimHesitation=scene>13&&scene<17?Mathf.Sin((scene-13)*1.1f)*.8f:0;
   p.eye.transform.localPosition=oldEye+Vector3.up*bob;
   p.eye.transform.localRotation=oldEyeRotation*Quaternion.Euler(aimHesitation,Mathf.Sin(Age*.9f)*.22f,Mathf.Sin(Age*1.3f)*.16f);
   bool ads=Age>ShotTime+.35f&&Age<SecondClickTime+.3f;p.AdvanceCinematicPose(Time.deltaTime,walking,ads);if(p.adsRifle&&p.adsRifle.gameObject.activeSelf)AdsFrames++;
   float lower=scene<4.2f?.05f:0;p.rifle.localPosition+=Vector3.down*lower;
  }
  // Irregular low-battery flicker, always leaving a faint red baseline.
  // It continues through the bag, empty click, retreat and final roar.
  float grain=Mathf.PerlinNoise(Age*1.75f,4.31f);
  float flutter=.84f+.10f*Mathf.Sin(Age*37)+.06f*Mathf.Sin(Age*23.7f);
  float contact=Mathf.Repeat(Age+.19f,3.4f);
  float dip=contact<.12f?.28f:contact<.20f?.60f:1;
  float hiddenBeat=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.82f,2.94f,Age))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.35f,3.65f,Age)));
  dip*=Mathf.Lerp(1,.035f,hiddenBeat);
  float redBase=inBag?.36f:TorchFailed?.32f:.44f;
  p.whiteHeadlight=false;p.torch.color=new Color(1,.06f,.025f);p.torch.range=TorchFailed?14:17;
  p.torch.intensity=WoodlandPlayer.RedTorchPower*redBase*Mathf.Lerp(.65f,1.05f,grain)*flutter*dip;p.torch.enabled=true;
  RedLightFrameCount++;RedLightMin=Mathf.Min(RedLightMin,p.torch.intensity);RedLightMax=Mathf.Max(RedLightMax,p.torch.intensity);if(!p.torch.enabled||p.torch.intensity<=0)RedLightOffFrames++;
  foreach(var spill in p.torch.GetComponentsInChildren<Light>())if(spill!=p.torch&&spill!=whiteAttempt&&spill!=whiteAttemptSpill){spill.enabled=true;spill.color=p.torch.color;spill.intensity=p.torch.intensity*.12f;spill.range=Mathf.Min(8,p.torch.range);}
  float aWhite=Age-WhiteTorchTime;
  float whiteBeam=aWhite<0||TorchFailed?0:aWhite<.16f?0:aWhite<.70f?1:aWhite<.83f?.02f:aWhite<.96f?.75f:aWhite<1.28f?0:aWhite<1.39f?.45f:aWhite<1.65f?.02f:aWhite<1.76f?.18f:0;
  whiteAttempt.enabled=whiteBeam>0;whiteAttempt.intensity=WoodlandPlayer.WhiteTorchPower*whiteBeam;whiteAttemptSpill.enabled=whiteAttempt.enabled;whiteAttemptSpill.intensity=whiteAttempt.intensity*.12f;
  if(TorchFailed)FailedTorchLight=Mathf.Max(FailedTorchLight,whiteAttempt.intensity);
  float lift=Mathf.SmoothStep(0,1,Mathf.InverseLerp(10.2f,10.85f,scene))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,12.8f,scene)));
  if(lift>0&&!inBag)AdjustTorch(lift);else HideHands();
  if(shotAge>=0){
   float a=Age-shotAge;bool burst=a>=0&&a<.10f;
   p.AlignMuzzle();p.muzzleFlash.enabled=false;p.muzzleFlash.intensity=32*(1-Mathf.Clamp01(a/.11f));p.muzzleFlash.range=12;
   if(p.muzzle)p.muzzle.gameObject.SetActive(a>=0&&a<.07f);
   shotBounce.enabled=a>=0&&a<.25f;shotBounce.transform.position=p.muzzle?p.muzzle.position:p.eye.transform.position;shotBounce.transform.rotation=p.eye.transform.rotation;
   viewFlash.enabled=burst;viewFlash.intensity=.18f*(1-Mathf.Clamp01(a/.11f));
   shotBounce.intensity=32*Mathf.Exp(-Mathf.Max(0,a)*17);MaxMuzzleLight=Mathf.Max(MaxMuzzleLight,burst?shotBounce.intensity:0);
  }
  // A visible strip on the player side of the lintel gives brief, motivated
  // glimpses of the claws/head above the weak red headtorch flicker.
  float fixture=0;
  if(crawl.Doorway&&crawl.Doorway.Active){
   float a=crawl.Doorway.Age;
   bool blink=(a>.2f&&a<.4f)||(a>.8f&&a<1.1f)||(a>4.7f&&a<5.25f)||(a>5.5f&&a<5.65f)||(a>6.5f&&a<6.9f)||(a>7.6f&&a<8.15f)||(a>9.2f&&a<9.4f)||(a>10.4f&&a<10.5f);
   if(blink)fixture=.8f+.2f*Mathf.Sin(a*101);
  }
  if(Age<Prelude){
   bool pulse=(Age>.35f&&Age<.48f)||(Age>1.55f&&Age<1.7f)||(Age>2.4f&&Age<2.48f)||(Age>2.7f&&Age<2.8f)||(Age>4.4f&&Age<4.52f)||(Age>7.2f&&Age<7.32f);
   fixture=pulse?.85f:0;
   // The disappearance happens with the fixtures out and red LED deeply dipped; the next flash reveals an empty corridor.
   if(room.lights!=null)foreach(var lamp in room.lights)if(lamp&&lamp.gameObject.activeInHierarchy){lamp.enabled=pulse&&Vector3.Distance(lamp.transform.position,p.transform.position)<8;lamp.intensity=pulse?2.3f:0;}
   if(room.lamps!=null)foreach(var surface in room.lamps)if(surface){surface.GetPropertyBlock(preludeLamps);preludeLamps.SetColor("_EmissionColor",new Color(.4f,.6f,.48f)*(pulse?1.3f:0));surface.SetPropertyBlock(preludeLamps);}
  }
  if(thresholdLamp){thresholdLamp.enabled=fixture>0;thresholdLamp.intensity=6.5f*fixture;}
  if(fixtureMaterial)fixtureMaterial.SetColor("_EmissionColor",new Color(.56f,.70f,.64f)*fixture*2);
 }
 void MakeThresholdFixture(){
  thresholdFixture=GameObject.CreatePrimitive(PrimitiveType.Cube);thresholdFixture.name="Failing threshold emergency strip";thresholdFixture.transform.SetParent(room.transform,true);
  thresholdFixture.transform.position=new Vector3(room.transform.position.x,room.transform.position.y+2.65f,room.jammedDoor.position.z+1.8f);thresholdFixture.transform.localScale=new Vector3(.58f,.035f,.16f);Destroy(thresholdFixture.GetComponent<Collider>());
  fixtureMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));fixtureMaterial.SetColor("_BaseColor",new Color(.14f,.16f,.15f));fixtureMaterial.EnableKeyword("_EMISSION");thresholdFixture.GetComponent<Renderer>().sharedMaterial=fixtureMaterial;
  var lamp=new GameObject("Intermittent threshold strip light");lamp.transform.SetParent(thresholdFixture.transform,false);lamp.transform.position=thresholdFixture.transform.position-Vector3.up*.18f;thresholdLamp=lamp.AddComponent<Light>();thresholdLamp.type=LightType.Point;thresholdLamp.color=new Color(.66f,.79f,.72f);thresholdLamp.range=8;thresholdLamp.shadows=LightShadows.Soft;thresholdLamp.enabled=false;
 }
 void AdjustTorch(float lift){
  if(!torchHand&&p.visorEffects&&p.visorEffects.wipeHand){
   torchHand=Instantiate(p.visorEffects.wipeHand,p.eye.transform,false);torchHand.name="Trailer headtorch check glove";torchHand.localScale*=.78f;torchHandRenderer=torchHand.GetComponentInChildren<Renderer>();torchHandMesh=torchHand.GetComponentInChildren<MeshFilter>();if(torchHandMesh&&torchHandMesh.sharedMesh&&torchHandMesh.sharedMesh.isReadable)torchHandVertices=torchHandMesh.sharedMesh.vertices;
   if(p.visorEffects.wipeSleeve){torchSleeve=Instantiate(p.visorEffects.wipeSleeve,p.eye.transform,false);torchSleeve.name="Trailer headtorch check sleeve";}
  }
  if(!torchHand)return;torchHand.gameObject.SetActive(true);torchHand.localRotation=Quaternion.Euler(-65,180,25+Mathf.Sin(Age*15)*2*lift);
  Vector3 center=Vector3.Lerp(new Vector3(-.29f,-.40f,.35f),new Vector3(-.22f,.15f,.30f),lift);
  var mesh=torchHandRenderer;
  torchHand.position+=p.eye.transform.TransformPoint(center)-mesh.bounds.center;
  if(torchSleeve){
   torchSleeve.gameObject.SetActive(true);
   // The wrist is the actual lower end of the posed mesh, not its FBX origin.
   Vector3 wristWorld=mesh.bounds.center-Vector3.up*(mesh.bounds.extents.y-.035f);
   var filter=torchHandMesh;
   if(filter&&torchHandVertices!=null){
    float min=float.MaxValue;Vector3 sum=Vector3.zero;int count=0;
    foreach(var v in torchHandVertices)min=Mathf.Min(min,p.eye.transform.InverseTransformPoint(filter.transform.TransformPoint(v)).y);
    foreach(var v in torchHandVertices){var point=p.eye.transform.InverseTransformPoint(filter.transform.TransformPoint(v));if(point.y<min+.015f){sum+=point;count++;}}
    if(count>0)wristWorld=p.eye.transform.TransformPoint(sum/count+Vector3.up*.035f);
   }
   Vector3 wrist=p.eye.transform.InverseTransformPoint(wristWorld),elbow=new Vector3(-.34f,-.40f,.12f);
   torchSleeve.localPosition=(wrist+elbow)*.5f;torchSleeve.localRotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);torchSleeve.localScale=new Vector3(.067f,Vector3.Distance(wrist,elbow)*.5f+.015f,.067f);
  }
  p.rifle.localPosition+=Vector3.down*.30f*lift;p.eye.transform.localRotation*=Quaternion.Euler(2*lift,0,-lift*.8f);
 }
 void HideHands(){if(torchHand)torchHand.gameObject.SetActive(false);if(torchSleeve)torchSleeve.gameObject.SetActive(false);}
 void OnDestroy(){
  if(!configured||stopped)return;stopped=true;if(inspection)inspection.Dispose();
  if(p){p.inventory.persist=oldPersist;p.enabled=playerWasEnabled;p.cinematicLocked=wasLocked;p.rounds=oldRounds;p.battery=oldBattery;p.whiteHeadlight=oldWhite;p.ApplyTorchMode();p.torch.enabled=oldTorchEnabled;p.torch.intensity=oldTorchIntensity;p.torch.range=oldTorchRange;p.muzzleFlash.intensity=oldFlashIntensity;p.muzzleFlash.range=oldFlashRange;p.muzzleFlash.enabled=false;p.eye.transform.localPosition=oldEye;p.eye.transform.localRotation=oldEyeRotation;p.eye.fieldOfView=oldFov;if(p.visorEffects){p.visorEffects.exposure=originalExposure;p.visorEffects.fog=originalFog;p.visorEffects.enabled=visorWasEnabled;}p.AdvanceCinematicPose(0,0,false);p.eye.fieldOfView=oldFov;}
  if(controls)controls.enabled=controlsWereEnabled;if(lowBattery)lowBattery.enabled=lowWasEnabled;
  foreach(var canvas in hiddenCanvases)if(canvas)canvas.enabled=true;
  if(viewFlash)Destroy(viewFlash.gameObject);
  if(CorridorExile)Destroy(CorridorExile);if(npcScream)Destroy(npcScream.gameObject);if(shockVoice)Destroy(shockVoice.gameObject);
  if(whiteAttempt)Destroy(whiteAttempt.gameObject);if(thresholdFixture)Destroy(thresholdFixture);if(fixtureMaterial)Destroy(fixtureMaterial);if(torchHand)Destroy(torchHand.gameObject);if(torchSleeve)Destroy(torchSleeve.gameObject);if(movementAudio)Destroy(movementAudio.gameObject);if(shotBounce)Destroy(shotBounce.gameObject);if(panicVoice)Destroy(panicVoice.gameObject);if(packFoley)Destroy(packFoley.gameObject);if(crawl&&crawl.Mimic)crawl.Mimic.PlayerDialogueGain=1;
 }
}
