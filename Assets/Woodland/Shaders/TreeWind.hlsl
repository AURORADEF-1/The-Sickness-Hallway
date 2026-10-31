#ifndef WOODLAND_TREE_WIND
#define WOODLAND_TREE_WIND
// World-space gusts keep both LODs moving together, with the root pinned.
float _WoodlandWindPreview, _WoodlandWindTime;
float3 WoodlandTreeWind(float3 positionOS){
 float3 w=TransformObjectToWorld(positionOS);
 float3 root=TransformObjectToWorld(float3(0,0,0));
 float time=lerp(_Time.y,_WoodlandWindTime,_WoodlandWindPreview);
 float h=max(0,w.y-root.y);
 float flex=pow(saturate(h/12.0),1.65);
 float gust=.55+.45*sin(time*.65+root.x*.12+root.z*.17);
 float sway=sin(time*.85+root.x*.23+root.z*.19)*.65+gust*.6;
 w.xz+=float2(.85,.4)*_WindStrength*flex*sway;
 float flutter=sin(time*3.6+w.x*1.3+w.z*.9)+.35*sin(time*6.1+w.y*2.4);
 float leaf=0;
#ifdef _ALPHATEST_ON
 leaf=_LeafFlutter*saturate(h/1.5);
#endif
 w+=float3(.75,.22,.4)*flutter*leaf;
 return TransformWorldToObject(w);
}
#endif
