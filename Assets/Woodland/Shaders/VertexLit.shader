Shader "Woodland/VertexLit" {
 Properties { _BaseMap("Colour",2D)="white"{} _AlphaMap("Leaf mask",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout"} Cull Off
 Pass { Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile _ _ADDITIONAL_LIGHTS
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_AlphaMap);SAMPLER(sampler_AlphaMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST;half4 _Tint;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;half4 color:COLOR;};
 struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 n:TEXCOORD1;float3 w:TEXCOORD2;half fog:TEXCOORD3;half4 color:COLOR;};
 V vert(A a){V o;VertexPositionInputs p=GetVertexPositionInputs(a.p.xyz);o.p=p.positionCS;o.w=p.positionWS;o.uv=a.uv;o.color=a.color;o.n=TransformObjectToWorldNormal(a.n);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_AlphaMap,sampler_AlphaMap,i.uv).r-.5);half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_Tint.rgb*max(i.color.rgb,half3(.016,.016,.016));Light l=GetMainLight(TransformWorldToShadowCoord(i.w));half lit=saturate(abs(dot(normalize(i.n),l.direction)))*.6+.25;half3 ambient=max(SampleSH(normalize(i.n)),0);half3 illumination=ambient+l.color*lit*l.shadowAttenuation;
#ifdef _ADDITIONAL_LIGHTS
InputData inputData=(InputData)0;inputData.positionWS=i.w;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);
LIGHT_LOOP_BEGIN(GetAdditionalLightsCount())
Light a=GetAdditionalLight(lightIndex,i.w);illumination+=a.color*a.distanceAttenuation*a.shadowAttenuation*(.25+.75*abs(dot(normalize(i.n),a.direction)));
LIGHT_LOOP_END
#endif
c*=illumination;return half4(MixFog(c,i.fog),1);}
 ENDHLSL }
 }
}
