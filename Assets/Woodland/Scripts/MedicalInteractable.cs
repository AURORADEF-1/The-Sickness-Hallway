using UnityEngine;
public class MedicalInteractable:MonoBehaviour {public enum Action{Exit,Supply,JammedDoor,Locked}public Action action;public string item="medical",label="Medical supplies";public int quantity=1;public bool used;}
