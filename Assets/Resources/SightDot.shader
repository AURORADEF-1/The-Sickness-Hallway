Shader "Woodland/SelfLitSight" {
SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
Pass { ZWrite Off ZTest LEqual Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A { float4 positionOS:POSITION; }; struct V { float4 positionCS:SV_POSITION; };
V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);return o;}
half4 frag(V i):SV_Target { return half4(2.5,.012,.003,1); }
ENDHLSL
} } }
