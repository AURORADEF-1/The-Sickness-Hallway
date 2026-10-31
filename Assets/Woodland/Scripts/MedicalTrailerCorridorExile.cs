using UnityEngine;
using System.Collections.Generic;

// A visual-only performance for the trailer prelude. No combat, health, ragdoll,
// audio, or AI behaviours are copied from the existing authored exile.
public sealed class MedicalTrailerCorridorExile : MonoBehaviour {
 public const float FlickerAt=2.4f,BlackoutAt=2.93f,DisappearAt=3.1f;
 public bool Configured{get;private set;}
 public bool Visible{get;private set;}
 public bool TorchOn=>torch&&torch.enabled&&torch.intensity>.001f;
 public bool Vanished{get;private set;}
 public bool WasVisible{get;private set;}
 public bool DisappearedAfterLampBlackout{get;private set;}
 public float VanishedAt{get;private set;}=-1;
 public float TorchIntensity=>torch?torch.intensity:0;
 public int VisibleFrames{get;private set;}
 public int FlickerDipFrames{get;private set;}
 public int SkinnedRendererCount{get;private set;}
 public Transform Actor=>actor;

 Transform actor,head,spine,torchMount;
 Light torch,reflectorSpill;
 Material housingMaterial,lensMaterial;
 readonly Dictionary<Transform,Transform> clones=new Dictionary<Transform,Transform>();
 readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
 readonly Dictionary<Transform,Quaternion> rest=new Dictionary<Transform,Quaternion>();

 public bool Configure(MedicalInterior room,WoodlandPlayer player){
  if(Configured)return true;
  if(!room||!room.bleedingExile||!player)return false;
  var source=room.bleedingExile;
  actor=CopyHierarchy(source,transform);
  actor.name="Distant corridor exile — visual only";
  actor.position=new Vector3(room.transform.position.x+.35f,room.transform.position.y+.04f,33.6f);
  actor.rotation=Quaternion.identity;
  actor.localScale=source.lossyScale;
  actor.gameObject.SetActive(true);
  CopyRenderers(source);
  // Sample the existing skeletal idle directly. This keeps the supplied human
  // bone proportions without instantiating its gameplay Animator or behaviours.
  foreach(var sourceAnimator in source.GetComponentsInChildren<Animator>(true)){
   if(!sourceAnimator.runtimeAnimatorController||!clones.TryGetValue(sourceAnimator.transform,out var model))continue;
   AnimationClip idle=null;
   foreach(var clip in sourceAnimator.runtimeAnimatorController.animationClips)
    if(clip&&(clip.name.ToLowerInvariant().Contains("standing rest")||clip.name.ToLowerInvariant().Contains("idle"))){idle=clip;break;}
   if(idle)idle.SampleAnimation(model.gameObject,0);
  }
  foreach(var t in actor.GetComponentsInChildren<Transform>(true)){
   if(t.name.StartsWith("mixamorig:"))bones[t.name.Substring(10)]=t;
   rest[t]=t.localRotation;
  }
  head=Bone("Head");spine=Bone("Spine2");
  // The rigged character is already metre-scaled. Keep its feet on this floor
  // after sampling, rather than adding an arbitrary height offset to the rig.
  float lowest=float.MaxValue;
  foreach(var r in actor.GetComponentsInChildren<Renderer>(true))if(r.enabled)lowest=Mathf.Min(lowest,r.bounds.min.y);
  if(lowest<float.MaxValue)actor.position+=Vector3.up*(room.transform.position.y+.045f-lowest);
  MakeTorch(player);
  Configured=true;Visible=true;Tick(0);
  return true;
 }

 Transform CopyHierarchy(Transform source,Transform parent){
  var copy=new GameObject(source.name).transform;
  copy.SetParent(parent,false);copy.localPosition=source.localPosition;
  copy.localRotation=source.localRotation;copy.localScale=source.localScale;
  copy.gameObject.layer=source.gameObject.layer;copy.gameObject.SetActive(source.gameObject.activeSelf);
  clones[source]=copy;
  foreach(Transform child in source)CopyHierarchy(child,copy);
  return copy;
 }

