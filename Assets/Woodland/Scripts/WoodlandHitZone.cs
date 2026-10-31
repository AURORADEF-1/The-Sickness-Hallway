using UnityEngine;
public class WoodlandHitZone:MonoBehaviour {
 public WoodlandAnatomy anatomy;public BodyRegion region;
 public void Receive(float damage,Vector3 point,Vector3 direction){var local=Vector3.Scale(transform.InverseTransformPoint(point),transform.lossyScale);float depth=region==BodyRegion.Chest?(Mathf.Abs(local.x)<.065f&&local.y>0?.9f:.3f):Mathf.Abs(local.x)<.045f?.8f:.2f;anatomy.Hit(region,damage,InjuryKind.Projectile,point,direction,depth);}
}
