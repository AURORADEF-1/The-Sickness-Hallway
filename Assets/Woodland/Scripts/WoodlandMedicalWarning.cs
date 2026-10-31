using UnityEngine;
public class WoodlandMedicalWarning:MonoBehaviour {
 public WoodlandMedicalSequence sequence;public WoodlandPlayer player;public bool Active{get;private set;}public float Period{get;private set;}=.8f;
 float nextScan;MedicalInterior inside;
 void Update(){if(!sequence||!player)return;if(Time.time>=nextScan){nextScan=Time.time+.5f;inside=Object.FindFirstObjectByType<MedicalInterior>();}Active=(inside&&!inside.finished)||(sequence.entrance&&Vector3.Distance(player.transform.position,sequence.entrance.position)<24);Period=inside&&inside.chasing?.22f:.8f;}
}
