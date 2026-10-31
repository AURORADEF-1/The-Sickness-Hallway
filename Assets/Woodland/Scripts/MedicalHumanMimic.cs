using UnityEngine;

// A finite, positional human lure. The first words precede the visible creature;
// the voice degrades during its approach and yields to the authored doorway roar.
public class MedicalHumanMimic : MonoBehaviour {
 public const float HiddenLeadIn=5.2f;
 public const float VoiceLevel=.72f;
 public bool InhumanLureLoaded{get;private set;}
 public bool InhumanReplyLoaded{get;private set;}
 public float PlayerDialogueGain {get;set;}=1;
 public int UnderlayPlayCount{get;private set;}
 public int UnderlayOverlapFrames{get;private set;}
 public bool UnderlayStartedHidden{get;private set;}
 public float MaximumUnderlayGain{get;private set;}
 public const float PostShotDelay=.45f;
 public bool PostShotQueued{get;private set;}
 public int PostShotPlayCount{get;private set;}
 public float PostShotRequestedAt{get;private set;}=-1;
 public float PostShotStartedAt{get;private set;}=-1;
 public bool PostShotFinished{get;private set;}
 public bool PostShotReplacedLure{get;private set;}
 public float PostShotElapsed=>PostShotStartedAt<0?0:Time.time-PostShotStartedAt;
 public float VoiceReverbDecay=>voice?voice.GetComponent<AudioReverbFilter>().decayTime:0;
 public float MaximumVoiceGain{get;private set;}
 public bool IsHidden {get;private set;}
 public bool Started {get;private set;}
 public bool VoiceEnded {get;private set;}
 public bool VoiceStartedHidden {get;private set;}
 public float FirstVoicePitch{get;private set;}
 public int HiddenColliderViolations{get;private set;}
 public float LowestPitch {get;private set;}=1;
 public float MaximumDistortion {get;private set;}
 public float MaximumBreath {get;private set;}
 public int HiddenRendererViolations {get;private set;}

 MedicalInterior room;
 AudioSource voice,breath,underlay;
 AudioClip postShotClip;bool postShotActive;float postShotClock;
 AudioDistortionFilter distortion;
 Renderer[] renderers;
 Collider[] colliders;
 bool[] rendererStates,colliderStates;
 readonly float[] speechSamples=new float[128];
 bool configured,stopped=true,released,yielding;
 float yieldAge,speechEnvelope;

 public void Configure(MedicalInterior interior,Transform visual){
  Stop();room=interior;
  renderers=visual?visual.GetComponentsInChildren<Renderer>(true):new Renderer[0];
  colliders=GetComponentsInChildren<Collider>(true);
  rendererStates=new bool[renderers.Length];colliderStates=new bool[colliders.Length];
  for(int i=0;i<renderers.Length;i++){rendererStates[i]=renderers[i].enabled;renderers[i].enabled=false;}
  for(int i=0;i<colliders.Length;i++){colliderStates[i]=colliders[i].enabled;colliders[i].enabled=false;}
  released=false;IsHidden=true;
  if(!voice){voice=Source("Thornback human lure",false);distortion=voice.gameObject.AddComponent<AudioDistortionFilter>();}
  if(!breath)breath=Source("Thornback mimic breathing",true);
  if(!underlay){underlay=Source("Recorded layer beneath human hello",false);underlay.GetComponent<MedicalCorridorAcoustics>().MaximumCutoffHz=1600;}
  underlay.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/human-lure-underlay");
  underlay.pitch=.88f;underlay.volume=0;
  UnderlayPlayCount=UnderlayOverlapFrames=0;UnderlayStartedHidden=false;MaximumUnderlayGain=0;
  // Baked duration-matched throat layers give the lure a different identity from
  // the dry, close MOTH-mask player lines without obscuring the first words.
  voice.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/monster-lure-inhuman");
  postShotClip=Resources.Load<AudioClip>("Medical/ThornbackFX/monster-postshot-inhuman");
  InhumanLureLoaded=voice.clip!=null;InhumanReplyLoaded=postShotClip!=null;
  PostShotQueued=postShotActive=PostShotFinished=PostShotReplacedLure=false;PostShotPlayCount=0;postShotClock=0;PostShotRequestedAt=PostShotStartedAt=-1;
  breath.clip=Resources.Load<AudioClip>("Medical/ThornbackFX/monster-breath-mimic");
  voice.pitch=1;voice.volume=0;breath.pitch=.92f;breath.volume=0;distortion.distortionLevel=.02f;
  Started=VoiceEnded=yielding=false;yieldAge=speechEnvelope=0;
  LowestPitch=1;MaximumDistortion=MaximumBreath=MaximumVoiceGain=0;HiddenRendererViolations=HiddenColliderViolations=0;VoiceStartedHidden=false;FirstVoicePitch=0;
  PlayerDialogueGain=1;configured=true;stopped=false;
 }

