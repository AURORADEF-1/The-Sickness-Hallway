using UnityEngine;
// Reserve six local-light slots for carried lights and gunfire. Distant markers stay emissive.
public class WoodlandTrailLighting:MonoBehaviour {
 public Transform player;Light[] lights;float next;
 void OnEnable(){lights=GetComponentsInChildren<Light>(true);Refresh();}
 void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;Refresh();}
 public void Refresh(){if(lights==null)lights=GetComponentsInChildren<Light>(true);if(!player)return;Light first=null,second=null;float a=float.MaxValue,b=float.MaxValue;foreach(var l in lights){float d=(l.transform.position-player.position).sqrMagnitude;if(d<a){second=first;b=a;first=l;a=d;}else if(d<b){second=l;b=d;}}foreach(var l in lights)l.enabled=l==first||l==second;}
}
