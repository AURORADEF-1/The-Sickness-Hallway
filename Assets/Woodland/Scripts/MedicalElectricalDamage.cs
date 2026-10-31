using UnityEngine;
using System.Collections.Generic;

// Small contact-local effects, with a fuse-out after each damaged fitting.
public class MedicalElectricalDamage:MonoBehaviour {
 class Cable {public Vector3 anchor,contact;public LineRenderer line;public float tornAt=-1;public Transform hand;public AudioSource audio;public bool secondary;}
 Transform[] tips;MedicalInterior room;Material insulation,hot;ParticleSystem sparks;Light arc;AudioClip crackle;float arcUntil;bool stopped;
 readonly List<Cable> cables=new List<Cable>();readonly HashSet<Light> broken=new HashSet<Light>();readonly List<GameObject> owned=new List<GameObject>();
 public int WireCount {get;private set;}public int CeilingCount {get;private set;}public float LastContact {get;private set;}public bool IsBroken(Light lamp)=>broken.Contains(lamp);
 public void Configure(MedicalInterior interior){room=interior;stopped=false;var hands=new List<Transform>();foreach(var t in GetComponentsInChildren<Transform>())if(t.name.StartsWith("Corridor claw contact"))hands.Add(t);tips=hands.ToArray();crackle=Resources.Load<AudioClip>("Medical/ThornbackFX/electrical-short");
  insulation=new Material(Resources.Load<Material>("Medical/ThornbackPlaster"));insulation.SetColor("_BaseColor",new Color(.018f,.022f,.019f));hot=new Material(Resources.Load<Material>("Medical/ThornbackSpark"));
  var g=new GameObject("Contact sparks");g.transform.SetParent(room.transform,false);g.SetActive(false);owned.Add(g);sparks=g.AddComponent<ParticleSystem>();
  var main=sparks.main;main.loop=false;main.playOnAwake=false;main.maxParticles=90;main.startLifetime=new ParticleSystem.MinMaxCurve(.14f,.38f);main.startSize=new ParticleSystem.MinMaxCurve(.004f,.012f);main.gravityModifier=1.6f;main.simulationSpace=ParticleSystemSimulationSpace.World;
  var emission=sparks.emission;emission.enabled=false;var shape=sparks.shape;shape.enabled=false;var color=sparks.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.85f,.45f),0),new GradientColorKey(new Color(1,.25f,.025f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
  var renderer=g.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=hot;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=1.8f;renderer.velocityScale=.09f;
  g.SetActive(true);sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var flash=new GameObject("Short circuit light");owned.Add(flash);flash.transform.SetParent(room.transform,false);arc=flash.AddComponent<Light>();arc.type=LightType.Point;arc.range=1.6f;arc.color=new Color(.65f,.80f,1);arc.shadows=LightShadows.None;arc.enabled=false;
  for(int n=0;n<3;n++){float sign=n%2==0?-1:1;Vector3 origin=room.transform.TransformPoint(new Vector3(0,1.45f,31+n*1.55f));Vector3 contact=origin+Vector3.right*sign*1.26f;
   foreach(var hit in Physics.RaycastAll(origin,Vector3.right*sign,1.5f,~0,QueryTriggerInteraction.Ignore)){if(hit.transform.IsChildOf(transform))continue;if(Mathf.Abs(hit.normal.x)>.8f){contact=hit.point+hit.normal*.02f;break;}}
   var cable=new Cable{contact=contact,anchor=contact+Vector3.up*.9f};var wire=new GameObject("Wall cable pulled from clips "+n);wire.transform.SetParent(room.transform,false);owned.Add(wire);cable.line=wire.AddComponent<LineRenderer>();cable.line.sharedMaterial=insulation;cable.line.positionCount=12;cable.line.startWidth=cable.line.endWidth=.016f;cable.line.useWorldSpace=true;cable.line.numCapVertices=3;cable.audio=MakeAudio(wire);cables.Add(cable);Draw(cable,0);
  }
 }
 AudioSource MakeAudio(GameObject go){var a=go.AddComponent<AudioSource>();a.playOnAwake=false;a.volume=.35f;MedicalCorridorAcoustics.Setup(a,room.owner.player.eye.transform);return a;}
 void Short(Vector3 point,AudioSource source){LastContact=Time.time;source.transform.position=point;if(crackle)source.PlayOneShot(crackle,.7f);
  for(int i=0;i<24;i++)sparks.Emit(new ParticleSystem.EmitParams{position=point,velocity=new Vector3(Random.Range(-1.3f,1.3f),Random.Range(-1.7f,.3f),Random.Range(-.8f,.8f))},1);
  arc.transform.position=point;arcUntil=Time.time+.18f;arc.enabled=true;
 }
 void Draw(Cable c,float age){Vector3 end=c.contact;
  if(c.tornAt>=0){float pull=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.55f));Vector3 tug=c.hand?c.hand.position:c.contact+Vector3.back*.25f;
   end=age<.7f?Vector3.Lerp(c.contact,tug,pull):c.anchor+Vector3.down*.83f+Vector3.right*Mathf.Sin(age*7)*.20f*Mathf.Exp(-age*.45f)+Vector3.back*.12f;
  }
  for(int i=0;i<12;i++){float t=i/11f;Vector3 p=Vector3.Lerp(c.anchor,end,t);if(c.tornAt>=0)p+=Vector3.down*Mathf.Sin(t*Mathf.PI)*.12f;c.line.SetPosition(i,p);}
 }
 void Update(){if(!room||stopped)return;float now=Time.time;if(arc){arc.enabled=now<arcUntil;arc.intensity=arc.enabled?(Mathf.Sin(now*107)>0?1.6f:.2f):0;}
  if(room.finished){StopEffects();return;}
  foreach(var c in cables){if(c.tornAt<0){foreach(var tip in tips){if(Vector3.Distance(tip.position,c.contact)<.48f){c.tornAt=now;c.hand=tip;WireCount++;Short(c.contact,c.audio);break;}}}else{float age=now-c.tornAt;if(age>.7f&&!c.secondary){c.secondary=true;Short(c.anchor,c.audio);}Draw(c,age);}}
  // Swept upper-back clearance reaches existing low ceiling fittings in order.
  foreach(var lamp in room.lights){if(!lamp||broken.Contains(lamp)||lamp.transform.position.y<room.transform.position.y+2.1f)continue;
   Vector3 d=lamp.transform.position-(transform.position+Vector3.up*2.55f+transform.forward*.4f);
   if(Mathf.Abs(d.z)<.55f&&Mathf.Abs(d.x)<.9f&&Mathf.Abs(d.y)<.65f){broken.Add(lamp);CeilingCount++;var g=new GameObject("Torn fixture wiring");g.transform.SetParent(room.transform,false);owned.Add(g);var a=MakeAudio(g);Short(lamp.transform.position,a);lamp.enabled=false;
    var wire=g.AddComponent<LineRenderer>();wire.sharedMaterial=insulation;wire.useWorldSpace=true;wire.positionCount=4;wire.startWidth=wire.endWidth=.014f;var at=lamp.transform.position;wire.SetPositions(new[]{at+Vector3.up*.12f,at,at+Vector3.down*.32f+Vector3.right*.12f,at+Vector3.down*.6f+Vector3.right*.08f});
   }
  }
 }
 public void StopEffects(){stopped=true;if(arc)arc.enabled=false;if(sparks)sparks.Stop(true,ParticleSystemStopBehavior.StopEmitting);foreach(var go in owned)if(go){var a=go.GetComponent<AudioSource>();if(a)a.Stop();}}
 void OnDisable(){StopEffects();}
 void OnDestroy(){foreach(var go in owned)if(go)Destroy(go);if(insulation)Destroy(insulation);if(hot)Destroy(hot);}
}
