using UnityEngine;
// Scoped to the interior: restore the player's outdoor light settings on unload.
public class MedicalGunLighting:MonoBehaviour {
 Light flash;float intensity,range;Color color;LightShadows shadows;
 public void Configure(Light target){flash=target;intensity=target.intensity;range=target.range;color=target.color;shadows=target.shadows;target.intensity=18;target.range=9;target.color=new Color(1,.68f,.35f);target.shadows=LightShadows.Hard;}
 void OnDestroy(){Restore();}
 public void Restore(){if(flash){flash.intensity=intensity;flash.range=range;flash.color=color;flash.shadows=shadows;}}
}
