using UnityEngine;
public class WoodlandRelayWarning : MonoBehaviour {
 public WoodlandPlayer player;
 public Light statusLight;
 public Renderer statusRenderer;
 public AudioSource warningAudio, repairedAudio;
 public float blinkPeriod=1.2f;
 float nextWarning; bool wasServiced;
 MaterialPropertyBlock block;
 void Start(){wasServiced=player&&player.serviced;}
 void Update(){Tick(Time.time);}
 public void Tick(float clock){
  if(!player)return;
  bool repaired=player.serviced;
  bool lit=repaired||clock%blinkPeriod<blinkPeriod*.42f;
  Color color=repaired?new Color(.04f,1,.12f):new Color(1,.025f,.01f);
  if(statusLight){statusLight.intensity=.18f;statusLight.range=1.5f;statusLight.color=color;statusLight.enabled=lit;}
  if(statusRenderer){
   if(block==null)block=new MaterialPropertyBlock();
   block.SetColor("_BaseColor",lit?color:new Color(.025f,.012f,.01f));
   block.SetColor("_EmissionColor",lit?color*2:Color.black);
   statusRenderer.SetPropertyBlock(block);
  }
  if(repaired){
   if(warningAudio&&warningAudio.isPlaying)warningAudio.Stop();
   if(!wasServiced&&repairedAudio)repairedAudio.Play();
  } else if(warningAudio&&warningAudio.clip&&clock>=nextWarning&&!warningAudio.isPlaying&&Vector3.Distance(player.transform.position,transform.position)<85){
   warningAudio.Play();nextWarning=clock+warningAudio.clip.length+6;
  }
  wasServiced=repaired;
 }
 void OnDisable(){if(warningAudio)warningAudio.Stop();if(repairedAudio)repairedAudio.Stop();}
}