 AudioSource Source(string label,bool loop){
  var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*1.7f;
  var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=loop;
  MedicalCorridorAcoustics.Setup(source,room.owner.player.eye.transform);
  source.priority=90;
  if(!loop){
   // Reflections carry intelligible syllables down the corridor; distance is
   // still conveyed by position, filtered highs and the long wet tail.
   source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.1f,.95f),new Keyframe(.3f,.58f),new Keyframe(.65f,.25f),new Keyframe(1,.025f)));
   var reverb=source.GetComponent<AudioReverbFilter>();
   reverb.dryLevel=-350;reverb.room=-500;reverb.roomHF=-1900;
   reverb.decayTime=4.6f;reverb.decayHFRatio=.48f;
   reverb.reflectionsLevel=-550;reverb.reflectionsDelay=.085f;
   reverb.reverbLevel=100;reverb.reverbDelay=.12f;reverb.diffusion=96;reverb.density=96;
   source.GetComponent<MedicalCorridorAcoustics>().MaximumCutoffHz=3600;
   go.transform.localPosition=new Vector3(0,1.7f,-4.5f);
  }
  return source;
 }

 // Called by the actual successful rifle-shot event, not the hit callback.
 // A miss still provokes the creature; dry clicks and repeated shots do not replay it.
 public void AfterGunshot(){
  if(!configured||stopped||!room||room.finished||IsHidden||PostShotQueued||PostShotPlayCount>0||!postShotClip)return;
  var beat=GetComponent<MedicalDoorwayBeat>();
  if(beat&&beat.Roared)return;
  // Do not interrupt the final roar with a late reply or extend the existing ending.
  if(beat&&beat.Active&&beat.Age+PostShotDelay+postShotClip.length/.68f+.25f>=7.8f)return;
  PostShotQueued=true;PostShotRequestedAt=Time.time;postShotClock=0;
 }

 public void Tick(float encounterTime,float dt){
  if(!configured||stopped)return;
  if(!room||room.finished){Stop();return;}
  if(IsHidden){
   // Catch accidental activation by another presentation component during the lure.
   for(int i=0;i<renderers.Length;i++)if(renderers[i]&&renderers[i].enabled){HiddenRendererViolations++;renderers[i].enabled=false;}
   for(int i=0;i<colliders.Length;i++)if(colliders[i]){if(colliders[i].enabled)HiddenColliderViolations++;colliders[i].enabled=false;}
   if(encounterTime>=HiddenLeadIn)RestorePresentation();
  }
  var doorway=GetComponent<MedicalDoorwayBeat>();
  if(doorway&&doorway.Roared)yielding=true;
  if(yielding){
   yieldAge+=dt;float fade=1-Mathf.SmoothStep(0,1,yieldAge/.30f);
   if(underlay)underlay.Stop();
   voice.volume=Mathf.Min(voice.volume,VoiceLevel*fade);breath.volume=Mathf.Min(breath.volume,.34f*fade);
   if(yieldAge>=.30f){voice.Stop();breath.Stop();VoiceEnded=true;stopped=true;}
   return;
  }
  // Defer a fraction of a second so capture/listener initialization cannot eat the onset.
  if(!Started&&encounterTime>=.12f&&voice.clip){
   voice.Play();Started=true;VoiceStartedHidden=IsHidden;FirstVoicePitch=voice.pitch;
   if(underlay.clip){underlay.Play();UnderlayPlayCount++;UnderlayStartedHidden=IsHidden;}
  }
  if(Started&&!voice.isPlaying){VoiceEnded=true;if(postShotActive)PostShotFinished=true;}
  if(PostShotQueued){
   postShotClock+=dt;
   if(postShotClock>=PostShotDelay){
    underlay.Stop();voice.Stop();voice.clip=postShotClip;VoiceEnded=false;postShotActive=true;PostShotQueued=false;
    PostShotReplacedLure=true;PostShotStartedAt=Time.time;PostShotPlayCount++;
    voice.pitch=.84f;voice.volume=0;distortion.distortionLevel=.10f;voice.Play();
   }
  }

  float distance=Vector3.Distance(transform.position,room.owner.player.eye.transform.position);
  float near=Mathf.InverseLerp(10,3,distance);
  float drop=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,15,encounterTime));
  drop=Mathf.Clamp01(drop*(1+near*.15f));
  float wobble=(Mathf.Sin(encounterTime*5.13f)+.45f*Mathf.Sin(encounterTime*8.41f))*.0018f*(1-drop);
  voice.pitch=Mathf.Lerp(1,.60f,drop)+wobble;
  voice.transform.localPosition=new Vector3(0,1.7f,Mathf.Lerp(-4.5f,-1.5f,drop));
  underlay.transform.localPosition=voice.transform.localPosition;
  // The supplied texture accompanies only the opening call, never the later reply/roar.
  if(underlay.isPlaying){
   underlay.volume=.26f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.25f,encounterTime));
   MaximumUnderlayGain=Mathf.Max(MaximumUnderlayGain,underlay.volume);
   if(voice.isPlaying&&!postShotActive)UnderlayOverlapFrames++;
  }
  distortion.distortionLevel=Mathf.Lerp(.02f,.35f,drop);
  if(postShotActive){
   float response=Mathf.SmoothStep(0,1,Mathf.Clamp01(PostShotElapsed/1.6f));
   voice.pitch=Mathf.Lerp(.84f,.68f,response);
   distortion.distortionLevel=Mathf.Lerp(.10f,.27f,response);
  }
  if(Started&&!VoiceEnded){
   voice.volume=VoiceLevel*Mathf.Clamp01(PlayerDialogueGain)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.28f,encounterTime));
   if(PostShotQueued)voice.volume*=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(postShotClock/.18f));
   if(postShotActive)voice.volume*=Mathf.SmoothStep(0,1,PostShotElapsed/.06f);
   MaximumVoiceGain=Mathf.Max(MaximumVoiceGain,voice.volume);
   LowestPitch=Mathf.Min(LowestPitch,voice.pitch);
   MaximumDistortion=Mathf.Max(MaximumDistortion,distortion.distortionLevel);
  }

  // Side-chain the creature's breath under loud syllables, keeping the lure legible.
  float rms=0;
  if(Started&&!VoiceEnded){voice.GetOutputData(speechSamples,0);for(int i=0;i<speechSamples.Length;i++)rms+=speechSamples[i]*speechSamples[i];rms=Mathf.Sqrt(rms/speechSamples.Length);}
  speechEnvelope=Mathf.Lerp(speechEnvelope,rms,1-Mathf.Exp(-dt*(rms>speechEnvelope?22:7)));
  float duck=Mathf.Lerp(1,.30f,Mathf.InverseLerp(.015f,.09f,speechEnvelope));
  float breathEnvelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.8f,9,encounterTime));
  float breathLevel=Mathf.Lerp(.035f,.34f,drop)*breathEnvelope*duck*Mathf.Lerp(.55f,1,Mathf.Clamp01(PlayerDialogueGain));
  if(encounterTime>=2.8f&&breath.clip&&!breath.isPlaying)breath.Play();
  breath.volume=breathLevel;breath.pitch=Mathf.Lerp(.92f,.78f,drop);
  MaximumBreath=Mathf.Max(MaximumBreath,breathLevel);
 }

 void RestorePresentation(){
  if(released)return;released=true;IsHidden=false;
  if(renderers!=null)for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=rendererStates[i];
  if(colliders!=null)for(int i=0;i<colliders.Length;i++)if(colliders[i])colliders[i].enabled=colliderStates[i];
 }
 public void Stop(){
  stopped=true;PostShotQueued=false;if(underlay)underlay.Stop();if(voice)voice.Stop();if(breath)breath.Stop();if(Started)VoiceEnded=true;
  RestorePresentation();
 }
 void OnDisable(){Stop();}
 void OnDestroy(){if(underlay)Destroy(underlay.gameObject);if(voice)Destroy(voice.gameObject);if(breath)Destroy(breath.gameObject);}
}
