using UnityEngine;
using System.Collections.Generic;

// Corridor-only variant of the existing Thornback. All deformation is instance-local.
// Grip targets remain in world space during the loaded part of a pull.
public class MedicalThornbackCrawl : MonoBehaviour {
 public Transform visual;
 public float cycleSeconds=5.4f;
 public int hitCount;
 public float stagger;
 public const float RevealSeconds=6.5f;
 public float GripError { get; private set; }
 public int ContactCount { get; private set; }
 public bool WallScraping {get;private set;}
 public Vector3 ScrapePoint {get;private set;}
 public int JawSnaps {get;private set;}
 float actualSpeed,snapStarted=-100,nextSnap;bool snapPlayed;
 Vector3 frameTravel;
 readonly bool[] wallContact=new bool[2];
 float staggerCooldown;
 bool ready;
 Vector3 visualRest;
 readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
 readonly Dictionary<string,Quaternion> rest=new Dictionary<string,Quaternion>();
 readonly Transform[] tips=new Transform[2];
 readonly Vector3[] anchors=new Vector3[2];
 readonly bool[] planted=new bool[2];
 readonly int[] lastCycle={int.MinValue,int.MinValue};
 MedicalThornbackEffects effects;MedicalDoorwaySurfaceDetail doorwaySurfaces;
 public MedicalDoorwayBeat Doorway {get;private set;}
 public MedicalHumanMimic Mimic {get;private set;}
 MedicalInterior encounterRoom;bool physicalStarted;
 public float PresentationTime{get;private set;}
 readonly bool[] jambPlanted=new bool[2];

