using UnityEngine;
public class WoodlandCasing:MonoBehaviour {
 public AudioClip[] impacts;public int ImpactPlayCount{get;private set;}AudioSource audioSource;Rigidbody body;float age;bool landed;
 void Awake(){body=GetComponent<Rigidbody>();audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=1;audioSource.minDistance=1;audioSource.maxDistance=12;}
 public void Launch(Vector3 velocity){age=0;landed=false;gameObject.SetActive(true);body.linearVelocity=velocity;body.angularVelocity=Random.insideUnitSphere*18;}
 void Update(){age+=Time.deltaTime;if(age>8)gameObject.SetActive(false);}
 void OnCollisionEnter(Collision hit){if(landed||age<.08f||hit.collider.isTrigger)return;landed=true;if(impacts!=null&&impacts.Length>0){audioSource.pitch=Random.Range(.92f,1.08f);audioSource.PlayOneShot(impacts[Random.Range(0,impacts.Length)],.28f);ImpactPlayCount++;}}
}
