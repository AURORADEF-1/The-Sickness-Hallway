using UnityEngine;
public class WoodlandLoot:MonoBehaviour {
 public FieldItem item;public bool claimed;
 public static WoodlandLoot Spawn(FieldDrop drop,string label){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Dropped "+label;g.transform.position=new Vector3(drop.x,drop.y,drop.z);g.transform.localScale=new Vector3(.12f,.07f,.10f);var loot=g.AddComponent<WoodlandLoot>();loot.item=drop.item;return loot;}
 public bool Take(WoodlandInventory inventory){if(claimed||item==null||inventory.Weight+inventory.defs[item.kind].weight*item.quantity>inventory.Capacity)return false;claimed=true;inventory.data.dropped.RemoveAll(d=>d.item.id==item.id);item.container="carry";inventory.data.items.Add(item);inventory.Notify("Picked up "+inventory.defs[item.kind].name);Destroy(gameObject);return true;}
}
