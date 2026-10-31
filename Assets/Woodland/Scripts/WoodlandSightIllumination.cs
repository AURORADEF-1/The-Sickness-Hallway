using UnityEngine;
public class WoodlandSightIllumination:MonoBehaviour {
 Material dotMaterial;
 public static void Install(WoodlandPlayer player){if(!player.GetComponent<WoodlandSightIllumination>())player.gameObject.AddComponent<WoodlandSightIllumination>();}
 void Start(){var p=GetComponent<WoodlandPlayer>();foreach(var model in new[]{p.rifle,p.adsRifle,p.focusRifle})if(model)foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;var shader=Resources.Load<Shader>("SightDot");if(!shader){Debug.LogError("SightDot shader missing");return;}dotMaterial=new Material(shader);foreach(var renderer in GetComponentsInChildren<Renderer>(true))if(renderer.name=="Red aiming dot"){renderer.sharedMaterial=dotMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;}}
 void OnDestroy(){if(dotMaterial)Destroy(dotMaterial);}
}
