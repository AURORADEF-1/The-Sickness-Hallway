Shader "Woodland/ForestFloor" {
Properties{_Soil("Soil",2D)="white"{} _Leaves("Leaves",2D)="white"{} _Moss("Moss",2D)="white"{} _Normal("Normal",2D)="bump"{}}
SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass{Tags{"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_fog
#pragma multi_compile _ _ADDITIONAL_LIGHTS
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_Soil);SAMPLER(sampler_Soil);TEXTURE2D(_Leaves);SAMPLER(sampler_Leaves);TEXTURE2D(_Moss);SAMPLER(sampler_Moss);TEXTURE2D(_Normal);SAMPLER(sampler_Normal);
struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;half fog:TEXCOORD2;};
V vert(A a){V o;VertexPositionInputs v=GetVertexPositionInputs(a.p.xyz);o.p=v.positionCS;o.w=v.positionWS;o.n=TransformObjectToWorldNormal(a.n);o.fog=ComputeFogFactor(o.p.z);return o;}
half4 frag(V i):SV_Target{float2 uv=i.w.xz;float path=2.4*sin(uv.y*.10)+.65*sin(uv.y*.31);float grain=sin(uv.x*18+sin(uv.y*21))*sin(uv.y*15);float edge=abs(uv.x-path)+grain*.09;float off=smoothstep(.38,.96,edge);float patch=saturate(.5+.3*sin(uv.x*.83+uv.y*.56)+.2*sin(uv.y*1.4));half3 soil=SAMPLE_TEXTURE2D(_Soil,sampler_Soil,uv*.62).rgb*half3(.67,.53,.42);half3 leaves=SAMPLE_TEXTURE2D(_Leaves,sampler_Leaves,uv*.52).rgb*half3(.46,.49,.43);half3 moss=SAMPLE_TEXTURE2D(_Moss,sampler_Moss,uv*.57).rgb*half3(.49,.62,.37);float scattered=smoothstep(.55,.87,sin(uv.x*5.3+sin(uv.y*2.7))*sin(uv.y*6.1));soil=lerp(soil,leaves,scattered*.32);float worn=1-.16*exp(-pow((uv.x-path)*4,2));half3 col=lerp(soil*worn,lerp(leaves,moss,patch*.65),off);half3 n=UnpackNormal(SAMPLE_TEXTURE2D(_Normal,sampler_Normal,uv*.52));n=normalize(i.n+half3(n.x,0,n.y)*.48);Light light=GetMainLight(TransformWorldToShadowCoord(i.w));half3 ambient=max(SampleSH(n),0);half3 illumination=ambient+light.color*saturate(dot(n,light.direction))*light.shadowAttenuation;
#ifdef _ADDITIONAL_LIGHTS
InputData inputData=(InputData)0;inputData.positionWS=i.w;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);
LIGHT_LOOP_BEGIN(GetAdditionalLightsCount())
Light a=GetAdditionalLight(lightIndex,i.w);illumination+=a.color*a.distanceAttenuation*a.shadowAttenuation*saturate(dot(n,a.direction));
LIGHT_LOOP_END
#endif
col*=illumination;return half4(MixFog(col,i.fog),1);}
ENDHLSL
} UsePass "Universal Render Pipeline/Lit/DepthOnly" UsePass "Universal Render Pipeline/Lit/ShadowCaster"}}
