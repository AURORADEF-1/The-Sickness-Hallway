using UnityEngine;
using UnityEngine.Rendering;

// Bounded, encounter-local sound and debris; no per-frame object allocation.
public class MedicalThornbackEffects : MonoBehaviour {
 MedicalInterior interior;
 AudioSource scrape,scrapeAlternate,growl,groan,impacts,jaw,doorwayRoar;
 public bool RoarPlayed{get;private set;} public float RoarStartedAt{get;private set;}=-1; public bool RoarFinished=>RoarPlayed&&Time.time-RoarStartedAt>=RoarDuration; public float RoarDuration=>doorwayRoar&&doorwayRoar.clip?doorwayRoar.clip.length:0;
 public float RoarCameraPeak=>cameraImpulse?cameraImpulse.PeakRoarShake:0;
 public float ScrapingSeconds {get;private set;}
 public int InvalidScrapeFrames {get;private set;}
 public bool SuppliedDragLoaded=>scrape&&scrape.clip&&scrape.clip.name=="claw-drag-8560";
 public int ScrapePulls{get;private set;}
 public int ScrapeRestFrames{get;private set;}
 public float ThresholdDragSeconds{get;private set;}
 public float CurrentScrapeGain=>scrape?scrape.volume+(scrapeAlternate?scrapeAlternate.volume:0):0;
 public float CurrentDragSpeed{get;private set;}
 public bool CurrentDragContact{get;private set;}
 public float CurrentDragDistance{get;private set;}
 public Vector3 CurrentDragPoint{get;private set;}
 public bool ScrapeActive{get;private set;}
 bool alternatePull;
 readonly RaycastHit[] dragFloorHits=new RaycastHit[8];
 public int PlantThuds {get;private set;}
 public int PullCrumbles {get;private set;}
 AudioSource plantLeft,plantRight,crumbling;
 bool useRight,pendingStrain;float plantAt,pullStrainAge;Vector3 plantedPoint;
 AudioClip thud,rubble,building,hit,doorBreak,doorStrain;
 MedicalThornbackDoorBreak door; MedicalElectricalDamage electrical;MedicalPanicBreathing breathing; float dragLevel;
 ParticleSystem chips;
 Material chipMaterial; Mesh chipMesh;
 MedicalThornbackCameraImpulse cameraImpulse;
 float nextGroan,nextGrowl,nextImpact;
 bool stopped=true;
 public void Configure(MedicalInterior room){
  interior=room;
  if(!scrape){
   scrape=Source("Recorded monster dragging scrape IMG 8560",true);groan=Source("Structure under strain",false);impacts=Source("Claw plants and rubble",false);
   scrape.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/claw-drag-8560");
   if(!scrape.clip)scrape.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/claw-scrape-user");
   if(!scrape.clip)scrape.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/claw-scrape");
   scrapeAlternate=Source("Recorded monster drag alternate take",true);
   scrapeAlternate.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/claw-drag-8560-b");
   scrape.GetComponent<MedicalCorridorAcoustics>().MaximumCutoffHz=6200;
   scrapeAlternate.GetComponent<MedicalCorridorAcoustics>().MaximumCutoffHz=6200;
   growl=Source("Thornback recorded growl",false);
   growl.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/monster-growl-deep");
   growl.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.1f,.95f),new Keyframe(.3f,.45f),new Keyframe(.65f,.12f),new Keyframe(1,0)));
   growl.transform.localPosition=Vector3.up*1.7f;
   doorwayRoar=Source("Final towering Thornback roar",false);
   doorwayRoar.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/doorway-roar-titan");
   doorwayRoar.transform.localPosition=Vector3.up*1.7f;doorwayRoar.volume=1;
   // Near-field climax keeps full impact across the doorway; quieter lure provides contrast.
   doorwayRoar.priority=0;doorwayRoar.minDistance=3;doorwayRoar.maxDistance=28;
   doorwayRoar.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.25f,1),new Keyframe(.60f,.45f),new Keyframe(1,0)));
   doorwayRoar.gameObject.AddComponent<AudioDistortionFilter>().distortionLevel=.22f;
   jaw=Source("Close jaw snap",false);jaw.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/jaw-snap");jaw.volume=.65f;
   thud=Resources.Load<AudioClip>("Medical/ThornbackFX/claw-plant-user");
   if(!thud)thud=Resources.Load<AudioClip>("Medical/ThornbackFX/weight-thud");
   plantLeft=Source("Claw plant thud A",false);plantRight=Source("Claw plant thud B",false);crumbling=Source("Wall crumbling under pull",false);
   rubble=Resources.Load<AudioClip>("Medical/ThornbackFX/plaster-fall");
   building=Resources.Load<AudioClip>("Medical/ThornbackFX/building-groan");hit=Resources.Load<AudioClip>("Medical/ThornbackFX/dull-impact");
   doorBreak=Resources.Load<AudioClip>("Medical/ThornbackFX/door-break");doorStrain=Resources.Load<AudioClip>("Medical/ThornbackFX/door-strain");
   MakeChips();
  }
  if(room&&room.owner&&room.owner.player&&room.owner.player.eye){
   var eye=room.owner.player.eye;cameraImpulse=eye.GetComponent<MedicalThornbackCameraImpulse>()??eye.gameObject.AddComponent<MedicalThornbackCameraImpulse>();
  }
  door=GetComponent<MedicalThornbackDoorBreak>()??gameObject.AddComponent<MedicalThornbackDoorBreak>();door.Configure(room,this);
  foreach(var voice in room.GetComponentsInChildren<MedicalVoice>(true))voice.SilenceForEncounter();
  electrical=GetComponent<MedicalElectricalDamage>()??gameObject.AddComponent<MedicalElectricalDamage>();electrical.Configure(room);
  var player=room.owner.player;breathing=player.GetComponent<MedicalPanicBreathing>()??player.gameObject.AddComponent<MedicalPanicBreathing>();breathing.Configure(room);
  stopped=false;nextGroan=4;nextImpact=0;dragLevel=0;scrape.volume=0;scrape.Stop();
  nextGrowl=2.5f;scrapeAlternate.volume=0;scrapeAlternate.Stop();
  ScrapingSeconds=ThresholdDragSeconds=0;InvalidScrapeFrames=ScrapePulls=ScrapeRestFrames=0;ScrapeActive=false;CurrentDragSpeed=0;CurrentDragContact=false;
  RoarPlayed=false;RoarStartedAt=-1;PlantThuds=PullCrumbles=0;pendingStrain=false;pullStrainAge=0;plantAt=-100;
 }
 AudioSource Source(string label,bool loop){var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up;var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.loop=loop;a.spatialBlend=1;a.rolloffMode=AudioRolloffMode.Logarithmic;a.minDistance=2.5f;a.maxDistance=24;a.dopplerLevel=0;MedicalCorridorAcoustics.Setup(a,interior.owner.player.eye.transform);return a;}
 void MakeChips(){
  var go=new GameObject("Falling plaster fragments");go.transform.SetParent(transform,false);go.SetActive(false);
  chips=go.AddComponent<ParticleSystem>();var main=chips.main;main.playOnAwake=false;main.loop=false;main.maxParticles=80;main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,.85f);main.startSize=new ParticleSystem.MinMaxCurve(.012f,.045f);main.startColor=new Color(.30f,.28f,.24f,.9f);main.gravityModifier=1.1f;main.simulationSpace=ParticleSystemSimulationSpace.World;
  var emission=chips.emission;emission.enabled=false;var shape=chips.shape;shape.enabled=false;
  var stored=Resources.Load<Material>("Medical/ThornbackPlaster");
  var shader=Shader.Find("Universal Render Pipeline/Lit");
  if(stored||shader){chipMaterial=stored?new Material(stored):new Material(shader);chipMaterial.SetColor("_BaseColor",new Color(.30f,.28f,.24f));go.GetComponent<ParticleSystemRenderer>().sharedMaterial=chipMaterial;}
  chipMesh=new Mesh{name="Plaster chip octahedron"};chipMesh.vertices=new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back};chipMesh.triangles=new[]{0,4,3,0,3,5,0,5,2,0,2,4,1,3,4,1,5,3,1,2,5,1,4,2};chipMesh.RecalculateNormals();
  var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=chipMesh;
  var rotation=chips.rotationOverLifetime;rotation.enabled=true;rotation.z=new ParticleSystem.MinMaxCurve(-4,4);
  go.SetActive(true);chips.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
 }
 public void Plant(Vector3 point,bool floor){
  if(stopped)return;impacts.transform.position=point;
  var plant=useRight?plantRight:plantLeft;useRight=!useRight;plant.transform.position=point;
  if(thud){plant.PlayOneShot(thud,floor?.65f:.55f);PlantThuds++;}
  if(!floor){pendingStrain=true;plantAt=Time.time;pullStrainAge=0;plantedPoint=point;}
  for(int n=0;n<(floor?5:10);n++){var p=new ParticleSystem.EmitParams{position=point,velocity=new Vector3(Random.Range(-.25f,.25f),Random.Range(.08f,.45f),Random.Range(-.2f,.2f))};chips.Emit(p,1);}
  if(cameraImpulse)cameraImpulse.Pulse(Attenuation()*(floor?.022f:.012f));
 }
 float Attenuation(){if(!interior||!interior.owner||!interior.owner.player)return 0;return Mathf.Clamp01(1-Vector3.Distance(transform.position,interior.owner.player.transform.position)/18);}
 public void Drag(float time,float speed,float stagger,bool wallContact,Vector3 contactPoint){
  if(stopped)return;if(interior&&interior.finished){StopAll();return;}
  var beat=GetComponent<MedicalDoorwayBeat>();
  bool moving=speed>.015f,contact=wallContact,thresholdDrag=false;
  Vector3 point=contactPoint;
  // The hands remain planted on the jamb during the final haul. That phase is
  // body/floor friction, not claws sliding through the static grip anchors.
  if(moving&&beat&&beat.Active){
   int count=Physics.RaycastNonAlloc(transform.position+Vector3.up*.55f,Vector3.down,dragFloorHits,.95f,~0,QueryTriggerInteraction.Ignore);
   float nearest=float.MaxValue;
   for(int n=0;n<count;n++){
    var hit=dragFloorHits[n];if(hit.transform.IsChildOf(transform)||hit.normal.y<.65f||hit.distance>=nearest)continue;
    nearest=hit.distance;point=hit.point+Vector3.up*.04f;contact=thresholdDrag=true;
   }
  }
  bool active=moving&&contact&&Time.time-plantAt>=.10f;
  CurrentDragSpeed=speed;CurrentDragContact=contact;CurrentDragPoint=point;
  if(active){
   if(!ScrapeActive){
    ScrapePulls++;alternatePull=scrapeAlternate&&scrapeAlternate.clip&&(ScrapePulls%2==0);
    var chosen=alternatePull?scrapeAlternate:scrape;chosen.time=0;chosen.Play();
   }
   ScrapingSeconds+=Time.deltaTime;if(thresholdDrag)ThresholdDragSeconds+=Time.deltaTime;
   scrape.transform.position=point;if(scrapeAlternate)scrapeAlternate.transform.position=point;
   CurrentDragDistance=Vector3.Distance(point,interior.owner.player.eye.transform.position);
  }else ScrapeRestFrames++;
  // Start the supplied scrape with the pull rather than running it at a random
  // phase under silence. The dry contact sound stops immediately at each pause.
  float pressure=Mathf.SmoothStep(0,1,speed/.10f);
  if(thresholdDrag)pressure*=Mathf.SmoothStep(0,1,(8.2f-beat.Age)/.12f);
  dragLevel=active?Mathf.MoveTowards(dragLevel,.64f*pressure,Time.deltaTime*6):0;
  scrape.volume=alternatePull?0:dragLevel;scrape.pitch=1;
  if(scrapeAlternate){scrapeAlternate.volume=alternatePull?dragLevel:0;scrapeAlternate.pitch=1;}
  ScrapeActive=active;
  if(!active&&CurrentScrapeGain>.0001f)InvalidScrapeFrames++;
  var mimic=GetComponent<MedicalHumanMimic>();
  if((!mimic||mimic.VoiceEnded)&&(!beat||!beat.Active)&&growl&&growl.clip&&time>=nextGrowl&&!growl.isPlaying){growl.volume=.74f;growl.Play();nextGrowl=time+13;}
  if(active&&pendingStrain){
   if(pullStrainAge==0&&time>=nextGroan&&!groan.isPlaying){nextGroan=time+4;groan.transform.position=plantedPoint;if(building)groan.PlayOneShot(building,.38f);if(cameraImpulse)cameraImpulse.Pulse(.012f*Attenuation());}
   pullStrainAge+=Time.deltaTime;
   if(pullStrainAge>=.28f){pendingStrain=false;crumbling.transform.position=plantedPoint;if(rubble){crumbling.PlayOneShot(rubble,.20f);PullCrumbles++;}
    for(int n=0;n<8;n++)chips.Emit(new ParticleSystem.EmitParams{position=plantedPoint,velocity=new Vector3(Random.Range(-.08f,.08f),-.18f,Random.Range(-.08f,.08f))},1);
   }
  }
  if(door)door.Step();
 }
 public void JawSnap(Vector3 point){if(stopped||!jaw||!jaw.clip)return;jaw.transform.position=point;jaw.Play();if(cameraImpulse)cameraImpulse.Pulse(.007f*Attenuation());}
 public void DoorwayRoar(){
  if(stopped||!doorwayRoar||!doorwayRoar.clip)return;
  if(growl)growl.Stop();doorwayRoar.Play();RoarPlayed=true;RoarStartedAt=Time.time;nextGrowl=float.MaxValue;
  if(cameraImpulse)cameraImpulse.Roar(doorwayRoar.clip.length);
  var player=interior.owner.player;if(player&&player.visorEffects)player.visorEffects.RoarExposure();
 }
 public void DoorStrain(Vector3 point){impacts.transform.position=point;if(doorStrain)impacts.PlayOneShot(doorStrain,.48f);}
 public void DoorBreak(Vector3 point){
  impacts.transform.position=point;if(doorBreak)impacts.PlayOneShot(doorBreak,.75f);if(rubble)impacts.PlayOneShot(rubble,.32f);
  for(int n=0;n<42;n++){var p=new ParticleSystem.EmitParams{position=point+new Vector3(Random.Range(-.65f,.65f),Random.Range(-.8f,1),0),velocity=new Vector3(Random.Range(-.6f,.6f),Random.Range(.2f,1.1f),Random.Range(.3f,1.5f)),startSize=Random.Range(.008f,.028f),startLifetime=Random.Range(.7f,1.6f)};chips.Emit(p,1);}
  if(cameraImpulse)cameraImpulse.Pulse(.03f*Attenuation());
 }
 public void Impact(Vector3 point){if(stopped||Time.time<nextImpact)return;nextImpact=Time.time+.09f;impacts.transform.position=point;if(hit)impacts.PlayOneShot(hit,.3f);}
 public void StopAll(){stopped=true;dragLevel=0;ScrapeActive=false;if(scrape)scrape.volume=0;if(scrapeAlternate)scrapeAlternate.volume=0;if(plantLeft)plantLeft.Stop();if(plantRight)plantRight.Stop();if(crumbling)crumbling.Stop();pendingStrain=false;if(electrical)electrical.StopEffects();if(breathing)breathing.StopBreathing();if(scrape)scrape.Stop();if(scrapeAlternate)scrapeAlternate.Stop();if(growl)growl.Stop();if(doorwayRoar)doorwayRoar.Stop();if(jaw)jaw.Stop();if(groan)groan.Stop();if(impacts)impacts.Stop();if(chips)chips.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(cameraImpulse)cameraImpulse.Clear();}
 void OnDisable(){StopAll();}
 void OnDestroy(){if(chipMesh)Destroy(chipMesh);if(chipMaterial)Destroy(chipMaterial);}
}

