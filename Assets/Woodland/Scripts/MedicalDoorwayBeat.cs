using UnityEngine;
// Short authored doorway beat; retains the original creature and encounter effects.
[DefaultExecutionOrder(1200)]
public class MedicalDoorwayBeat:MonoBehaviour {
 public bool PlayerMotionEnabled=true;
 public bool Active{get;private set;}public float Age{get;private set;}
 // The ending follows the audible roar, regardless of whether the player fires.
 public bool Complete=>Active&&Roared&&GetComponent<MedicalThornbackEffects>().RoarFinished;
 public int BackSteps{get;private set;}public bool Roared{get;private set;}
 public Vector3 LeftTarget,RightTarget;public float FramePlane;
 MedicalInterior room;WoodlandPlayer p;MedicalThornbackCrawl crawl;AudioSource steps;AudioClip glass;
 Transform hand,sleeve;Vector3 eyeRest,rifleRest;Quaternion eyeRotation,torchRest;bool wasLocked,rifleVisible;float travel,lastStep=-1;bool released;
 public void Configure(MedicalInterior r){room=r;p=r.owner.player;crawl=GetComponent<MedicalThornbackCrawl>();FramePlane=r.jammedDoor.position.z;
 float center=r.transform.position.x,half=.73f;
 foreach(var c in r.GetComponentsInChildren<Collider>(true))if(c.name=="Doorway side wall"&&Mathf.Abs(c.bounds.center.z-FramePlane)<.3f){float edge=c.bounds.center.x<center?c.bounds.max.x:c.bounds.min.x;half=Mathf.Abs(edge-center);}
 // Front face of the jamb, just inside the aperture; never target the broad corridor wall.
 LeftTarget=new Vector3(center-half-.03f,r.transform.position.y+1.60f,FramePlane+.24f);RightTarget=new Vector3(center+half+.03f,r.transform.position.y+1.60f,FramePlane+.24f);
 var go=new GameObject("Retreat boots on glass");go.transform.SetParent(p.transform,false);steps=go.AddComponent<AudioSource>();steps.playOnAwake=false;glass=Resources.Load<AudioClip>("Medical/ThornbackFX/glass-step");MedicalCorridorAcoustics.Setup(steps,p.eye.transform);steps.volume=.48f;
 }
 public float Advance(float dt){if(!Active&&FramePlane-transform.position.z<2.15f){Active=true;Age=0;eyeRest=p.eye.transform.localPosition;eyeRotation=p.eye.transform.localRotation;rifleRest=p.rifle.localPosition;torchRest=p.torch.transform.localRotation;rifleVisible=p.rifle.gameObject.activeSelf;wasLocked=p.cinematicLocked;}
 if(!Active)return -1;Age+=dt;
 if(PlayerMotionEnabled&&Age>1.1f&&Age<4.5f){p.cinematicLocked=true;p.whiteHeadlight=false;p.ApplyTorchMode();p.torch.enabled=true;
 float desired=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.2f,4.3f,Age))*1.05f;float step=desired-travel;var body=p.GetComponent<CharacterController>();Vector3 before=p.transform.position;if(body&&body.enabled)body.Move(-p.transform.forward*step);else p.transform.position-=p.transform.forward*step;travel+=Vector3.Distance(before,p.transform.position);
 int index=Mathf.FloorToInt((Age-2.3f)/.65f);if(step>.001f&&index>=0&&index<3&&index>lastStep){lastStep=index;BackSteps++;steps.transform.position=p.transform.position+Vector3.up*.08f;if(glass)steps.PlayOneShot(glass);}
 }
 if(Age>=4.5f&&!released){released=true;if(PlayerMotionEnabled)RestorePlayer();}
 if(Age>=7.8f&&!Roared){Roared=true;GetComponent<MedicalThornbackEffects>().DoorwayRoar();}
 return Age<4.6f?0:Age<8.2f?.30f:0;
 }
 void LateUpdate(){if(!Active||!p||room.finished||!PlayerMotionEnabled)return;
 if(!p.whiteHeadlight){float pulse=Mathf.Repeat(Age,2.6f);p.torch.intensity=WoodlandPlayer.RedTorchPower*(pulse<.10f?.08f:pulse<.24f?1.2f:.65f);p.torch.range=15;}
 float raise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,1.8f,Age))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.0f,3.7f,Age)));
 if(raise>0){if(!hand&&p.visorEffects&&p.visorEffects.wipeHand){hand=Instantiate(p.visorEffects.wipeHand,p.eye.transform,false);hand.name="Headtorch adjustment glove";if(p.visorEffects.wipeSleeve){sleeve=Instantiate(p.visorEffects.wipeSleeve,p.eye.transform,false);sleeve.name="Headtorch adjustment sleeve";}}
 if(hand){hand.gameObject.SetActive(true);hand.localPosition=Vector3.Lerp(new Vector3(-.30f,-.48f,.35f),new Vector3(-.17f,.015f,.36f),raise);hand.localRotation=Quaternion.Euler(-65,180,25+Mathf.Sin(Age*14)*3*raise);}
 if(sleeve){sleeve.gameObject.SetActive(true);Vector3 wrist=hand.localPosition+new Vector3(-.025f,-.09f,0),elbow=new Vector3(-.38f,-.50f,.14f);sleeve.localPosition=(wrist+elbow)*.5f;sleeve.localRotation=Quaternion.FromToRotation(Vector3.up,wrist-elbow);sleeve.localScale=new Vector3(.082f,Vector3.Distance(wrist,elbow)*.5f,.082f);}
 p.rifle.localPosition=rifleRest+Vector3.down*.4f*raise;p.torch.transform.localRotation=torchRest*Quaternion.Euler(Mathf.Sin(Age*18)*5*raise,-7*raise,0);
 p.eye.transform.localRotation=eyeRotation*Quaternion.Euler(3*raise,0,Mathf.Sin(Age*8)*.8f*raise);
 }else{if(hand)hand.gameObject.SetActive(false);if(sleeve)sleeve.gameObject.SetActive(false);}
 if(Age>2.2f&&Age<4.4f)p.eye.transform.localPosition=eyeRest+Vector3.up*(Mathf.Sin((Age-2.2f)*Mathf.PI/.65f)*.018f);
 }
 void RestorePlayer(){if(!p)return;p.cinematicLocked=wasLocked;p.eye.transform.localPosition=eyeRest;p.eye.transform.localRotation=eyeRotation;p.rifle.localPosition=rifleRest;p.torch.transform.localRotation=torchRest;if(hand)hand.gameObject.SetActive(false);if(sleeve)sleeve.gameObject.SetActive(false);}
 void OnDisable(){if(Active&&!released&&PlayerMotionEnabled)RestorePlayer();if(steps)steps.Stop();}
 void OnDestroy(){if(hand)Destroy(hand.gameObject);if(sleeve)Destroy(sleeve.gameObject);if(steps)Destroy(steps.gameObject);}
}
