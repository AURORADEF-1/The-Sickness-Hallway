using UnityEditor.Build;using UnityEngine;using UnityEditor.Rendering;using UnityEngine.Rendering;using System.Collections.Generic;
// These two demo scenes contain no baked lightmaps, mixed-lighting data or reflection probes.
// Preserve real-time lights, cookies, shadows, fog and material features.
public sealed class DesktopDemoShaderStrip:IPreprocessShaders {
 public static bool Enabled;public int callbackOrder=>1000;
 public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> data){if(!Enabled)return;string[] unused={"LIGHTMAP_ON","DYNAMICLIGHTMAP_ON","DIRLIGHTMAP_COMBINED","SHADOWS_SHADOWMASK","LIGHTMAP_SHADOW_MIXING","_REFLECTION_PROBE_BLENDING","_REFLECTION_PROBE_BOX_PROJECTION","_REFLECTION_PROBE_ATLAS","PROBE_VOLUMES_L1","PROBE_VOLUMES_L2"};for(int i=data.Count-1;i>=0;i--){foreach(var name in unused){if(data[i].shaderKeywordSet.IsEnabled(new ShaderKeyword(shader,name))){data.RemoveAt(i);break;}}}}
}
