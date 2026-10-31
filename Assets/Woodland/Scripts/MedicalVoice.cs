using UnityEngine;
public class MedicalVoice:MonoBehaviour {
 bool encounterSilent;public bool SilencedForEncounter=>encounterSilent;
 public void SilenceForEncounter(){encounterSilent=true;if(source)source.Stop();}
 public AudioClip[] pain=new AudioClip[0];public AudioClip effort,death;WoodlandAnatomy body;WoodlandHostile hostile;AudioSource source;float next;int shots;bool dead;
 void Start(){body=GetComponent<WoodlandAnatomy>();hostile=GetComponent<WoodlandHostile>();source=gameObject.AddComponent<AudioSource>();source.spatialBlend=1;source.minDistance=1.5f;source.maxDistance=15;source.volume=.55f;source.rolloffMode=AudioRolloffMode.Linear;source.playOnAwake=false;if(body)body.HitOccurred+=Hit;}
 public void Plead(AudioClip clip){if(encounterSilent||!clip||!source)return;source.Stop();source.PlayOneShot(clip,1.25f);next=Time.time+clip.length+.3f;}
 void Hit(BodyRegion r,float d,Vector3 p,Vector3 dir){if(encounterSilent||Time.time<next||(pain==null||pain.Length==0))return;source.PlayOneShot(pain[Random.Range(0,pain.Length)]);next=Time.time+2;}
 void Update(){if(encounterSilent)return;var crawl=GetComponent<MedicalCrawlingExile>();if(crawl&&crawl.started&&!crawl.stopped&&Time.time>next&&pain!=null&&pain.Length>0){source.PlayOneShot(pain[Random.Range(0,pain.Length)],.5f);next=Time.time+5;}if(body&&body.Dead&&!dead){dead=true;if(death)source.PlayOneShot(death,.65f);}if(hostile&&hostile.ShotsFired>shots){shots=hostile.ShotsFired;if(Time.time>next&&effort){source.PlayOneShot(effort,.55f);next=Time.time+9;}}}
 void OnDestroy(){if(body)body.HitOccurred-=Hit;}
}
