using UnityEngine;using System.Collections.Generic;
// Procedural reaction layer over the supplied skeletal locomotion. No attack behaviour.
[DefaultExecutionOrder(150)]
public class WoodlandNPCInjury:MonoBehaviour {
 [Range(1,3)] public float bleedVisibility=1;
 public enum Reaction { None, GuardWound, WallBrace, Kneel, Collapsed }
 public Reaction CurrentReaction{get;private set;}
 public bool HoldsPosition=>!anatomy.Incapacitated&&(Time.time<reactionUntil||reactionBlend>.001f)&&(CurrentReaction==Reaction.Kneel||CurrentReaction==Reaction.WallBrace);
 public bool BlocksWeaponUse=>CurrentReaction!=Reaction.None&&CurrentReaction!=Reaction.Collapsed&&(Time.time<reactionUntil||reactionBlend>.001f);
 public float ReactionStrength=>reactionBlend;
 float reactionUntil,reactionBlend;Vector3 guardTarget;bool hasGuardTarget;Vector3 wallNormal,hipRest;bool hipMoved;
 public WoodlandAnatomy anatomy;public Animator animator;public Material bloodMaterial;
 class Wound{public BodyRegion region;public Transform anchor;public Vector3 local,normal;public float timer;public GameObject stain;}
 readonly List<Wound> wounds=new List<Wound>();readonly List<Transform> posed=new List<Transform>();readonly List<Quaternion> rotations=new List<Quaternion>();
 readonly Queue<GameObject> marks=new Queue<GameObject>();Transform spine,hips,leftArm,leftElbow,leftHand,rightArm,rightElbow,rightHand;ParticleSystem droplets;Mesh stainMesh;float flinch,age,deathTime;Vector3 impact,home;Wound latest;bool applied;WoodlandCompanion companion;
 Transform Bone(string n){foreach(var t in GetComponentsInChildren<Transform>())if(t.name.EndsWith(":"+n))return t;return null;}
 void Awake(){if(!anatomy)anatomy=GetComponent<WoodlandAnatomy>();if(!animator)animator=GetComponentInChildren<Animator>();companion=GetComponent<WoodlandCompanion>();home=transform.position;spine=Bone("Spine2");hips=Bone("Hips");leftArm=Bone("LeftArm");leftElbow=Bone("LeftForeArm");leftHand=Bone("LeftHand");rightArm=Bone("RightArm");rightElbow=Bone("RightForeArm");rightHand=Bone("RightHand");anatomy.HitOccurred+=OnHit;stainMesh=new Mesh();var verts=new Vector3[13];var triangles=new int[36];for(int i=0;i<12;i++){float angle=i*Mathf.PI/6;float radius=.45f+.12f*Mathf.Sin(i*8.7f);verts[i+1]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%12+1;}stainMesh.vertices=verts;stainMesh.triangles=triangles;stainMesh.RecalculateNormals();
 var g=new GameObject("Wound droplets");g.transform.SetParent(transform,false);droplets=g.AddComponent<ParticleSystem>();var main=droplets.main;main.playOnAwake=false;main.loop=false;main.maxParticles=256;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.55f;main.startSize=.014f;main.gravityModifier=1;main.startSpeed=0;var emission=droplets.emission;emission.enabled=false;var r=g.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=bloodMaterial;var temp=GameObject.CreatePrimitive(PrimitiveType.Sphere);r.renderMode=ParticleSystemRenderMode.Mesh;r.mesh=temp.GetComponent<MeshFilter>().sharedMesh;Destroy(temp);droplets.Play();}
 void OnHit(BodyRegion region,float damage,Vector3 point,Vector3 direction){if(damage<=0)return;SelectReaction(region,damage);flinch=Mathf.Max(flinch,Mathf.Clamp01(damage/35));impact=direction.normalized;WoodlandHitZone zone=null;foreach(var z in GetComponentsInChildren<WoodlandHitZone>())if(z.region==region){zone=z;break;}if(!zone)return;
 var w=new Wound{region=region,anchor=zone.transform,local=zone.transform.InverseTransformPoint(point),normal=zone.transform.InverseTransformDirection(-direction.normalized)};var stain=new GameObject("Wound stain");stain.AddComponent<MeshFilter>().sharedMesh=stainMesh;stain.AddComponent<MeshRenderer>();stain.name="Blood at "+region;stain.GetComponent<Renderer>().sharedMaterial=bloodMaterial;stain.transform.position=point-direction.normalized*.006f;stain.transform.rotation=Quaternion.LookRotation(-direction);stain.transform.localScale=new Vector3(.10f,.13f,1);stain.transform.SetParent(zone.transform,true);w.stain=stain;wounds.Add(w);if(latest==null||WoundSeverity(w.region)>WoundSeverity(latest.region)*1.2f)latest=w;if(wounds.Count>10){Destroy(wounds[0].stain);wounds.RemoveAt(0);}Emit(point,-direction*.65f,7);}
 float WoundSeverity(BodyRegion region){var r=anatomy.Region(region);return (100-r.tissue)+(100-r.muscle)*.5f+(r.externalBleed+r.internalBleed)*40;}
 public static Reaction PreserveReaction(Reaction current,Reaction incoming,bool active)=>active&&(int)current>(int)incoming?current:incoming;
 void SelectReaction(BodyRegion region,float damage){
 if(anatomy.Incapacitated){CurrentReaction=Reaction.Collapsed;return;}
 bool active=Time.time<reactionUntil||reactionBlend>.001f;
 var candidate=Reaction.GuardWound;var candidateNormal=wallNormal;
 bool leg=WoodlandAnatomy.IsLeg(region)||region==BodyRegion.Pelvis;
 if(leg&&damage>=12)candidate=Reaction.Kneel;
 else if(damage>=12){foreach(var direction in new[]{transform.right,-transform.right,-transform.forward}){
 foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up*1.1f,direction,.55f,~0,QueryTriggerInteraction.Ignore)){
 if(hit.collider.transform.IsChildOf(transform)||Mathf.Abs(hit.normal.y)>.25f||hit.collider.GetComponentInParent<WoodlandAnatomy>())continue;
 candidateNormal=hit.normal;candidate=Reaction.WallBrace;break;}if(candidate==Reaction.WallBrace)break;}}
 CurrentReaction=PreserveReaction(CurrentReaction,candidate,active);
 if(CurrentReaction==Reaction.WallBrace&&(!active||candidate==Reaction.WallBrace))wallNormal=candidateNormal;
 reactionUntil=Mathf.Max(reactionUntil,Time.time+Mathf.Clamp(damage*.16f,1.5f,6));
 }
 void ReactionPose(){
 reactionBlend=Mathf.MoveTowards(reactionBlend,Time.time<reactionUntil?1:0,Time.deltaTime*(Time.time<reactionUntil?3:2));
 if(reactionBlend<=0){CurrentReaction=Reaction.None;return;}
 if(CurrentReaction==Reaction.WallBrace&&spine){Vector3 toward=transform.InverseTransformDirection(-wallNormal);spine.localRotation*=Quaternion.Euler(toward.z*16*reactionBlend,0,-toward.x*20*reactionBlend);}
 if(CurrentReaction!=Reaction.Kneel||!hips)return;
 var lu=Bone("LeftUpLeg");var lk=Bone("LeftLeg");var lf=Bone("LeftFoot");var ru=Bone("RightUpLeg");var rk=Bone("RightLeg");var rf=Bone("RightFoot");if(!lu||!lk||!lf||!ru||!rk||!rf)return;
 Vector3 l=lf.position,r=rf.position;hipRest=hips.localPosition;hipMoved=true;hips.position+=Vector3.down*(.33f*reactionBlend);
 Save(lu);Save(lk);Save(ru);Save(rk);Reach(lu,lk,lf,l,1,true);Reach(ru,rk,rf,r,1,true);
 }
 void Emit(Vector3 point,Vector3 velocity,int count){if(!droplets)return;var emit=new ParticleSystem.EmitParams{position=point,velocity=velocity+Random.insideUnitSphere*.12f,startSize=.012f*bleedVisibility,startLifetime=.65f};droplets.Emit(emit,count);}
 void RestorePose(){if(hipMoved&&hips){hips.localPosition=hipRest;hipMoved=false;}if(!applied)return;for(int i=0;i<posed.Count;i++)if(posed[i])posed[i].localRotation=rotations[i];posed.Clear();rotations.Clear();applied=false;}
 void Save(Transform t){if(t&&!posed.Contains(t)){posed.Add(t);rotations.Add(t.localRotation);applied=true;}}
 void Update(){if(!anatomy.Incapacitated)RestorePose();else {posed.Clear();rotations.Clear();applied=false;}float dt=Time.deltaTime;age+=dt;flinch=Mathf.MoveTowards(flinch,0,dt*1.4f);if(anatomy.Dead)deathTime+=dt;
 foreach(var w in wounds){if(!w.anchor)continue;float flow=anatomy.Region(w.region).externalBleed;if(flow<=0||deathTime>8)continue;w.timer+=dt*flow*(anatomy.Dead?.3f:1);if(w.timer>.14f/bleedVisibility){w.timer=0;Vector3 point=w.anchor.TransformPoint(w.local);Vector3 outflow=w.anchor.TransformDirection(w.normal)*(.08f+Mathf.Sin(age*8)*.025f)+Vector3.down*.22f;Emit(point,outflow,Mathf.CeilToInt(2*bleedVisibility));GroundBlood(point,flow);}}
 if(!GetComponentInParent<MedicalInterior>()&&!companion&&!GetComponent<WoodlandHostile>()&&animator&&!anatomy.Incapacitated){bool stagger=flinch>.08f;float weakness=1-anatomy.MovementMultiplier;float speed=stagger?.28f:weakness>.3f?.1f:0;Vector3 step=(stagger?impact:-transform.forward)*speed*dt;if(speed>.001f&&Vector3.Distance(transform.position+step,home)<1.5f&&!Physics.SphereCast(transform.position+Vector3.up*.6f,.2f,step.normalized,out _,step.magnitude+.1f,~0,QueryTriggerInteraction.Ignore)){transform.position+=step;var p=transform.position;p.y=WoodlandCompanion.Ground(p.x,p.z)+.04f;transform.position=p;}animator.SetFloat("Speed",speed>0?Mathf.Lerp(.3f,.75f,anatomy.MovementMultiplier):0);animator.speed=Mathf.Lerp(.45f,1,anatomy.MovementMultiplier);}}
 void GroundBlood(Vector3 point,float flow){var hits=Physics.RaycastAll(point,Vector3.down,3,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var hit in hits){if(hit.collider.transform.IsChildOf(transform)||hit.normal.y<.6f)continue;GameObject mark;if(marks.Count>=40)mark=marks.Dequeue();else{mark=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(mark.GetComponent<Collider>());}mark.name="Blood drip";mark.transform.position=hit.point+hit.normal*.005f;mark.transform.rotation=Quaternion.FromToRotation(Vector3.up,hit.normal);float size=Mathf.Clamp((.035f+flow*.10f)*Mathf.Sqrt(bleedVisibility),.035f,.42f);mark.transform.localScale=new Vector3(size,.004f,size*Random.Range(.7f,1.3f));mark.GetComponent<Renderer>().sharedMaterial=bloodMaterial;marks.Enqueue(mark);break;}}
 void LateUpdate(){if(anatomy&&anatomy.Incapacitated){CurrentReaction=Reaction.Collapsed;hipMoved=false;}if(!anatomy||anatomy.Incapacitated||!animator||!animator.enabled||latest==null)return;Save(spine);float weakness=1-anatomy.MovementMultiplier;if(spine)spine.localRotation*=Quaternion.Euler(8+weakness*18+flinch*14,Mathf.Sin(age*2.1f)*weakness*7,Mathf.Sin(age*3)*weakness*5);
 ReactionPose();
 bool leftInjury=latest.region==BodyRegion.LeftArm||latest.region==BodyRegion.LeftForearm||latest.region==BodyRegion.LeftThigh||latest.region==BodyRegion.LeftCalf;bool useRight=leftInjury||anatomy.Region(BodyRegion.RightArm).muscle>=anatomy.Region(BodyRegion.LeftArm).muscle;var upper=useRight?rightArm:leftArm;var elbow=useRight?rightElbow:leftElbow;var hand=useRight?rightHand:leftHand;if(!upper||!elbow||!hand)return;Save(upper);Save(elbow);Save(hand);Vector3 target=latest.anchor.TransformPoint(latest.local)+transform.forward*.045f;if(!hasGuardTarget){guardTarget=hand.position;hasGuardTarget=true;}guardTarget=Vector3.Lerp(guardTarget,target,1-Mathf.Exp(-Time.deltaTime*9));Reach(upper,elbow,hand,guardTarget,.72f*reactionBlend);}
 void Reach(Transform upper,Transform elbow,Transform hand,Vector3 target,float weight,bool leg=false){target=Vector3.Lerp(hand.position,target,weight);Vector3 root=upper.position,joint=elbow.position;float a=Vector3.Distance(root,joint),b=Vector3.Distance(joint,hand.position);Vector3 axis=(target-root).normalized;float d=Mathf.Clamp(Vector3.Distance(root,target),.03f,a+b-.005f);Vector3 bend=Vector3.ProjectOnPlane(leg?transform.forward:-transform.up+transform.right*.25f,axis).normalized;float along=(a*a+d*d-b*b)/(2*d);Vector3 desired=root+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));upper.rotation=Quaternion.FromToRotation(joint-root,desired-root)*upper.rotation;elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;}
 void OnDisable(){if(anatomy&&!anatomy.Incapacitated)RestorePose();}void OnDestroy(){if(stainMesh)Destroy(stainMesh);if(anatomy)anatomy.HitOccurred-=OnHit;foreach(var mark in marks)if(mark)Destroy(mark);}
}
