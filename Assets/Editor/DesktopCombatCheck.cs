using UnityEngine;using UnityEditor;using System.IO;
public static class DesktopCombatCheck {
 public static void Run(){var text=new System.Text.StringBuilder();System.Action<bool,string> check=(ok,label)=>text.AppendLine(label+": "+(ok?"PASS":"FAIL"));
 var R=WoodlandNPCInjury.Reaction.Kneel;
 check(WoodlandNPCInjury.PreserveReaction(R,WoodlandNPCInjury.Reaction.GuardWound,true)==R,"Follow-up hit cannot cancel kneel");
 check(WoodlandNPCInjury.PreserveReaction(WoodlandNPCInjury.Reaction.WallBrace,WoodlandNPCInjury.Reaction.GuardWound,true)==WoodlandNPCInjury.Reaction.WallBrace,"Follow-up hit cannot cancel wall brace");
 check(WoodlandNPCInjury.PreserveReaction(WoodlandNPCInjury.Reaction.GuardWound,R,true)==R,"Leg hit escalates reaction");
 var g=new GameObject("Combat checks");g.SetActive(false);var a=g.AddComponent<WoodlandAnatomy>();a.Hit(BodyRegion.LeftThigh,18,InjuryKind.Projectile,Vector3.zero,Vector3.forward);float tissue=a.Region(BodyRegion.LeftThigh).tissue,blood=a.condition.blood,bleed=a.Bleeding;a.Hit(BodyRegion.LeftThigh,8,InjuryKind.Projectile,Vector3.zero,Vector3.forward);check(a.Region(BodyRegion.LeftThigh).tissue<tissue&&a.condition.blood<blood&&a.Bleeding>bleed,"Repeat hits accumulate damage and blood loss");
 var inv=g.AddComponent<WoodlandInventory>();inv.persist=false;inv.definitions=AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath("5c64a8d316dea4eec93f84125d67fe03"));inv.Initialise(false);check(inv.Count("556")==300,"Fresh demo has 300 reserve rounds");
 inv.Consume("556",280);inv.data.demoAmmoRevision=0;inv.ApplyDemoAmmoTopUp();check(inv.Count("556")==300,"Existing low-ammo save topped up");inv.Consume("556",30);inv.ApplyDemoAmmoTopUp();check(inv.Count("556")==270,"Save top-up runs only once");check(inv.LoadRifle(30)==30&&inv.Count("556")==240,"Reload consumes reserve normally");check(inv.Weight<=inv.Capacity,"Ammo allocation respects backpack capacity");
 Object.DestroyImmediate(g);File.WriteAllText("/Users/georgeambrose/Documents/Codex/2026-09-06/whqa/work/desktop-demo/combat-check.txt",text.ToString());if(text.ToString().Contains("FAIL"))throw new System.Exception(text.ToString());}
}