 void CopyRenderers(Transform source){
  foreach(var original in source.GetComponentsInChildren<Renderer>(true)){
   if(!clones.TryGetValue(original.transform,out var target))continue;
   Renderer copy=null;
   if(original is SkinnedMeshRenderer skin){
    var r=target.gameObject.AddComponent<SkinnedMeshRenderer>();
    r.sharedMesh=skin.sharedMesh;r.localBounds=skin.localBounds;r.quality=skin.quality;
    var mapped=new Transform[skin.bones.Length];
    for(int i=0;i<mapped.Length;i++)if(skin.bones[i])clones.TryGetValue(skin.bones[i],out mapped[i]);
    r.bones=mapped;if(skin.rootBone&&clones.TryGetValue(skin.rootBone,out var rootBone))r.rootBone=rootBone;
    r.updateWhenOffscreen=true;SkinnedRendererCount++;copy=r;
   }else if(original is MeshRenderer){
    var mesh=original.GetComponent<MeshFilter>();if(!mesh||!mesh.sharedMesh)continue;
    target.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
    copy=target.gameObject.AddComponent<MeshRenderer>();
   }
   if(!copy)continue;
   copy.sharedMaterials=original.sharedMaterials;copy.enabled=original.enabled;
   copy.shadowCastingMode=original.shadowCastingMode;copy.receiveShadows=original.receiveShadows;
   copy.lightProbeUsage=original.lightProbeUsage;copy.reflectionProbeUsage=original.reflectionProbeUsage;
  }
 }

 Transform Bone(string name){bones.TryGetValue(name,out var bone);return bone;}

 void MakeTorch(WoodlandPlayer player){
  var shader=Shader.Find("Universal Render Pipeline/Lit");
  housingMaterial=new Material(shader);housingMaterial.color=new Color(.045f,.053f,.045f);
  housingMaterial.SetFloat("_Metallic",.45f);housingMaterial.SetFloat("_Smoothness",.3f);
  lensMaterial=new Material(shader);lensMaterial.color=new Color(.72f,.84f,.88f);
  lensMaterial.EnableKeyword("_EMISSION");
  torchMount=new GameObject("Exile headtorch and reflected spill").transform;
  torchMount.SetParent(head?head:actor,true);
  torchMount.position=head?head.position+Vector3.up*.12f+actor.forward*.13f:actor.position+Vector3.up*1.65f;
  torchMount.rotation=Quaternion.LookRotation(actor.forward+Vector3.down*.24f);
  AddPiece("Rubber headtorch body",PrimitiveType.Cube,Vector3.zero,new Vector3(.11f,.055f,.045f),housingMaterial);
  AddPiece("Headtorch glass emitter",PrimitiveType.Sphere,new Vector3(0,0,.025f),new Vector3(.035f,.035f,.011f),lensMaterial);
  torch=torchMount.gameObject.AddComponent<Light>();torch.type=LightType.Spot;
  torch.spotAngle=45;torch.innerSpotAngle=19;torch.range=12;
  torch.color=new Color(.82f,.9f,1);torch.intensity=6.3f;torch.shadows=LightShadows.Hard;
  torch.cookie=player.torch?player.torch.cookie:null;
  var spill=new GameObject("Exile headtorch local reflector spill");spill.transform.SetParent(torchMount,false);
  spill.transform.localPosition=new Vector3(0,-.08f,.10f);
  reflectorSpill=spill.AddComponent<Light>();reflectorSpill.type=LightType.Point;
  reflectorSpill.color=torch.color;reflectorSpill.range=1.4f;reflectorSpill.intensity=.32f;
  reflectorSpill.shadows=LightShadows.None;
 }

