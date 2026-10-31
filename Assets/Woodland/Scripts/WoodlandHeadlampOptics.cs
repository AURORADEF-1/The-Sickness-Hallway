using UnityEngine;
// A broad reflector spill surrounding an offset hotspot, with soft optical falloff.
[DefaultExecutionOrder(1100)] public class WoodlandHeadlampOptics:MonoBehaviour {
 public Light source;Texture2D cookie;Light spill;
 void Start(){if(!source)source=GetComponent<Light>();if(!source)return;cookie=new Texture2D(128,128,TextureFormat.RGBA32,false,true);cookie.name="Headlamp reflector cookie";cookie.wrapMode=TextureWrapMode.Clamp;for(int y=0;y<128;y++)for(int x=0;x<128;x++){float u=(x-63.5f)/63.5f,v=(y-63.5f)/63.5f;float r=Mathf.Sqrt(u*u+v*v);float hotspot=Mathf.Exp(-(u*u*15+(v-.06f)*(v-.06f)*19));float flood=.28f*Mathf.Exp(-r*r*2.4f);float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,r));float optics=1+.025f*Mathf.Sin(Mathf.Atan2(v,u)*18+r*30);float a=Mathf.Clamp01((hotspot+flood)*edge*optics);cookie.SetPixel(x,y,new Color(a,a,a,a));}cookie.Apply();source.cookie=cookie;var g=new GameObject("Headlamp peripheral spill");g.transform.SetParent(source.transform,false);spill=g.AddComponent<Light>();spill.type=LightType.Spot;spill.spotAngle=95;spill.innerSpotAngle=15;spill.shadows=LightShadows.None;}
 void LateUpdate(){if(!source||!spill)return;spill.enabled=source.enabled;spill.color=source.color;spill.intensity=source.intensity*.12f;spill.range=Mathf.Min(8,source.range);}
 void OnDestroy(){if(cookie)Destroy(cookie);if(spill)Destroy(spill.gameObject);}
}