 public void Prepare(){
  if(ready)return;
  if(!visual){if(transform.childCount==0)return;visual=transform.GetChild(0);}
  ready=true;
  bool isVariant=visual.name.StartsWith("ThornbackCorridorVisual");
  if(!isVariant&&Application.isPlaying){
   var prefab=Resources.Load<GameObject>("Medical/ThornbackCorridorVisual");
   if(prefab){var old=visual;visual=Instantiate(prefab,old.parent).transform;visual.localPosition=old.localPosition;visual.localRotation=old.localRotation;visual.localScale=old.localScale;old.gameObject.SetActive(false);Destroy(old.gameObject);isVariant=true;}
  }
  foreach(var a in visual.GetComponentsInChildren<Animator>(true))a.enabled=false;
  foreach(var t in visual.GetComponentsInChildren<Transform>(true)){bones[t.name]=t;rest[t.name]=t.localRotation;}
  // Elongate the existing rig, rather than stretching the whole creature into a giant.
  // Preserve its slender trunk; add modest muscle mass only to the working arms.
  for(int s=0;s<2;s++){
   string side=s==0?"L":"R";
   if(!bones.TryGetValue("arm"+side,out var upper)||!bones.TryGetValue("forearm"+side,out var lower))continue;
   if(isVariant){tips[s]=bones["Corridor claw contact "+side];continue;}
   upper.localScale=Vector3.Scale(upper.localScale,Vector3.one*1.38f);
   lower.localScale=Vector3.Scale(lower.localScale,Vector3.one*1.08f);
   var tip=new GameObject("Corridor claw contact "+side).transform;
   tip.SetParent(lower,false);
   Vector3 boneAxis=upper.InverseTransformDirection(lower.position-upper.position).normalized;
   tip.position=lower.position+lower.TransformDirection(boneAxis)*Vector3.Distance(upper.position,lower.position)*1.80f;
   tips[s]=tip;
   AddLimbCollider(upper,lower,.13f);AddLimbCollider(lower,tip,.10f);
  }
  visualRest=visual.localPosition;
  foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>()){
   // Extended arms must not disappear when their original imported bounds leave view.
   var bounds=r.localBounds;bounds.Expand(bounds.size*.65f);r.localBounds=bounds;
  }
  effects=GetComponent<MedicalThornbackEffects>()??gameObject.AddComponent<MedicalThornbackEffects>();
 }
 static void AddLimbCollider(Transform bone,Transform end,float radius){
  var go=new GameObject("Corridor limb hit volume");go.transform.SetParent(bone,false);
  Vector3 delta=end.position-bone.position;go.transform.position=(bone.position+end.position)*.5f;
  go.transform.rotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);
  var scale=go.transform.lossyScale;var collider=go.AddComponent<CapsuleCollider>();collider.direction=1;
  collider.radius=radius/Mathf.Max(.001f,Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)));
  collider.height=delta.magnitude/Mathf.Max(.001f,Mathf.Abs(scale.y));
 }
 public void Begin(MedicalInterior interior){
  Prepare();stagger=staggerCooldown=0;hitCount=ContactCount=0;
  jambPlanted[0]=jambPlanted[1]=false;JawSnaps=0;snapStarted=-100;nextSnap=0;actualSpeed=0;frameTravel=Vector3.zero;
  for(int s=0;s<2;s++){planted[s]=false;lastCycle[s]=int.MinValue;}
  encounterRoom=interior;physicalStarted=false;PresentationTime=0;
  doorwaySurfaces=interior.GetComponent<MedicalDoorwaySurfaceDetail>();
  Mimic=GetComponent<MedicalHumanMimic>();if(!Mimic)Mimic=gameObject.AddComponent<MedicalHumanMimic>();Mimic.Configure(interior,visual);
  Pose(0);
 }
 void Bend(string name,float angle){if(bones.TryGetValue(name,out var b))b.rotation=Quaternion.AngleAxis(angle,transform.right)*b.rotation;}
 public void ResetPose(){Prepare();if(!ready)return;foreach(var pair in rest)if(bones[pair.Key])bones[pair.Key].localRotation=pair.Value;visual.localPosition=visualRest;}
 public void Pose(float time){
  ResetPose();if(!ready)return;
  float reveal=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.4f,RevealSeconds,time));
  float phase=Mathf.Repeat(Mathf.Max(0,time-RevealSeconds)/Mathf.Max(4.8f,cycleSeconds),1);
  // Arms reach ahead while the head and shoulders remain bowed in the dark entrance.
  visual.localPosition=visualRest-transform.InverseTransformDirection(transform.forward)*(.50f*(1-reveal))+Vector3.down*.24f;
  Bend("body",Mathf.Lerp(48,30,reveal)-stagger*3);
  Bend("head",Mathf.Lerp(24,-4,reveal)+stagger*5);
  float biteAge=time-snapStarted;
  if(biteAge>=0&&biteAge<.8f){float bite=biteAge<.32f?Mathf.SmoothStep(0,1,biteAge/.32f):1-Mathf.SmoothStep(0,1,(biteAge-.32f)/.48f);Bend("head",-bite*12);if(bones.ContainsKey("jaw"))Bend("jaw",biteAge<.32f?bite*24:0);}
  Bend("tail",-12);
  Bend("thighL",-18);Bend("thighR",-18);Bend("shinL",30);Bend("shinR",30);
  GripError=0;WallScraping=false;ScrapePoint=Vector3.zero;int scrapeContacts=0;
  if(Doorway&&Doorway.Active){
   for(int s=0;s<2;s++){if(!tips[s])continue;Vector3 grip=SideSign(s)<0?Doorway.LeftTarget:Doorway.RightTarget;
    SolveArm(s,grip);GripError=Mathf.Max(GripError,Vector3.Distance(tips[s].position,grip));
    if(!jambPlanted[s]){jambPlanted[s]=true;ContactCount++;effects.Plant(grip,false);if(doorwaySurfaces)doorwaySurfaces.ClawImpact(grip);}
   }
   return;
  }
  for(int s=0;s<2;s++){
   if(!tips[s])continue;
   float armTime=Mathf.Max(0,time-s*1.25f);
   float armPhase=time<RevealSeconds?Mathf.Clamp01(armTime/2.2f):Mathf.Repeat(phase+s*.5f,1);
   int lap=time<RevealSeconds?-1:Mathf.FloorToInt((time-RevealSeconds)/Mathf.Max(4.8f,cycleSeconds)+s*.5f);
   bool floor=time>=RevealSeconds&&(lap+s)%3==2;
   Vector3 goal=ContactTarget(s,floor);
   bool loaded=time<RevealSeconds?armTime>=1.55f:armPhase>=.26f;
   if(loaded&&!planted[s]){
    anchors[s]=goal;planted[s]=true;
    if(Application.isPlaying&&lastCycle[s]!=lap){lastCycle[s]=lap;ContactCount++;if(effects)effects.Plant(goal,floor);}
   }
   if(!loaded)planted[s]=false;
   // A small controlled wall slide during the haul provides real scrape travel.
   if(loaded&&!floor&&wallContact[s]&&actualSpeed>.015f)anchors[s]+=frameTravel*.30f;
   Vector3 target=loaded?anchors[s]:Vector3.Lerp(transform.position+transform.forward*.55f+Vector3.up*.8f,goal,Mathf.SmoothStep(0,1,time<RevealSeconds?armTime/1.55f:armPhase/.26f));
   if(!loaded)target.y+=Mathf.Sin(Mathf.PI*Mathf.Clamp01(time<RevealSeconds?armTime/1.55f:armPhase/.26f))*.2f;
   SolveArm(s,target);
   if(loaded)GripError=Mathf.Max(GripError,Vector3.Distance(tips[s].position,target));
   if(loaded&&!floor&&wallContact[s]&&actualSpeed>.015f&&Vector3.Distance(tips[s].position,target)<.04f){WallScraping=true;ScrapePoint+=tips[s].position;scrapeContacts++;}
  }
  if(scrapeContacts>0)ScrapePoint/=scrapeContacts;
 }
 float SideSign(int side){return Mathf.Sign(Vector3.Dot(bones["arm"+(side==0?"L":"R")].position-transform.position,transform.right));}
 Vector3 ContactTarget(int side,bool floor){
  wallContact[side]=false;
  float sign=SideSign(side);
  Vector3 target=transform.position+transform.forward*1.55f+transform.right*(floor?.68f:1.23f)*sign+Vector3.up*(floor?.12f:1.38f);
  // Actual wall/floor geometry defines the contact; ignore our own hit collider.
  Vector3 origin=floor?target+Vector3.up:transform.position+transform.forward*1.55f+Vector3.up*1.38f;
  Vector3 direction=floor?Vector3.down:transform.right*sign;
  float nearest=floor?1.2f:1.5f;
  foreach(var hit in Physics.RaycastAll(origin,direction,nearest,~0,QueryTriggerInteraction.Ignore)){
   if(hit.transform.IsChildOf(transform)||hit.distance>=nearest)continue;
   nearest=hit.distance;target=hit.point+hit.normal*.035f;wallContact[side]=!floor&&Mathf.Abs(hit.normal.y)<.35f;
  }
  return target;
 }
 void SolveArm(int side,Vector3 goal){
  string suffix=side==0?"L":"R";var upper=bones["arm"+suffix];var lower=bones["forearm"+suffix];var tip=tips[side];
  Vector3 origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,tip.position);
  Vector3 delta=goal-origin;if(a<.001f||b<.001f||delta.sqrMagnitude<.0001f)return;
  float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);Vector3 axis=delta.normalized;
  Vector3 pole=Vector3.ProjectOnPlane(Doorway&&Doorway.Active?-transform.forward+transform.right*SideSign(side)*.65f+Vector3.up*.15f:transform.right*SideSign(side)+Vector3.up*.1f,axis).normalized;
  if(pole.sqrMagnitude<.001f)pole=Vector3.Cross(axis,transform.forward).normalized;
  float x=(a*a-b*b+d*d)/(2*d);float y=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));
  Vector3 elbow=origin+axis*x+pole*y;
  upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;
  lower.rotation=Quaternion.FromToRotation(tip.position-lower.position,origin+axis*d-lower.position)*lower.rotation;
 }
 public float AdvanceSpeed(float time){
  if(time<RevealSeconds)return 0;
  float p=Mathf.Repeat((time-RevealSeconds)/Mathf.Max(4.8f,cycleSeconds),.5f)*2;
  // A recovery pause followed by a heavy pull. No constant-speed sliding.
  return p<.30f?0:.48f*Mathf.Sin(Mathf.InverseLerp(.30f,1,p)*Mathf.PI);
 }
 public void Step(float time,float dt,Vector3 target){
  Prepare();
  if(Mimic){Mimic.Tick(time,dt);if(Mimic.IsHidden){actualSpeed=0;frameTravel=Vector3.zero;WallScraping=false;return;}time=Mathf.Max(0,time-MedicalHumanMimic.HiddenLeadIn);}
  PresentationTime=time;
  if(!physicalStarted){physicalStarted=true;effects.Configure(encounterRoom);Doorway=GetComponent<MedicalDoorwayBeat>();if(!Doorway)Doorway=gameObject.AddComponent<MedicalDoorwayBeat>();Doorway.Configure(encounterRoom);}
  stagger=Mathf.MoveTowards(stagger,0,dt*1.6f);staggerCooldown=Mathf.Max(0,staggerCooldown-dt);
  float speed=AdvanceSpeed(time)*(1-stagger*.75f);
  if(Doorway){float authored=Doorway.Advance(dt);if(authored>=0)speed=authored*(1-stagger*.75f);}
  Vector3 delta=target-transform.position;delta.y=0;
  Vector3 before=transform.position;
  // Never turn the long body through the corridor walls or back away from the player.
  if(Vector3.Dot(delta,transform.forward)>2.4f)transform.position+=transform.forward*speed*dt;
  frameTravel=transform.position-before;actualSpeed=dt>0?frameTravel.magnitude/dt:0;
  if(time>RevealSeconds&&delta.magnitude<4.8f&&Vector3.Dot(delta.normalized,transform.forward)>.75f&&time>=nextSnap){snapStarted=time;snapPlayed=false;nextSnap=time+4.5f;}
  Pose(time);
  if(!snapPlayed&&time-snapStarted>=.32f&&time-snapStarted<.8f){snapPlayed=true;if(delta.magnitude<5.1f){JawSnaps++;if(effects)effects.JawSnap(bones.TryGetValue("head",out var head)?head.position:transform.position+Vector3.up*1.7f);}}
  if(effects)effects.Drag(time,actualSpeed,stagger,WallScraping,ScrapePoint);
 }
 public void ReceiveHit(Vector3 point,Vector3 direction){
  hitCount++;
  if(staggerCooldown<=0){stagger=.65f;staggerCooldown=.8f;}
  // This encounter's Thornback has no blood emitter and cannot be killed.
  if(effects)effects.Impact(point);
 }
 public void StopPresentation(){if(Mimic)Mimic.Stop();if(effects)effects.StopAll();}
 void OnDisable(){StopPresentation();}
}
