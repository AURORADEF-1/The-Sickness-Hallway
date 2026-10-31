using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using System.IO;
public static class DesktopControllerCheck {
 public static void Run(){var pad=InputSystem.AddDevice<Gamepad>();var obj=new GameObject("Controller test");var adapter=obj.AddComponent<WoodlandDemoControls>();adapter.SendMessage("Awake");var results=new System.Text.StringBuilder();
 // Initialize the virtual keyboard and mouse before testing action mappings.
 Check(pad,new GamepadState().WithButton(GamepadButton.South),Key.E,"Device warm-up",new System.Text.StringBuilder());
 Check(pad,new GamepadState().WithButton(GamepadButton.West),Key.R,"Reload",results);
 Check(pad,new GamepadState().WithButton(GamepadButton.South),Key.E,"A action / UI click",results);
 Check(pad,new GamepadState().WithButton(GamepadButton.North),Key.F,"Torch",results);
 Check(pad,new GamepadState().WithButton(GamepadButton.East),Key.B,"Backpack",results);
 Check(pad,new GamepadState().WithButton(GamepadButton.RightShoulder),Key.H,"Bandage",results);
 Check(pad,new GamepadState().WithButton(GamepadButton.RightShoulder).WithButton(GamepadButton.LeftShoulder),Key.P,"Suit patch",results);
 Check(pad,new GamepadState{leftTrigger=1},Key.Q,"Aim",results);
 Check(pad,new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.DpadLeft),Key.Digit2,"Radio",results);
 Object.DestroyImmediate(obj);InputSystem.RemoveDevice(pad);File.WriteAllText("/Users/georgeambrose/Documents/Codex/2026-09-06/whqa/work/desktop-demo/controller-check.txt",results.ToString());if(results.ToString().Contains("FAIL"))throw new System.Exception(results.ToString());}
 static void Check(Gamepad pad,GamepadState state,Key key,string label,System.Text.StringBuilder result){Cursor.lockState=CursorLockMode.Locked;pad.MakeCurrent();InputState.Change(pad,state);InputSystem.QueueStateEvent(pad,state);InputSystem.Update();InputSystem.Update();InputSystem.Update();result.AppendLine(label+": "+((Keyboard.current!=null&&Keyboard.current[key].isPressed||key==Key.E&&Mouse.current!=null&&Mouse.current.leftButton.isPressed)?"PASS":"FAIL"));InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.Update();InputSystem.Update();}
}
