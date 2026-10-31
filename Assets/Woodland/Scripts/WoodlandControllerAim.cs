using UnityEngine;
// Angular velocity, independent of mouse sensitivity and Input System update count.
public static class WoodlandControllerAim {
 public const float Deadzone=.12f,OuterLimit=.98f;
 public static Vector2 Rate(Vector2 raw,bool aiming,bool focused,float aimSensitivity=1f){
  float magnitude=raw.magnitude;
  if(magnitude<=Deadzone)return Vector2.zero;
  float response=Mathf.Pow(Mathf.Clamp01((magnitude-Deadzone)/(OuterLimit-Deadzone)),1.8f);
  float speed=aiming?(focused?38f:65f)*Mathf.Clamp(aimSensitivity,.5f,1.5f):150f;
  Vector2 rate=raw/magnitude*(response*speed);
  rate.y*=.75f;
  return rate;
 }
 public static Vector2 Delta(Vector2 raw,bool aiming,bool focused,float sensitivity,float dt)=>Rate(raw,aiming,focused,sensitivity)*Mathf.Clamp(dt,0,.1f);
}
