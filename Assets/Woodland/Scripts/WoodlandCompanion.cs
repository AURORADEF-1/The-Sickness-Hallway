using UnityEngine;using UnityEngine.InputSystem;
public class WoodlandCompanion:MonoBehaviour {
 public Transform player,relay;public Animator animator;public WoodlandAnatomy anatomy;public float walkSpeed=1.35f,runSpeed=3.3f;public float speed;public bool waiting=true,arrived,followPlayer;
 public static float PathX(float z)=>2.4f*Mathf.Sin(z*.10f)+.65f*Mathf.Sin(z*.31f);
 public static float Ground(float x,float z)=>.11f*Mathf.Sin(z*.3f)+.07f*Mathf.Sin(x*.7f+z*.23f);
 void Update(){if(Keyboard.current!=null&&Keyboard.current.gKey.wasPressedThisFrame&&Cursor.lockState==CursorLockMode.Locked)followPlayer=!followPlayer;Tick(Time.deltaTime);}
 public void Tick(float dt){if(!player||!animator||anatomy&&anatomy.Incapacitated)return;float distance=Vector3.Distance(player.position,transform.position);Vector3 target;float desired;
  if(followPlayer){Vector3 offset=transform.position-player.position;offset.y=0;target=player.position+(offset.sqrMagnitude>.01f?offset.normalized:-player.forward)*2;target.y=transform.position.y;desired=distance<2.25f?0:distance>6?runSpeed:walkSpeed;arrived=false;waiting=desired==0;}
  else{if(distance>9&&player.position.z<transform.position.z)waiting=true;else if(distance<6||player.position.z>transform.position.z)waiting=false;float end=relay?relay.position.z-2.5f:21;arrived=transform.position.z>=end-.08f;desired=waiting||arrived?0:player.position.z>transform.position.z+2?runSpeed:walkSpeed;float z=Mathf.Min(end,transform.position.z+Mathf.Max(speed,.1f)*dt);target=new Vector3(PathX(z),Ground(PathX(z),z)+.025f,z);}
  desired*=anatomy?anatomy.MovementMultiplier:1;speed=Mathf.MoveTowards(speed,desired,dt*(desired==0?4:2.3f));if(speed>.005f){Vector3 d=target-transform.position;d.y=0;float length=d.magnitude;if(length>.001f){Vector3 step=d.normalized*Mathf.Min(length,speed*dt);if(!Physics.SphereCast(transform.position+Vector3.up*.65f,.18f,step.normalized,out var obstacle,step.magnitude+.08f,~0,QueryTriggerInteraction.Ignore)){transform.position+=step;Vector3 pos=transform.position;pos.y=Ground(pos.x,pos.z)+.025f;transform.position=pos;}else speed=0;transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-dt*7));}}
  animator.SetFloat("Speed",speed);animator.speed=speed>.1f?Mathf.Clamp(speed/(speed>2.3f?runSpeed:walkSpeed),.65f,1.2f):1;
 }
}
