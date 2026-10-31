using UnityEngine;
using UnityEngine.InputSystem;
public class WoodlandTriggerReset:MonoBehaviour {
 public AudioClip clip;public AudioSource audioSource;
 [Range(0,1)]public float volume=.16f;
 public bool vibration=true;
 bool pending,controller;Gamepad firingPad,vibratingPad;float stopAt;
 public void Arm(bool fromController){pending=true;controller=fromController;firingPad=fromController?Gamepad.current:null;}
 void Update(){
  if(vibratingPad!=null&&Time.unscaledTime>=stopAt)StopPulse();
  if(Cursor.lockState!=CursorLockMode.Locked){pending=false;StopPulse();return;}
  if(!pending)return;
  if(controller&&(firingPad==null||!firingPad.added)){pending=false;return;}
  if(controller?!firingPad.rightTrigger.isPressed:Mouse.current!=null&&Mouse.current.leftButton.wasReleasedThisFrame)Release();
 }
 public bool Release(){if(!pending)return false;pending=false;if(audioSource&&clip)audioSource.PlayOneShot(clip,volume);
  if(vibration&&controller&&firingPad!=null&&firingPad.added){StopPulse();vibratingPad=firingPad;vibratingPad.SetMotorSpeeds(.025f,.075f);stopAt=Time.unscaledTime+.045f;}
  return true;
 }
 void StopPulse(){if(vibratingPad!=null&&vibratingPad.added)vibratingPad.SetMotorSpeeds(0,0);vibratingPad=null;}
 void OnDisable(){pending=false;StopPulse();}
 void OnApplicationFocus(bool focused){if(!focused){pending=false;StopPulse();}}
 void OnApplicationPause(bool paused){if(paused){pending=false;StopPulse();}}
}
