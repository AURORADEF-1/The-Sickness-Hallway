using UnityEngine;
[DefaultExecutionOrder(1000)] public class MedicalLowBattery:MonoBehaviour {
 public WoodlandPlayer player;public MedicalInterior interior;public float brightness=1;
 public static float EncounterBeam(float time){float phase=Mathf.Repeat(time,4.6f);if(phase>.65f&&phase<.82f)return .38f;if(phase>2.9f&&phase<3.08f)return .25f;return .012f+.018f*Mathf.PerlinNoise(time*2,2);}
 void LateUpdate(){if(!player||!player.torch)return;float level=player.battery;float cycle=Mathf.Repeat(Time.time,2.7f);bool encounter=interior&&interior.chasing&&!interior.finished&&level<15;brightness=encounter?EncounterBeam(interior.chaseTime):level<15?(cycle<.12f?.04f:cycle<.20f?.75f:cycle<.31f?.08f:cycle<.43f?.9f:Mathf.Lerp(.38f,.8f,Mathf.PerlinNoise(Time.time*5,2))):1;if(encounter){var door=interior.thornback.GetComponent<MedicalThornbackDoorBreak>();if(door&&door.FlashAge>=0&&door.FlashAge<.22f)brightness=Mathf.Max(brightness,.32f);}
 player.torch.intensity=(player.whiteHeadlight?WoodlandPlayer.WhiteTorchPower:WoodlandPlayer.RedTorchPower)*brightness;player.torch.range=encounter?11:player.whiteHeadlight?32:23;}
 void OnDestroy(){if(player)player.ApplyTorchMode();}
}
