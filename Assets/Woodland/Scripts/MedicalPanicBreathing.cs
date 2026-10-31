using UnityEngine;
[DefaultExecutionOrder(1100)] public class MedicalPanicBreathing:MonoBehaviour {
 MedicalInterior room;WoodlandPlayer player;AudioSource breath;float[] envelope;public float MaxFog {get;private set;}
 public void Configure(MedicalInterior interior){room=interior;player=room.owner.player;var clip=Resources.Load<AudioClip>("Medical/ThornbackFX/panic-breathing");
  if(!breath){var holder=new GameObject("Quiet breathing inside respirator");holder.transform.SetParent(player.eye.transform,false);holder.SetActive(false);breath=holder.AddComponent<AudioSource>();breath.playOnAwake=false;breath.spatialBlend=0;breath.reverbZoneMix=0;breath.bypassReverbZones=true;breath.loop=true;breath.clip=clip;holder.SetActive(true);}
  breath.clip=clip;breath.volume=0;
  if(clip){var pcm=new float[clip.samples*clip.channels];clip.GetData(pcm,0);int window=Mathf.Max(1,clip.frequency*clip.channels/25);envelope=new float[Mathf.CeilToInt((float)pcm.Length/window)];float max=.001f;
   for(int i=0;i<envelope.Length;i++){float sum=0;int end=Mathf.Min(pcm.Length,(i+1)*window);for(int j=i*window;j<end;j++)sum+=pcm[j]*pcm[j];envelope[i]=Mathf.Sqrt(sum/window);max=Mathf.Max(max,envelope[i]);}for(int i=0;i<envelope.Length;i++)envelope[i]/=max;breath.Play();}
 }
 void LateUpdate(){if(!player||!breath)return;bool active=room&&room.chasing&&!room.finished;float distance=active?Vector3.Distance(room.thornback.position,player.transform.position):20;
  breath.volume=Mathf.MoveTowards(breath.volume,active?Mathf.Lerp(.11f,.19f,Mathf.InverseLerp(12,3,distance)):0,Time.deltaTime*.2f);
  if(player.visorEffects){float signal=active&&envelope!=null&&envelope.Length>0?envelope[Mathf.Clamp(Mathf.FloorToInt(breath.time*25),0,envelope.Length-1)]:0;
   float target=player.visorEffects.Busy?0:signal*.045f;player.visorEffects.panicFog=Mathf.MoveTowards(player.visorEffects.panicFog,target,Time.deltaTime*(target>player.visorEffects.panicFog?.06f:.025f));MaxFog=Mathf.Max(MaxFog,player.visorEffects.panicFog);player.visorEffects.ApplyVisual();}
 }
 public void StopBreathing(){if(breath)breath.Stop();if(player&&player.visorEffects){player.visorEffects.panicFog=0;player.visorEffects.ApplyVisual();}}
 void OnDisable(){StopBreathing();}
 void OnDestroy(){if(breath)Destroy(breath.gameObject);}
}
