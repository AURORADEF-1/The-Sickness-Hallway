using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.IO;
public static class DesktopAimCheck {
 static System.Text.StringBuilder result;
 static void Check(bool pass,string name){result.AppendLine(name+": "+(pass?"PASS":"FAIL"));}
 public static void Run(){result=new System.Text.StringBuilder();
 Check(WoodlandControllerAim.Rate(new Vector2(.06f,.06f),true,false)==Vector2.zero,"Radial stick drift rejected");
 float fine=WoodlandControllerAim.Rate(new Vector2(.25f,0),true,false).x;
 Check(fine>0&&fine<5,"Quarter-stick ADS supports fine correction below 5 degrees/second");
 float hip=WoodlandControllerAim.Rate(Vector2.right,false,false).x,ads=WoodlandControllerAim.Rate(Vector2.right,true,false).x,focus=WoodlandControllerAim.Rate(Vector2.right,true,true).x;
 Check(hip>ads&&ads>focus&&focus>0,"ADS and focus progressively slower");
 Check(WoodlandControllerAim.Rate(Vector2.one,true,false).magnitude<=ads,"Diagonal input cannot exceed cardinal maximum");
 float[] fps={30,45,60,120};float previous=-1;bool invariant=true;foreach(float rate in fps){float total=0;for(int i=0;i<(int)rate;i++)total+=WoodlandControllerAim.Delta(new Vector2(.6f,0),true,false,1,1/rate).x;if(previous>=0&&Mathf.Abs(total-previous)>.001f)invariant=false;previous=total;}Check(invariant,"Same one-second turn at 30, 45, 60 and 120 FPS");
 Check(WoodlandControllerAim.Delta(Vector2.zero,true,false,1,1f/60)==Vector2.zero,"Stick release stops aim immediately");
 Check(Mathf.Abs(WoodlandControllerAim.Rate(Vector2.right,true,false,.5f).x*2-ads)<.01f,"Pause sensitivity scales ADS");
 var pad=InputSystem.AddDevice<Gamepad>();InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=new Vector2(.25f,0)});InputSystem.Update();Check(Mathf.Abs(WoodlandControllerAim.Rate(pad.rightStick.ReadUnprocessedValue(),true,false).x-fine)<.001f,"Raw stick avoids double deadzone processing");InputSystem.RemoveDevice(pad);
 File.WriteAllText("/Users/georgeambrose/Documents/Codex/2026-09-06/whqa/work/desktop-demo/aim-check.txt",result.ToString());if(result.ToString().Contains("FAIL"))throw new System.Exception(result.ToString());
 }
}
