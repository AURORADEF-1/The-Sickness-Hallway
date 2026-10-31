using UnityEngine;
// Authored gravity fall avoids solver explosions inside the narrow jamb. The leaf
// topples onto the actual floor surface, then remains as inert collidable debris.
public class MedicalTimberSettler:MonoBehaviour {
 Rigidbody body;BoxCollider shape;float floor,age,velocity,grounded;Quaternion from,to;Vector3 position;public bool Settled {get;private set;}
 public void Configure(float floorHeight){body=GetComponent<Rigidbody>();shape=GetComponent<BoxCollider>();floor=floorHeight;position=body.position;from=body.rotation;
  foreach(var hit in Physics.RaycastAll(new Vector3(position.x,floorHeight+.5f,position.z),Vector3.down,1,~0,QueryTriggerInteraction.Ignore))if(hit.normal.y>.9f&&!hit.rigidbody&&hit.collider.bounds.size.x>2&&hit.collider.bounds.size.z>2)floor=Mathf.Max(floor,hit.point.y);
  to=Quaternion.Euler(90,Mathf.Sin(position.z*11+position.y*7)*12,0);body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;
 }
 void FixedUpdate(){if(!body||Settled)return;float dt=Time.fixedDeltaTime;age+=dt;velocity-=9.81f*dt;position+=Vector3.down*(-velocity)*dt;
  var rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.95f)));Vector3 size=Vector3.Scale(shape.size*.5f,transform.lossyScale);Vector3 center=rotation*Vector3.Scale(shape.center,transform.lossyScale);
  float half=Mathf.Abs((rotation*Vector3.right).y)*size.x+Mathf.Abs((rotation*Vector3.up).y)*size.y+Mathf.Abs((rotation*Vector3.forward).y)*size.z;
  float bottom=position.y+center.y-half;
  if(bottom<=floor+.012f){position.y+=floor+.012f-bottom;velocity=0;grounded+=dt;}else grounded=0;
  body.MoveRotation(rotation);body.MovePosition(position);
  if(age>=.95f&&grounded>.18f){Settled=true;body.position=position;body.rotation=to;}
 }
}
