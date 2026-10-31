Shader "Woodland/WindGrass" {
Properties{_BaseColor("Grass colour",Color)=(.17,.24,.07,1) _WindStrength("Wind strength metres",Range(0,.4))=.12 _PreviewTime("Preview time",Float)=0 _UsePreview("Preview override",Float)=0}
SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off
Pass{Tags{"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile _ _ADDITIONAL_LIGHTS
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
CBUFFER_START(UnityPerMaterial)
half4 _BaseColor;float _WindStrength,_PreviewTime,_UsePreview;
CBUFFER_END
struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;half4 color:TEXCOORD3;float height:TEXCOORD4;};
V vert(A a){V o;float3 w=TransformObjectToWorld(a.p.xyz);float time=lerp(_Time.y,_PreviewTime,_UsePreview);float gust=.55+.45*sin(time*.65+w.x*.12+w.z*.17);float wave=sin(time*2.5+w.x*1.3+w.z*.6)+.35*sin(time*4.4+w.z*3);float flex=a.uv.y*a.uv.y;w.xz+=float2(.85,.4)*_WindStrength*flex*(wave*.45+gust);o.w=w;o.p=TransformWorldToHClip(w);o.n=TransformObjectToWorldNormal(a.n);o.fog=ComputeFogFactor(o.p.z);o.color=a.color;o.height=a.uv.y;return o;}
half4 frag(V i):SV_Target{half3 n=normalize(i.n);Light l=GetMainLight(TransformWorldToShadowCoord(i.w));half3 lighting=max(SampleSH(n),0)+l.color*(.3+.7*abs(dot(n,l.direction)))*l.shadowAttenuation;
#ifdef _ADDITIONAL_LIGHTS
InputData inputData=(InputData)0;inputData.positionWS=i.w;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);
LIGHT_LOOP_BEGIN(GetAdditionalLightsCount())
Light a=GetAdditionalLight(lightIndex,i.w);lighting+=a.color*a.distanceAttenuation*a.shadowAttenuation*(.25+.75*abs(dot(n,a.direction)));
LIGHT_LOOP_END
#endif
half3 color=_BaseColor.rgb*i.color.rgb*lerp(.42,1.15,i.height)*lighting;return half4(MixFog(color,i.fog),1);}
ENDHLSL
}}}
