using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using System.IO;
public static class WindowsDemoBuild {
 public static void Build(){
  DesktopControllerCheck.Run();DesktopAimCheck.Run();DesktopCombatCheck.Run();DesktopDemoShaderStrip.Enabled=false;
  const BuildTarget target=BuildTarget.StandaloneWindows64;
  if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,target))throw new System.Exception("Windows build support is missing");
  var named=NamedBuildTarget.Standalone;var backend=PlayerSettings.GetScriptingBackend(named);
  bool auto=PlayerSettings.GetUseDefaultGraphicsAPIs(target);var apis=PlayerSettings.GetGraphicsAPIs(target);
  try {
   PlayerSettings.productName="The Sickness Demo";PlayerSettings.companyName="Sadtrip";
   PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
   PlayerSettings.SetScriptingBackend(named,ScriptingImplementation.Mono2x);
   PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.Direct3D11});
   var args=System.Environment.GetCommandLineArgs();int i=System.Array.IndexOf(args,"-demoBuildDirectory");
   if(i<0||i+1>=args.Length)throw new System.Exception("Specify -demoBuildDirectory");
   var dir=args[i+1];Directory.CreateDirectory(dir);
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Woodland.unity","Assets/Scenes/MedicalResearch.unity"},locationPathName=Path.Combine(dir,"The Sickness Demo.exe"),target=target,options=BuildOptions.CleanBuildCache|BuildOptions.CompressWithLz4});
   File.WriteAllText(Path.Combine(dir,"Build verification.txt"),"Windows x64 / Direct3D 11 / Mono\n"+result.summary.result+" errors="+result.summary.totalErrors+" warnings="+result.summary.totalWarnings+"\nBoth woodland and medical research scenes included.\nController, aiming and combat regression checks passed.\nBuilt on macOS; Windows runtime playtest not performed.\n");
   if(result.summary.result!=BuildResult.Succeeded||result.summary.totalErrors!=0)throw new System.Exception("Windows build failed; see build report");
  }finally{PlayerSettings.SetScriptingBackend(named,backend);PlayerSettings.SetUseDefaultGraphicsAPIs(target,auto);PlayerSettings.SetGraphicsAPIs(target,apis);}
 }
}
