using UnityEngine;
using System.Collections;
using System.IO;
// Opt-in packaged-player render verification. Does not run during normal play.
[DefaultExecutionOrder(10000)]
public class DesktopDemoVisualCheck : MonoBehaviour {
 string dir; WoodlandPlayer p; WoodlandHostile inspectedEnemy;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){var args=System.Environment.GetCommandLineArgs();int index=System.Array.IndexOf(args,"-demoVisualCheck");if(index<0||index+1>=args.Length)return;var g=Object.FindFirstObjectByType<WoodlandPlayer>().gameObject;var c=g.AddComponent<DesktopDemoVisualCheck>();c.dir=args[index+1];var inv=Object.FindFirstObjectByType<WoodlandInventory>();if(inv){inv.persist=false;inv.anatomy.condition=new HumanCondition();}}
 IEnumerator Start(){Directory.CreateDirectory(dir);Application.runInBackground=true;p=Object.FindFirstObjectByType<WoodlandPlayer>();yield return new WaitForSecondsRealtime(3);yield return Capture("01-startup");yield return new WaitForSecondsRealtime(12);int shots=0;foreach(var h in Object.FindObjectsByType<WoodlandHostile>(FindObjectsSortMode.None))shots+=h.ShotsFired;File.WriteAllText(Path.Combine(dir,"startup.txt"),"Position="+p.transform.position+"\nRifle active="+p.rifle.gameObject.activeInHierarchy+"\nIncapacitated="+p.anatomy.Incapacitated+"\nEnemy shots in opening="+shots);p.enabled=false;for(int i=0;i<8;i++)p.Preview(4.5f);yield return null;yield return Capture("02-ads");if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-demoWeaponCheck")>=0)yield return WeaponCheck();for(int i=0;i<8;i++)p.Preview(0);foreach(var h in Object.FindObjectsByType<WoodlandHostile>(FindObjectsSortMode.None)){h.enabled=false;inspectedEnemy=h;p.eye.transform.position=h.transform.position+h.transform.forward*3+Vector3.up*1.45f;p.eye.transform.LookAt(h.transform.position+Vector3.up*1.15f);break;}yield return null;yield return Capture("03-exile");
 if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-demoInjuryCheck")>=0){
 var enemy=inspectedEnemy;var injury=enemy.GetComponent<WoodlandNPCInjury>();var a=enemy.anatomy;
 WoodlandHitZone zone=null;foreach(var z in enemy.GetComponentsInChildren<WoodlandHitZone>())if(z.region==BodyRegion.Abdomen)zone=z;
 var point=zone?zone.transform.position:enemy.transform.position+Vector3.up;
 a.Hit(BodyRegion.Abdomen,18,InjuryKind.Projectile,point,-enemy.transform.forward);yield return new WaitForSeconds(.7f);yield return Capture("06-first-hit");float blend=injury.ReactionStrength;float blood=a.condition.blood;
 a.Hit(BodyRegion.RightForearm,6,InjuryKind.Projectile,point,-enemy.transform.forward);yield return null;yield return Capture("07-follow-up-hit");
 File.WriteAllText(Path.Combine(dir,"injury.txt"),"Before="+blend+" After="+injury.ReactionStrength+" Reaction="+injury.CurrentReaction+" Blood decreased="+(a.condition.blood<blood));
 yield return new WaitForSeconds(.4f);yield return Capture("08-follow-up-settled");
 a.Hit(BodyRegion.LeftThigh,18,InjuryKind.Projectile,point,-enemy.transform.forward);yield return new WaitForSeconds(.5f);var state=injury.CurrentReaction;float kneeBlend=injury.ReactionStrength;a.Hit(BodyRegion.RightForearm,5,InjuryKind.Projectile,point,-enemy.transform.forward);yield return null;
 File.AppendAllText(Path.Combine(dir,"injury.txt"),"\nKneel preserved="+(state==WoodlandNPCInjury.Reaction.Kneel&&injury.CurrentReaction==state)+" Blend maintained="+(injury.ReactionStrength>=kneeBlend));yield return Capture("09-kneel-follow-up");
 }
 var flow=Object.FindFirstObjectByType<WoodlandMedicalSequence>();
 if(flow&&flow.entrance){p.TeleportTo(flow.entrance.position-flow.entrance.forward*7+Vector3.up*.15f,flow.entrance.eulerAngles.y);p.eye.transform.LookAt(flow.entrance.position+Vector3.up*1.5f);p.torch.enabled=true;yield return new WaitForSecondsRealtime(1);yield return Capture("04-medical-exterior");flow.powered=true;yield return flow.Enter();yield return new WaitForSecondsRealtime(2);yield return Capture("05-medical-interior");File.WriteAllText(Path.Combine(dir,"medical.txt"),"Inside="+flow.inside+"\nInterior="+(flow.interior?flow.interior.name:"MISSING")+"\nCamera="+p.eye.transform.position);}
 File.WriteAllText(Path.Combine(dir,"complete.txt"),"Visual captures complete");Application.Quit();}
 IEnumerator WeaponCheck(){
 foreach(var h in Object.FindObjectsByType<WoodlandHostile>(FindObjectsSortMode.None))h.enabled=false;
 p.torch.enabled=false;yield return null;yield return Capture("10-sight-torch-off");
 int cells=p.inventory.Count("torch_cell");p.battery=0;p.TapTorch(Time.unscaledTime);yield return new WaitForSeconds(2.2f);
 bool cell=p.battery>99&&p.torch.enabled&&p.inventory.Count("torch_cell")==cells-1;
 p.whiteHeadlight=true;p.ApplyTorchMode();p.Preview(0);yield return new WaitForSeconds(.3f);yield return Capture("11-torch-restored");
 p.TapTorch(Time.unscaledTime+1);bool off=!p.torch.enabled;p.TapTorch(Time.unscaledTime+2);bool on=p.torch.enabled;p.TapTorch(Time.unscaledTime+2.15f);bool mode=!p.whiteHeadlight&&p.torch.enabled;
 p.whiteHeadlight=true;p.ApplyTorchMode();p.rounds=30;p.Preview(4.5f);bool shot=p.Fire();float kick=p.RecoilAngle.x;yield return null;yield return Capture("12-recoil");
 for(int i=0;i<45;i++){p.ApplyLook(Vector2.zero,true,1f/45);p.RenderPose(1f/45,0);yield return null;}bool recovery=p.RecoilAngle.x<kick*.01f;
 WoodlandGunshotAcoustics.Play(p,p.eye.transform.position,p.shot,true,false);yield return new WaitForSeconds(3);
 WoodlandGunshotAcoustics.Play(p,p.eye.transform.position,p.shot,true,true);yield return new WaitForSeconds(3);
 WoodlandGunshotAcoustics.Play(p,p.eye.transform.position+p.eye.transform.forward*60,p.shot,false,false);yield return new WaitForSeconds(3);
 string result="Battery replacement="+cell+" Toggle off="+off+" Toggle on="+on+" Double tap="+mode+"\nShot="+shot+" Kick="+kick+" Recovery="+recovery+"\nAudio: woodland, corridor and distant woodland test shots emitted";
 File.WriteAllText(Path.Combine(dir,"weapon-check.txt"),result);if(!(cell&&off&&on&&mode&&shot&&kick>0&&recovery))Debug.LogError("Weapon regression failed: "+result);
 }
 string pendingCapture;
 IEnumerator Capture(string name){pendingCapture=name;while(pendingCapture!=null)yield return null;}
 void LateUpdate(){if(pendingCapture==null)return;RenderCapture(pendingCapture);pendingCapture=null;}
 void RenderCapture(string name){var cam=p.eye;bool injuryFrame=name.StartsWith("06-")||name.StartsWith("07-")||name.StartsWith("08-")||name.StartsWith("09-");GameObject fill=null;if(injuryFrame){fill=new GameObject("Diagnostic illumination");fill.transform.position=cam.transform.position;var light=fill.AddComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=5;}var rt=new RenderTexture(1280,720,24);var previous=cam.targetTexture;var active=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(dir,name+".png"),tex.EncodeToPNG());cam.targetTexture=previous;RenderTexture.active=active;Destroy(tex);Destroy(rt);if(fill)Destroy(fill);}
}