// A render-only pose, applied before culling and restored exactly afterwards.
// The explicit capture entry points also cover standalone Camera.Render paths
// which do not dispatch SRP begin/end callbacks. Applying twice is a no-op.
public class MedicalThornbackCameraImpulse : MonoBehaviour {
 [Range(0,1)] public float strength=1;
 float envelope;Vector3 savedPosition;Quaternion savedRotation;bool applied;Camera eye;int captureDepth;
 float roarStart=-100,roarDuration,savedFov;
 public float PeakRoarShake{get;private set;}
 public float PeakRoarRotation{get;private set;}
 public float PeakRoarFov{get;private set;}
 public int RoarRenderSamples{get;private set;}
 public bool IsApplied=>applied;
 public bool RoarActive=>Time.time>=roarStart&&Time.time-roarStart<roarDuration;
 public void Roar(float duration){Restore();roarStart=Time.time;roarDuration=Mathf.Max(0,duration);PeakRoarShake=PeakRoarRotation=PeakRoarFov=0;RoarRenderSamples=0;}
 void OnEnable(){eye=GetComponent<Camera>();RenderPipelineManager.beginCameraRendering+=Begin;RenderPipelineManager.endCameraRendering+=End;}
 public void Pulse(float amount){envelope=Mathf.Min(.035f,envelope+Mathf.Max(0,amount));}
 public void Clear(){envelope=0;roarStart=-100;roarDuration=0;captureDepth=0;Restore();}
 void LateUpdate(){envelope=Mathf.MoveTowards(envelope,0,Time.deltaTime*.065f);}
 void Begin(ScriptableRenderContext context,Camera camera){if(camera==eye)Apply();}
 void End(ScriptableRenderContext context,Camera camera){if(camera==eye&&captureDepth==0)Restore();}
 void OnPreCull(){if(GraphicsSettings.currentRenderPipeline==null)Apply();}
 void OnPostRender(){if(GraphicsSettings.currentRenderPipeline==null&&captureDepth==0)Restore();}
 public void BeginCapture(){captureDepth++;Apply();}
 public void EndCapture(){captureDepth=Mathf.Max(0,captureDepth-1);if(captureDepth==0)Restore();}
 void Apply(){
  if(applied||!eye||Time.timeScale==0||strength<=0||(envelope<=0&&!RoarActive))return;
  applied=true;savedPosition=transform.localPosition;savedRotation=transform.localRotation;savedFov=eye.fieldOfView;
  float t=Time.time,amount=envelope*strength;
  Vector3 offset=new Vector3(Mathf.Sin(t*39)*.3f,Mathf.Sin(t*31),0)*amount;
  Vector3 angles=new Vector3(Mathf.Sin(t*29)*amount*12,0,Mathf.Sin(t*23)*amount*6);
  float fov=0;
  if(RoarActive){
   float age=Time.time-roarStart;
   float release=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(.1f,roarDuration-1.65f),roarDuration,age));
   float tremor=Mathf.SmoothStep(0,1,age/.10f)*Mathf.Lerp(1,.55f,Mathf.Clamp01(age/roarDuration))*release*strength;
   float impact=Mathf.Exp(-age*5.5f)*strength;
   // The onset knocks the head backwards, then an irregular, diminishing
   // tremor carries the roar's weight without a constant mechanical vibration.
   offset+=new Vector3(Mathf.Sin(age*31)*.018f,Mathf.Sin(age*43)*.035f+Mathf.Sin(age*17)*.008f,Mathf.Sin(age*23)*.012f)*tremor;
   offset+=new Vector3(0,-.018f,-.035f)*impact;
   angles+=new Vector3(Mathf.Sin(age*29)*1.4f+Mathf.Sin(age*13)*.3f,Mathf.Sin(age*19)*.65f,Mathf.Sin(age*17)*1.15f)*tremor;
   angles+=new Vector3(-2.6f,.35f,1.1f)*impact;
   fov=-2.8f*impact+tremor*(-.9f+Mathf.Sin(age*16)*.85f);
  }
  transform.localPosition=savedPosition+offset;
  transform.localRotation=savedRotation*Quaternion.Euler(angles);
  eye.fieldOfView=savedFov+fov;
  if(RoarActive){
   RoarRenderSamples++;
   PeakRoarShake=Mathf.Max(PeakRoarShake,offset.magnitude);
   PeakRoarRotation=Mathf.Max(PeakRoarRotation,Quaternion.Angle(savedRotation,transform.localRotation));
   PeakRoarFov=Mathf.Max(PeakRoarFov,Mathf.Abs(fov));
  }
 }
 void Restore(){if(!applied)return;transform.localPosition=savedPosition;transform.localRotation=savedRotation;if(eye)eye.fieldOfView=savedFov;applied=false;}
 void OnDisable(){RenderPipelineManager.beginCameraRendering-=Begin;RenderPipelineManager.endCameraRendering-=End;Clear();}
}
