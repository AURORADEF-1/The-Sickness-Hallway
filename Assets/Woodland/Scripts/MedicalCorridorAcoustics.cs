using UnityEngine;
public class MedicalCorridorAcoustics:MonoBehaviour {
 public float MaximumCutoffHz=22000;
 Transform listener;WoodlandVisorEffects hearing;AudioLowPassFilter low;public float Distance {get;private set;}public float DistanceGain {get;private set;}
 public static void Setup(AudioSource source,Transform ear){
  source.spatialBlend=1;source.dopplerLevel=0;source.minDistance=1;source.maxDistance=28;source.rolloffMode=AudioRolloffMode.Custom;
  source.SetCustomCurve(AudioSourceCurveType.CustomRolloff,new AnimationCurve(new Keyframe(0,1),new Keyframe(.1f,.90f),new Keyframe(.3f,.30f),new Keyframe(.65f,.07f),new Keyframe(1,0)));
  source.reverbZoneMix=0;
  var reverb=source.GetComponent<AudioReverbFilter>();if(!reverb)reverb=source.gameObject.AddComponent<AudioReverbFilter>();reverb.reverbPreset=AudioReverbPreset.User;reverb.dryLevel=0;reverb.room=-1600;reverb.roomHF=-2100;reverb.decayTime=2.6f;reverb.decayHFRatio=.55f;reverb.reflectionsLevel=-1500;reverb.reflectionsDelay=.055f;reverb.reverbLevel=-1400;reverb.reverbDelay=.085f;reverb.diffusion=90;reverb.density=85;
  var component=source.GetComponent<MedicalCorridorAcoustics>();if(!component)component=source.gameObject.AddComponent<MedicalCorridorAcoustics>();component.listener=ear;component.hearing=ear.GetComponentInParent<WoodlandVisorEffects>();component.low=source.GetComponent<AudioLowPassFilter>();if(!component.low)component.low=source.gameObject.AddComponent<AudioLowPassFilter>();
 }
 void Update(){if(!listener||!low)return;Distance=Vector3.Distance(transform.position,listener.position);low.cutoffFrequency=Mathf.Min(MaximumCutoffHz,Mathf.Min(Mathf.Lerp(11500,2800,Mathf.InverseLerp(2,14,Distance)),hearing?Mathf.Lerp(20000,3900,hearing.RingIntensity):22000));DistanceGain=GetComponent<AudioSource>().GetCustomCurve(AudioSourceCurveType.CustomRolloff).Evaluate(Distance/28);}
}
