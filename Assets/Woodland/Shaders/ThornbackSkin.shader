Shader "Woodland/ThornbackSkin" {
Properties {_BaseMap("Original skin",2D)="white"{} _BumpMap("Skin normal",2D)="bump"{} _BaseColor("Tint",Color)=(.55,.61,.48,1) _BumpScale("Relief",Float)=1.2 _Cutoff("Cutoff",Float)=.5}
SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
Pass {Tags{"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _ADDITIONAL_LIGHTS
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile_fog
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;half4 _BaseColor;half _BumpScale;half _Cutoff;
CBUFFER_END
struct A {float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;};
struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float4 t:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;};
V vert(A a){V o;VertexPositionInputs p=GetVertexPositionInputs(a.p.xyz);o.p=p.positionCS;o.w=p.positionWS;o.n=TransformObjectToWorldNormal(a.n);o.t=float4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
half4 frag(V i):SV_Target {half3 original=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;float mottling=noise(i.uv*38)*.65+noise(i.uv*113)*.35;float lesions=smoothstep(.63,.85,mottling);float veins=pow(saturate(1-abs(sin((i.uv.x*100+noise(i.uv*24)*9)))),24)*.12;SurfaceData s=(SurfaceData)0;s.albedo=original*lerp(_BaseColor.rgb,half3(.38,.17,.14),lesions)*(1-veins);s.alpha=1;s.metallic=0;s.smoothness=lerp(.25,.56,lesions);s.occlusion=lerp(.7,1,mottling);s.normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);InputData d=(InputData)0;d.positionWS=i.w;half3 n=normalize(i.n),t=normalize(i.t.xyz),b=cross(n,t)*i.t.w;d.normalWS=normalize(TransformTangentToWorld(s.normalTS,half3x3(t,b,n)));d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=1;half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;}
ENDHLSL
}
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
}}
