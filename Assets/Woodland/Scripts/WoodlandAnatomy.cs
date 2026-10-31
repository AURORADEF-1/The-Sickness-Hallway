using System;
using System.Collections.Generic;
using UnityEngine;
// Tunable fiction/gameplay model, not a clinical or ballistic simulator.
public enum BodyRegion { Head,Neck,Chest,Abdomen,Pelvis,LeftArm,RightArm,LeftForearm,RightForearm,LeftThigh,RightThigh,LeftCalf,RightCalf }
public enum InjuryKind { Projectile,Blunt,Fall }
[Serializable] public class RegionInjury {
 public BodyRegion region;public float tissue=100,bone=100,muscle=100,externalBleed,internalBleed;
 public bool bandaged,splinted;public float armour;
}
[Serializable] public class HumanCondition {
 public float oxygen=100;public float blood=100,pain,shock,brain=100,heart=100,lungs=100,liver=100,spine=100;
 public bool dead;public List<RegionInjury> regions=new List<RegionInjury>();
 public HumanCondition(){foreach(BodyRegion r in Enum.GetValues(typeof(BodyRegion)))regions.Add(new RegionInjury{region=r});}
}
public class WoodlandAnatomy:MonoBehaviour {
 [Range(1,6)] public float woundBleedSeverity=1;
 public HumanCondition condition=new HumanCondition();public WoodlandRagdoll ragdoll;public bool playerBody;
 public bool Downed=>!condition.dead&&(condition.blood<42||condition.shock>82||condition.brain<35||condition.spine<20||Region(BodyRegion.Chest).tissue<=0);
 public bool Dead=>condition.dead;public bool Incapacitated=>Dead||Downed;
 public float Bleeding{get{float sum=0;foreach(var r in condition.regions)sum+=r.externalBleed+r.internalBleed;return sum;}}
 public event Action Changed;
 public event Action<BodyRegion,float,Vector3,Vector3> HitOccurred;
 float clock;bool wasIncapacitated;Vector3 lastForce,lastPoint;BodyRegion lastRegion;
 public RegionInjury Region(BodyRegion r){return condition.regions.Find(x=>x.region==r);}
 public float MovementMultiplier {get{if(Incapacitated)return 0;float weakest=1;foreach(var r in condition.regions)if(IsLeg(r.region)){float strength=Mathf.Min(r.muscle, r.bone<40?(r.splinted?55:20):100);weakest=Mathf.Min(weakest,Mathf.Lerp(.22f,1,strength/100));}return weakest*Mathf.Lerp(.6f,1,condition.blood/100)*Mathf.Lerp(.55f,1,condition.lungs/100);}}
 public float AimInstability {get{float weakness=0;foreach(var r in condition.regions)if(IsArm(r.region))weakness=Mathf.Max(weakness,1-Mathf.Min(r.bone,r.muscle)/100);return weakness*1.7f+condition.pain*.008f+condition.shock*.012f;}}
 public static bool IsLeg(BodyRegion r)=>r>=BodyRegion.LeftThigh;
 public static bool IsArm(BodyRegion r)=>r>=BodyRegion.LeftArm&&r<=BodyRegion.RightForearm;
 public void Hit(BodyRegion region,float power,InjuryKind kind,Vector3 point,Vector3 direction,float organDepth=0){
  if(Dead||power<=0)return;var r=Region(region);float absorbed=Mathf.Min(r.armour,power*.6f);r.armour=Mathf.Max(0,r.armour-power*.35f);float d=Mathf.Max(0,power-absorbed);
  r.tissue=Mathf.Max(0,r.tissue-d);r.muscle=Mathf.Max(0,r.muscle-d*(IsArm(region)||IsLeg(region)?1.15f:.35f));r.bone=Mathf.Max(0,r.bone-d*(kind==InjuryKind.Blunt||kind==InjuryKind.Fall?1.25f:.85f));
  if(d>0){r.bandaged=false;if(kind==InjuryKind.Projectile){r.externalBleed=Mathf.Min(1.5f*woundBleedSeverity,r.externalBleed+d*.006f*woundBleedSeverity);if(region==BodyRegion.Neck)r.externalBleed+=d*.007f;}else r.externalBleed=Mathf.Min(.4f,r.externalBleed+d*.0005f);}
  float penetration=kind==InjuryKind.Projectile?d:Mathf.Max(0,d-30)*.3f;
  // Region and local hit position approximate organ involvement for gameplay.
  if(region==BodyRegion.Head)condition.brain=Mathf.Max(0,condition.brain-penetration*2.7f);
  if(region==BodyRegion.Neck&&organDepth>.6f)condition.spine=Mathf.Max(0,condition.spine-penetration*1.8f);
  if(region==BodyRegion.Chest){if(organDepth>.75f)condition.heart=Mathf.Max(0,condition.heart-penetration*1.5f);else condition.lungs=Mathf.Max(0,condition.lungs-penetration*.75f);r.internalBleed+=penetration*.0025f;}
  if(region==BodyRegion.Abdomen){condition.liver=Mathf.Max(0,condition.liver-penetration*.7f);r.internalBleed+=penetration*.003f;}
  if(region==BodyRegion.Pelvis&&organDepth>.7f)condition.spine=Mathf.Max(0,condition.spine-penetration);
  condition.pain=Mathf.Clamp(condition.pain+d*.6f,0,100);condition.shock=Mathf.Clamp(condition.shock+d*.15f,0,100);condition.blood=Mathf.Max(0,condition.blood-d*.12f);
  if(!playerBody&&kind==InjuryKind.Projectile&&organDepth>.7f&&d>24&&(IsArm(region)||IsLeg(region)||region==BodyRegion.Neck))r.externalBleed=Mathf.Min(2.2f,r.externalBleed+d*.01f);
  HitOccurred?.Invoke(region,d,point,direction);lastForce=direction.normalized*Mathf.Min(7,d*.09f);lastPoint=point;lastRegion=region;Evaluate();Changed?.Invoke();
 }
 void Update(){clock+=Time.deltaTime;if(clock>=.1f){float dt=clock;clock=0;Tick(dt);}}
 public void Tick(float dt){if(Dead||dt<=0)return;float pressure=playerBody?1:Mathf.Lerp(.45f,1,Mathf.Clamp01(condition.blood/75));condition.blood=Mathf.Max(0,condition.blood-Bleeding*pressure*dt);
 if(!playerBody){float oxygenTarget=Mathf.Min(condition.lungs,Mathf.Min(condition.blood*1.45f,condition.heart*1.5f));condition.oxygen=Mathf.MoveTowards(condition.oxygen,oxygenTarget,dt*(oxygenTarget<condition.oxygen?1.2f:.35f));if(condition.oxygen<35){condition.brain=Mathf.Max(0,condition.brain-dt*(35-condition.oxygen)*.035f);condition.shock=Mathf.Min(100,condition.shock+dt*.22f);}if(condition.heart<35&&condition.heart>0)condition.heart=Mathf.Max(0,condition.heart-dt*.10f);foreach(var wound in condition.regions)if(wound.internalBleed>0)wound.internalBleed=Mathf.Min(1.8f,wound.internalBleed+dt*.0015f*(1+(100-condition.liver)/100));}
float distress=(100-condition.lungs)*.003f+(100-condition.heart)*.006f+(condition.blood<55?.18f:0);condition.shock=Mathf.Clamp(condition.shock+dt*(distress-.035f),0,100);condition.pain=Mathf.Max(0,condition.pain-dt*.025f);Evaluate();}
 void Evaluate(){if(condition.blood<=12||condition.brain<=0||condition.heart<=0)condition.dead=true;if(Incapacitated&&!wasIncapacitated&&ragdoll)ragdoll.Collapse(lastRegion,lastPoint,lastForce);wasIncapacitated=Incapacitated;}
 public bool Bandage(BodyRegion region){var r=Region(region);if(r.externalBleed<=0)return false;r.externalBleed=0;r.bandaged=true;Changed?.Invoke();return true;}
 public bool Splint(BodyRegion region){var r=Region(region);if((!IsArm(region)&&!IsLeg(region))||r.bone>=40||r.splinted)return false;r.splinted=true;Changed?.Invoke();return true;}
 public BodyRegion WorstBleed(){BodyRegion region=BodyRegion.Chest;float worst=-1;foreach(var r in condition.regions)if(r.externalBleed>worst){worst=r.externalBleed;region=r.region;}return region;}
 public bool NeedsBandage=>Region(WorstBleed()).externalBleed>0;
 public string Summary(){if(Dead)return "DECEASED";string state=Downed?"INCAPACITATED":Bleeding>.001f?"BLEEDING":"STABLE";return state+"   Blood "+Mathf.RoundToInt(condition.blood)+"%   Pain "+Mathf.RoundToInt(condition.pain)+"%";}
 public string DetailedSummary(){var text=Summary()+"\nBrain "+condition.brain.ToString("F0")+" · Heart "+condition.heart.ToString("F0")+" · Lungs "+condition.lungs.ToString("F0")+"\nLiver "+condition.liver.ToString("F0")+" · Spine "+condition.spine.ToString("F0");foreach(var r in condition.regions)if(r.tissue<100||r.bone<100||r.muscle<100)text+="\n"+r.region+": tissue "+r.tissue.ToString("F0")+" / bone "+r.bone.ToString("F0")+" / muscle "+r.muscle.ToString("F0")+(r.externalBleed>0?" · bleeding":"")+(r.internalBleed>0?" · internal bleed":"")+(r.splinted?" · splinted":"");return text;}
 public void ResetForCheckpoint(){if(ragdoll)ragdoll.ResetBody();condition=new HumanCondition();wasIncapacitated=false;Changed?.Invoke();}
 public void ReceiveClinicalCare(){if(Dead)return;condition=new HumanCondition();wasIncapacitated=false;Changed?.Invoke();}
}
