using UnityEngine;
// A bounded voice pool; every shot keeps its source position as the shooter moves.
public class WoodlandGunshotAcoustics:MonoBehaviour {
 class Voice {public AudioSource source;public AudioLowPassFilter low;public AudioReverbFilter reverb;public AudioEchoFilter echo;public float until,nextCheck;public bool local,indoor;public Transform owner;public Vector3 position;}
 readonly Voice[] voices=new Voice[16];int cursor;WoodlandPlayer player;WoodlandMedicalSequence medical;
 public static bool PlayerIndoors(WoodlandPlayer player){var flow=Object.FindFirstObjectByType<WoodlandMedicalSequence>();return flow&&flow.inside;}
 public static float DistanceCutoff(float distance,bool blocked)=>Mathf.Lerp(19000,1800,Mathf.Clamp01(distance/120))*(blocked?.35f:1);
 public static void Play(WoodlandPlayer listener,Vector3 position,AudioClip clip,bool local,bool indoor,Transform owner=null){if(!listener||!clip)return;var mix=listener.GetComponent<WoodlandGunshotAcoustics>();if(!mix){mix=listener.gameObject.AddComponent<WoodlandGunshotAcoustics>();mix.player=listener;}mix.Emit(position,clip,local,indoor,owner);}
 void Emit(Vector3 position,AudioClip clip,bool local,bool indoor,Transform owner){
 if(!medical)medical=Object.FindFirstObjectByType<WoodlandMedicalSequence>();
 int index=cursor++%voices.Length;var v=voices[index];if(v==null){var g=new GameObject("Gunshot acoustic voice "+index);g.transform.SetParent(transform);v=new Voice{source=g.AddComponent<AudioSource>(),low=g.AddComponent<AudioLowPassFilter>(),reverb=g.AddComponent<AudioReverbFilter>(),echo=g.AddComponent<AudioEchoFilter>()};voices[index]=v;v.source.playOnAwake=false;v.source.dopplerLevel=0;v.source.rolloffMode=AudioRolloffMode.Logarithmic;v.source.minDistance=4;v.source.maxDistance=180;}
 v.source.Stop();v.position=position;v.source.transform.position=position;v.local=local;v.indoor=indoor;v.owner=owner;v.source.clip=clip;v.source.spatialBlend=local?0:1;v.source.pitch=Random.Range(.97f,1.025f);v.source.volume=local?.72f:.85f;
 v.reverb.reverbPreset=indoor?AudioReverbPreset.StoneCorridor:AudioReverbPreset.Forest;
 v.reverb.dryLevel=0;v.reverb.room=indoor?-1400:-2600;v.reverb.decayTime=indoor?1.65f:.65f;v.reverb.reverbLevel=indoor?-1000:-2200;
 v.echo.delay=indoor?74:175;v.echo.decayRatio=indoor?.28f:.18f;v.echo.wetMix=indoor?.14f:.075f;v.echo.dryMix=1;
 float distance=local?0:Vector3.Distance(position,player.eye.transform.position);v.until=Time.time+clip.length/v.source.pitch+2.2f+distance/343;v.nextCheck=0;Filter(v);v.source.PlayDelayed(local?0:Mathf.Min(distance/343,.6f));
 }
 void Filter(Voice v){float d=v.local?0:Vector3.Distance(v.source.transform.position,player.eye.transform.position);bool blocked=false;if(!v.local){Vector3 from=v.source.transform.position,to=player.eye.transform.position;foreach(var h in Physics.RaycastAll(from,(to-from).normalized,d,~0,QueryTriggerInteraction.Ignore)){if(h.collider.transform.IsChildOf(player.transform)||(v.owner&&h.collider.transform.IsChildOf(v.owner))||h.collider.GetComponentInParent<WoodlandAnatomy>())continue;blocked=true;break;}if(medical&&medical.inside!=v.indoor)blocked=true;}v.low.cutoffFrequency=DistanceCutoff(d,blocked);v.source.volume=(v.local?.72f:.85f)*(blocked?.55f:1);v.nextCheck=Time.time+.15f;}
 void LateUpdate(){if(WoodlandDemoControls.Paused||!player)return;foreach(var v in voices){if(v==null||Time.time>=v.until)continue;if(v.local)v.source.transform.position=player.eye.transform.position;else {v.source.transform.position=v.position;if(Time.time>=v.nextCheck)Filter(v);}}}
}
