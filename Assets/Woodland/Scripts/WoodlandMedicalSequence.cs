using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
// The outdoor controller owns the persistent player. The small interior loads additively;
// outdoor rendering, actors and audio are suspended until the player leaves.
public class WoodlandMedicalSequence:MonoBehaviour {
 public WoodlandPlayer player;public Transform generator,entrance;public Renderer generatorLamp;public AudioSource generatorAudio;public AudioClip startSound;
 public GeneratorWireRepair repair;public bool powered,inside,loading;public string stage="Woodland";public MedicalInterior interior;
 public const string InteriorScene="MedicalResearch";readonly List<GameObject> suspended=new List<GameObject>();Vector3 returnPoint;float returnYaw;string prompt="";float progress;
 void Start(){repair=GetComponent<GeneratorWireRepair>()??gameObject.AddComponent<GeneratorWireRepair>();repair.owner=this;player.ExternalUse=Use;player.Gunshot+=Shot;}
 void OnDestroy(){if(player){player.ExternalUse=null;player.Gunshot-=Shot;}}
 void Shot(Vector3 p){if(inside&&interior)interior.OnShot();}
 void Update(){if(!player)return;if(loading)return;if(inside&&interior){prompt=interior.Prompt();player.objectiveMessage=prompt;return;}
 var target=Target();prompt=target==generator?(powered?"Generator running · Auxiliary power restored":"E   Repair generator wiring"):target==entrance?(powered?"E   Enter medical research building":"No power · Start the generator beside the entrance"):player.transform.position.z>26?"Follow the trail · Search the abandoned research building":"";player.objectiveMessage=prompt;}
 Transform Target(){if(!Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3,~0,QueryTriggerInteraction.Ignore))return null;if(hit.transform.IsChildOf(generator))return generator;if(hit.transform.IsChildOf(entrance))return entrance;return null;}
 public bool Use(){if(loading||player.cinematicLocked||player.anatomy.Incapacitated)return true;if(inside)return interior&&interior.Use();var target=Target();if(target==generator){if(!powered)StartCoroutine(StartGenerator());return true;}if(target==entrance){if(powered)StartCoroutine(Enter());return true;}return false;}
 public IEnumerator StartGenerator(){if(powered||loading)yield break;if(!repair){repair=gameObject.AddComponent<GeneratorWireRepair>();repair.owner=this;}if(!repair.Complete){repair.Open();yield break;}loading=true;stage="Starting generator";player.cinematicLocked=true;progress=0;if(generatorAudio&&startSound)generatorAudio.PlayOneShot(startSound,.65f);while(progress<1){progress+=Time.deltaTime/2.4f;yield return null;}powered=true;if(generatorAudio)generatorAudio.Play();if(generatorLamp){generatorLamp.material.EnableKeyword("_EMISSION");generatorLamp.material.SetColor("_EmissionColor",Color.green*2);}player.cinematicLocked=false;loading=false;stage="Powered";}
 public IEnumerator Enter(){if(!powered||loading||inside)yield break;loading=true;stage="Loading medical research";player.cinematicLocked=true;progress=0;returnPoint=player.transform.position;returnYaw=player.transform.eulerAngles.y;if(player.fieldKit&&player.fieldKit.Open)player.fieldKit.Close();
 var op=SceneManager.LoadSceneAsync(InteriorScene,LoadSceneMode.Additive);if(op==null){loading=false;player.cinematicLocked=false;yield break;}while(!op.isDone){progress=op.progress;yield return null;}
 var scene=SceneManager.GetSceneByName(InteriorScene);foreach(var root in scene.GetRootGameObjects()){interior=root.GetComponent<MedicalInterior>();if(interior)break;}
 if(!interior){yield return SceneManager.UnloadSceneAsync(scene);loading=false;player.cinematicLocked=false;stage="Interior unavailable";yield break;}
 foreach(var root in gameObject.scene.GetRootGameObjects())if(root!=gameObject&&!player.transform.IsChildOf(root.transform)&&root.activeSelf){suspended.Add(root);root.SetActive(false);}
 inside=true;interior.Bind(this);player.TeleportTo(new Vector3(200,.08f,1.5f),0,true);player.eye.farClipPlane=65;player.cinematicLocked=false;loading=false;stage="Medical research";
 }
 public IEnumerator Leave(){if(!inside||loading)yield break;loading=true;player.cinematicLocked=true;foreach(var root in suspended)if(root)root.SetActive(true);suspended.Clear();inside=false;player.TeleportTo(returnPoint,returnYaw,true);player.eye.farClipPlane=100;yield return SceneManager.UnloadSceneAsync(InteriorScene);interior=null;player.cinematicLocked=false;loading=false;stage="Powered";}
 void OnGUI(){if(!loading)return;GUI.depth=-100;GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=new Color(.75f,.82f,.77f);var style=new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleCenter};GUI.Label(new Rect(0,Screen.height*.45f,Screen.width,80),stage+"\n"+Mathf.RoundToInt(Mathf.Clamp01(progress)*100)+"%",style);}
}
