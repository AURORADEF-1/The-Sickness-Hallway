using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using System.IO;using System.Text;
public static class DoorwayGeometryReview {
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/MedicalResearch.unity");var room=Object.FindFirstObjectByType<MedicalInterior>();var log=new StringBuilder();log.AppendLine("Door "+room.jammedDoor.position+" parent "+room.jammedDoor.parent.name);foreach(var c in room.GetComponentsInChildren<Collider>(true))if(Mathf.Abs(c.bounds.center.z-room.jammedDoor.position.z)<1.2f)log.AppendLine(c.name+" "+c.bounds);
 room.thornback.gameObject.SetActive(true);var crawl=room.thornback.GetComponent<MedicalThornbackCrawl>();crawl.Prepare();foreach(var t in crawl.visual.GetComponentsInChildren<Transform>())log.AppendLine(t.name+" pos="+t.position+" scale="+t.lossyScale);
 File.WriteAllText("/Users/georgeambrose/Documents/Codex/2026-09-06/whqa/work/doorway-grip-roar/geometry.txt",log.ToString());}
}