 void AddPiece(string name,PrimitiveType shape,Vector3 position,Vector3 scale,Material material){
  var piece=GameObject.CreatePrimitive(shape);piece.name=name;piece.transform.SetParent(torchMount,false);
  piece.transform.localPosition=position;piece.transform.localScale=scale;
  Destroy(piece.GetComponent<Collider>());piece.GetComponent<Renderer>().sharedMaterial=material;
 }

 // Parent owns the ceiling blackout and the scream/player-voice timeline. Calling
 // this before the render step makes lamp-off -> dark pause -> disappearance exact.
 public void Tick(float age){
  if(!Configured||Vanished)return;
  if(age>=DisappearAt){
   DisappearedAfterLampBlackout=!TorchOn;
   SetLamp(0);actor.gameObject.SetActive(false);Visible=false;Vanished=true;VanishedAt=age;
   return;
  }
  Visible=true;WasVisible=true;VisibleFrames++;
  foreach(var pair in rest)if(pair.Key)pair.Key.localRotation=pair.Value;
  float breath=Mathf.Sin(age*3.4f);
  if(spine)spine.localRotation*=Quaternion.Euler(5+breath*.7f,0,1.6f);
  if(head)head.localRotation*=Quaternion.Euler(-3,Mathf.Sin(age*.9f)*5,1.5f);
  // A guarded, human stance: one hand at the sternum, the other lowered, with
  // original bone lengths retained by analytic two-bone IK. No T-pose silhouette.
  Solve("RightArm","RightForeArm","RightHand",actor.TransformPoint(new Vector3(.18f,1.18f,.18f)),actor.TransformPoint(new Vector3(.48f,.95f,.02f)));
  Solve("LeftArm","LeftForeArm","LeftHand",actor.TransformPoint(new Vector3(-.22f,.83f,.10f)),actor.TransformPoint(new Vector3(-.42f,.93f,.02f)));
  if(torchMount)torchMount.rotation=Quaternion.LookRotation(actor.forward+Vector3.down*.24f+actor.right*Mathf.Sin(age*.8f)*.07f);
  float level=1;
  if(age>=BlackoutAt)level=0;
  else if(age>=FlickerAt){
   float t=age-FlickerAt;
   level=t<.08f?.04f:t<.16f?.85f:t<.25f?0:t<.31f?.58f:t<.42f?.02f:.3f;
   if(level<.1f)FlickerDipFrames++;
  }else level=.93f+.07f*Mathf.Sin(age*2.7f);
  SetLamp(level);
 }

 void SetLamp(float level){
  if(torch){torch.enabled=level>0;torch.intensity=6.3f*level;}
  if(reflectorSpill){reflectorSpill.enabled=level>0;reflectorSpill.intensity=.32f*level;}
  if(lensMaterial)lensMaterial.SetColor("_EmissionColor",new Color(.56f,.76f,1)*level*4);
 }

 void Solve(string upperName,string lowerName,string endName,Vector3 target,Vector3 pole){
  var upper=Bone(upperName);var lower=Bone(lowerName);var end=Bone(endName);
  if(!upper||!lower||!end)return;
  float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,end.position);
  var delta=target-upper.position;if(a<=.001f||b<=.001f||delta.sqrMagnitude<.0001f)return;
  var axis=delta.normalized;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.005f,a+b-.005f);
  var bend=Vector3.ProjectOnPlane(pole-upper.position,axis).normalized;if(bend.sqrMagnitude<.01f)bend=actor.right;
  float along=(a*a+d*d-b*b)/(2*d);
  var joint=upper.position+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
  upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,joint-upper.position)*upper.rotation;
  lower.rotation=Quaternion.FromToRotation(end.position-lower.position,target-lower.position)*lower.rotation;
 }

 public void HideImmediately(){SetLamp(0);if(actor)actor.gameObject.SetActive(false);Visible=false;}
 void OnDisable(){HideImmediately();}
 void OnDestroy(){if(actor)Destroy(actor.gameObject);if(housingMaterial)Destroy(housingMaterial);if(lensMaterial)Destroy(lensMaterial);}
}
