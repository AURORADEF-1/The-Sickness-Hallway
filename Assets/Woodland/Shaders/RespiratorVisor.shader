Shader "Woodland/RespiratorVisor" {
 Properties { _Opacity("Rim opacity", Range(0,1)) = .9 _Fog("Condensation",Range(0,1))=0 _Wipe("Wipe front",Float)=-.2 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
 Pass { ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float _Opacity,_Fog,_Wipe;
 struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; }; struct V { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; };
 V vert(A a){V o;o.positionCS=float4(a.positionOS.xy,0,1);o.uv=a.uv;return o;}
 float noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);float4 v=frac(sin(float4(dot(q,float2(127.1,311.7)),dot(q+float2(1,0),float2(127.1,311.7)),dot(q+float2(0,1),float2(127.1,311.7)),dot(q+1,float2(127.1,311.7))))*43758.5453);return lerp(lerp(v.x,v.y,f.x),lerp(v.z,v.w,f.x),f.y);}
 half4 frag(V i):SV_Target {
 float2 p=i.uv*2-1;
 float edge=length(p*float2(.78,.88));
 float rim=smoothstep(.83,1.26,edge)*.83;
 float lower=smoothstep(.72,1.05,-p.y)*.22;
 float haze=_Fog*(.40+.36*noise(i.uv*float2(11,7))+.24*noise(i.uv*float2(36,22)));
 haze*=lerp(.025,1,smoothstep(-.08,.08,i.uv.x-_Wipe));
 float breath=1+.025*sin(_Time.y*2.2);
 haze=saturate(haze*breath);
 float rimAlpha=saturate((rim+lower)*_Opacity);
 float alpha=haze+rimAlpha*(1-haze);
 float3 rgb=(float3(.014,.018,.017)*haze+float3(.006,.009,.008)*rimAlpha*(1-haze))/max(alpha,.001);
 return half4(rgb,alpha);
 }
 ENDHLSL
 }} }
