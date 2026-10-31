using UnityEngine;
using System.Collections.Generic;

// Encounter-local breakaway leaf. The static doorway stays in place.
public class MedicalThornbackDoorBreak : MonoBehaviour {
 public bool Broken {get;private set;} public float BrokenAt {get;private set;} public int FragmentCount {get;private set;}
 public float FlashAge=>Broken?Time.time-BrokenAt:-1;
 MedicalInterior room;MedicalThornbackEffects fx;Transform door;Quaternion rest;float strain=-1;
 readonly List<GameObject> fragments=new List<GameObject>();Material core;PhysicsMaterial timberPhysics;
 public int SettledCount {get {int n=0;foreach(var go in fragments)if(go&&go.GetComponent<MedicalTimberSettler>().Settled)n++;return n;}}
 public void Configure(MedicalInterior interior,MedicalThornbackEffects effects){room=interior;fx=effects;door=room.jammedDoor;rest=door.localRotation;}
 public void Step(){
  if(!door||Broken||!room||room.finished)return;
  // Long arms reach the jamb first; the shoulder then loads the weakened leaf.
  float distance=Vector3.Dot(door.position-transform.position,transform.forward);
  if(distance<1.85f&&strain<0){strain=Time.time;fx.DoorStrain(door.position+Vector3.up*1.2f);}
  if(strain<0)return;
  float age=Time.time-strain;
  door.localRotation=rest*Quaternion.Euler(0,Mathf.Sin(age*16)*Mathf.Min(2.4f,age*2),Mathf.Sin(age*9)*.7f);
  if(distance<1.45f&&age>.65f)Break();
 }
 void Break(){
  if(Broken)return;Broken=true;BrokenAt=Time.time;
  foreach(var c in door.GetComponentsInChildren<Collider>())c.enabled=false;
  timberPhysics=new PhysicsMaterial("Dead timber - no bounce"){bounciness=0,staticFriction=.85f,dynamicFriction=.7f,bounceCombine=PhysicsMaterialCombine.Minimum};
  var leaf=door.GetComponentInChildren<MeshFilter>();
  if(leaf&&leaf.sharedMesh){
   var renderer=leaf.GetComponent<Renderer>();Material surface=renderer?renderer.sharedMaterial:null;
   core=new Material(Resources.Load<Material>("Medical/ThornbackPlaster"));core.name="Exposed splintered timber";core.SetColor("_BaseColor",new Color(.22f,.13f,.065f));
   Bounds b=leaf.sharedMesh.bounds;
   // Tile the leaf with shared jagged seams; retain its material and world transform.
   var grid=new Vector2[8,3];var rng=new System.Random(34);
   for(int x=0;x<8;x++)for(int y=0;y<3;y++)grid[x,y]=new Vector2(Mathf.Lerp(b.min.x,b.max.x,x/7f)+(x>0&&x<7?(float)(rng.NextDouble()-.5)*b.size.x*.09f:0),Mathf.Lerp(b.min.y,b.max.y,y/2f)+(y>0&&y<2?(float)(rng.NextDouble()-.5)*b.size.y*.12f:0));
   for(int x=0;x<7;x++)for(int y=0;y<2;y++){
    var corners=new[]{grid[x,y],grid[x+1,y],grid[x+1,y+1],grid[x,y+1]};
    var polygon=new List<Vector2>(corners);
    Spawn(leaf.transform,b,polygon,surface,rng);
   }
  }
  foreach(var r in door.GetComponentsInChildren<Renderer>())r.enabled=false;
  foreach(var c in door.GetComponentsInChildren<Collider>())c.enabled=false;
  fx.DoorBreak(door.position+Vector3.up*1.1f);
 }
 void Spawn(Transform source,Bounds bounds,List<Vector2> polygon,Material surface,System.Random rng){
  Vector2 center=Vector2.zero;foreach(var p in polygon)center+=p;center/=polygon.Count;
  float thick=Mathf.Max(.035f,bounds.size.z);float zmid=bounds.center.z;
  var mesh=Resources.Load<Mesh>("Medical/ThornbackDoorFragments/fragment-"+FragmentCount);
  if(!mesh){Debug.LogError("Missing baked door fragment "+FragmentCount);return;}
  var go=new GameObject("Broken door timber "+FragmentCount++);go.transform.SetParent(room.transform,true);go.transform.position=source.TransformPoint(new Vector3(center.x,center.y,zmid));go.transform.rotation=source.rotation;go.transform.localScale=new Vector3(source.lossyScale.x/room.transform.lossyScale.x,source.lossyScale.y/room.transform.lossyScale.y,source.lossyScale.z/room.transform.lossyScale.z);
  go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=new[]{surface?surface:core,core};var collider=go.AddComponent<BoxCollider>();collider.center=mesh.bounds.center;collider.size=mesh.bounds.size;collider.sharedMaterial=timberPhysics;
  var body=go.AddComponent<Rigidbody>();body.mass=.55f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearDamping=.8f;body.angularDamping=.65f;body.maxDepenetrationVelocity=.35f;
  body.linearVelocity=transform.forward*.12f+Vector3.down*.12f;
  body.angularVelocity=source.right*1.8f+Vector3.up*.1f;
  foreach(var previous in fragments)if(previous)Physics.IgnoreCollision(collider,previous.GetComponent<Collider>());
  go.AddComponent<MedicalTimberSettler>().Configure(room.transform.position.y);
  // Debris must not trap the player or slap the animated monster collider chain.
  foreach(var c in room.owner.player.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(collider,c);
  foreach(var c in transform.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(collider,c);
  fragments.Add(go);
 }
 void OnDestroy(){foreach(var go in fragments)if(go)Destroy(go);if(timberPhysics)Destroy(timberPhysics);if(core)Destroy(core);}
}
