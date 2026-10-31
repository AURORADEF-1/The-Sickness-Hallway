using UnityEngine;using UnityEngine.InputSystem;using System.Collections;using System.Collections.Generic;using UnityEngine.UI;
public class MedicalInterior:MonoBehaviour {
 public Transform jammedDoor,thornback,bleedingExile,crowbar;public WoodlandHostile[] hostiles;public Light[] lights;public AudioSource atmosphere,threatAudio;public AudioClip clang,force,roar;public Transform[] autoDoors;public Renderer[] lamps;
 public WoodlandMedicalSequence owner;public bool breached,collapseStarted,collapsed,chasing,finished;public int shotsDuringChase;public float chaseTime;int previousCameraMask;float clock;Vector3[] doorRest;Vector3[] doorOpen;float[] doorBlend;float actionTime;bool levering;Transform leverArm;Vector3 leverRest;Quaternion leverRotation;float[] intensities;MaterialPropertyBlock lightBlock;Color previousAmbient;float previousReflection;UnityEngine.Rendering.AmbientMode previousAmbientMode;
 public float EndAge{get;private set;}public bool TitleVisible{get;private set;}
 public float TitleAlpha{get;private set;}public float ComingSoonAlpha{get;private set;}
 public int TitleInkStreakCount=>endingInk?endingInk.StreakCount:0;public float TitleInkLength=>endingInk?endingInk.MaximumLength:0;
 public float ThemeStartedAt{get;private set;}=-1;public int ThemePlayCount{get;private set;}
 public bool ThemePlaying=>endingTheme&&endingTheme.isPlaying;public float ThemeDuration=>endingThemeClip?endingThemeClip.length:0;
 public const float EndingBlackBeat=.65f;
 public const string EndingThemeSourceName="Ending theme - supplied recording";
 GameObject endingCanvas;Text endingTitle,endingComingSoon;MedicalTitleInk endingInk;AudioClip endingThemeClip;AudioSource endingTheme;WoodlandDemoControls endingControls;bool controlsWereEnabled,endingExitRequested;CameraClearFlags endingClearFlags;Color endingBackground;
 readonly List<Canvas> hiddenCanvases=new List<Canvas>();
 struct EndingAudioState {public AudioSource source;public bool muted,playing;}
 readonly List<EndingAudioState> endingAudio=new List<EndingAudioState>();
 public void Bind(WoodlandMedicalSequence flow){endingThemeClip=Resources.Load<AudioClip>("Medical/ThornbackFX/ending-theme");if(endingThemeClip)endingThemeClip.LoadAudioData();foreach(var supply in GetComponentsInChildren<MedicalInteractable>(true))if(supply.action==MedicalInteractable.Action.Supply&&supply.item=="556")supply.quantity=Mathf.Max(supply.quantity,180);lightBlock=new MaterialPropertyBlock();owner=flow;var surfaces=GetComponent<MedicalDoorwaySurfaceDetail>();if(!surfaces)surfaces=gameObject.AddComponent<MedicalDoorwaySurfaceDetail>();surfaces.Configure(this);var low=flow.player.GetComponent<MedicalLowBattery>();if(low)low.interior=this;previousAmbient=RenderSettings.ambientLight;previousAmbientMode=RenderSettings.ambientMode;previousReflection=RenderSettings.reflectionIntensity;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.reflectionIntensity=0;lights=GetComponentsInChildren<Light>(true);var fixtures=new System.Collections.Generic.List<Light>(lights);foreach(var h in hostiles)if(h.flash){fixtures.Remove(h.flash);gameObject.AddComponent<MedicalGunLighting>().Configure(h.flash);}lights=fixtures.ToArray();if(flow.player.muzzleFlash)gameObject.AddComponent<MedicalGunLighting>().Configure(flow.player.muzzleFlash);previousCameraMask=flow.player.eye.cullingMask;doorRest=new Vector3[autoDoors.Length];doorOpen=new Vector3[autoDoors.Length];doorBlend=new float[autoDoors.Length];for(int i=0;i<autoDoors.Length;i++){doorRest[i]=autoDoors[i].position;doorOpen[i]=doorRest[i]+Vector3.right*1.6f;}intensities=new float[lights.Length];for(int i=0;i<lights.Length;i++)intensities[i]=lights[i].intensity;foreach(var h in hostiles){h.player=flow.player;h.gameObject.SetActive(true);}thornback.gameObject.SetActive(false);crowbar.gameObject.SetActive(false);atmosphere.Play();}
 MedicalInteractable Target(){var p=owner.player;if(Physics.Raycast(p.eye.transform.position,p.eye.transform.forward,out var hit,2.7f,~0,QueryTriggerInteraction.Ignore))return hit.collider.GetComponentInParent<MedicalInteractable>();return null;}
 public string Prompt(){if(finished)return "";if(chasing){var opening=thornback.GetComponent<MedicalHumanMimic>();if(opening&&opening.IsHidden)return "";}if(levering)return "Levering the jammed door…";var target=Target();if(target&&!target.used){switch(target.action){case MedicalInteractable.Action.Exit:return "E   Return to woodland";case MedicalInteractable.Action.Supply:return "E   Take "+target.label;case MedicalInteractable.Action.JammedDoor:return owner.player.inventory.Count("crowbar")>0?"E   Use crowbar to force door":"Crowbar required · Check your backpack";case MedicalInteractable.Action.Locked:return "Sealed · No access";}}
 if(chasing)return "Behind you.  Q aim · Left mouse fire · Y drop glow stick";return breached?"Investigate the sound ahead":!owner.player.torch.enabled?"F   Headtorch · B   Backpack · Search for medical supplies":"Search the research wing · F torch · Y glow stick · B backpack";}
 public bool Use(){var t=Target();if(!t||t.used||levering||chasing)return false;switch(t.action){case MedicalInteractable.Action.Exit:StartCoroutine(owner.Leave());return true;case MedicalInteractable.Action.Supply:if(owner.player.inventory.Add(t.item,t.quantity)){t.used=true;t.gameObject.SetActive(false);}else owner.player.inventory.Notify("Backpack full · Make space before taking supplies");return true;case MedicalInteractable.Action.JammedDoor:if(owner.player.inventory.Count("crowbar")>0){t.used=true;StartCoroutine(LeverDoor());}return true;default:return true;}}
 public static float EncounterFixture(float time,int index){float phase=Mathf.Repeat(time,4.6f);return ((phase>.12f&&phase<.28f)||(phase>2.25f&&phase<2.38f))&&index%3==Mathf.FloorToInt(time/4.6f)%3?.20f:0;}
 void Update(){if(!owner)return;float dt=Mathf.Min(Time.deltaTime,.05f);clock+=dt;var p=owner.player;if(finished){UpdateEnding();return;}if(p.anatomy.Incapacitated)return;
 for(int i=0;i<autoDoors.Length;i++){float distance=Vector3.Distance(p.transform.position,doorRest[i]-Vector3.up*1.1f);doorBlend[i]=Mathf.MoveTowards(doorBlend[i],distance<3?1:0,dt*.8f);autoDoors[i].position=Vector3.Lerp(doorRest[i],doorOpen[i],Mathf.SmoothStep(0,1,doorBlend[i]));}
 for(int i=0;i<lights.Length;i++){float cycle=Mathf.Repeat(clock+i*.731f,3.7f+i%3*.49f);float pulse=cycle<.10f?1:cycle<.22f?.02f:cycle<.37f?.72f:cycle<.51f?0:cycle<.83f?.48f:cycle<.91f?1:0;float flicker=pulse*(.7f+.3f*Mathf.PerlinNoise(clock*17,i*4.1f));if(chasing){flicker=EncounterFixture(chaseTime,i);var damaged=thornback.GetComponent<MedicalElectricalDamage>();if(damaged&&damaged.IsBroken(lights[i]))flicker=0;}lights[i].enabled=Vector3.Distance(lights[i].transform.position,p.transform.position)<8;lights[i].intensity=intensities[i]*flicker;if(i<lamps.Length&&lamps[i]){lightBlock.SetColor("_EmissionColor",new Color(.4f,.6f,.48f)*flicker*1.3f);lamps[i].SetPropertyBlock(lightBlock);}}
 if(!collapseStarted&&p.transform.position.z>9){collapseStarted=true;StartCoroutine(BleedingEncounter());}
 if(breached&&!chasing&&p.transform.position.z>35)BeginChase();
 if(chasing){chaseTime+=dt;Vector3 target=p.transform.position;var crawl=thornback.GetComponent<MedicalThornbackCrawl>();crawl.Step(chaseTime,dt,target);if((crawl.Doorway&&crawl.Doorway.Complete)||(chaseTime>55+MedicalHumanMimic.HiddenLeadIn&&!(crawl.Doorway&&crawl.Doorway.Active&&!crawl.Doorway.Complete)))ShowEnding();}
 }
 IEnumerator BleedingEncounter(){
 var a=bleedingExile.GetComponent<WoodlandAnatomy>();var animator=bleedingExile.GetComponentInChildren<Animator>();
 var injury=bleedingExile.GetComponent<WoodlandNPCInjury>();if(injury)injury.bleedVisibility=3;
 // Use the actual torso hit zone so the wound stays attached to the skeleton.
 Transform wound=bleedingExile;foreach(var zone in bleedingExile.GetComponentsInChildren<WoodlandHitZone>())if(zone.region==BodyRegion.Abdomen){wound=zone.transform;break;}
 Vector3 woundPoint=wound==bleedingExile?bleedingExile.position+Vector3.up*.95f:wound.position+bleedingExile.forward*.13f;
 a.Hit(BodyRegion.Abdomen,18,InjuryKind.Projectile,woundPoint,-bleedingExile.forward,.8f);
 a.Region(BodyRegion.Abdomen).externalBleed=2.6f;
 var voice=bleedingExile.GetComponent<MedicalVoice>();if(voice)voice.Plead(Resources.Load<AudioClip>("Medical/ExileHelpMe"));
 a.Region(BodyRegion.LeftThigh).muscle=48;a.condition.pain=Mathf.Max(a.condition.pain,48);
 float t=0;float originalSpeed=animator?animator.speed:1;
 while(t<8&&!a.Incapacitated){
 float dt=Mathf.Min(Time.deltaTime,.05f);t+=dt;var target=owner.player.transform.position;target.x=transform.position.x;target.z=Mathf.Clamp(target.z,9,14);
 Vector3 delta=target-bleedingExile.position;delta.y=0;
 // Alternating short and long steps with a recovery pause, rather than constant sliding.
 float cycle=Mathf.Repeat(t,2.3f);float effort=cycle<.7f?Mathf.Sin(cycle/.7f*Mathf.PI):cycle<1.25f?.08f:cycle<1.8f?.55f*Mathf.Sin((cycle-1.25f)/.55f*Mathf.PI):0;
 float speed=.64f*Mathf.Max(0,effort)*Mathf.Lerp(1,.4f,t/8);
 if(delta.magnitude>1.7f){bleedingExile.position+=delta.normalized*dt*speed;Quaternion facing=Quaternion.LookRotation(delta);bleedingExile.rotation=Quaternion.Slerp(bleedingExile.rotation,facing,dt*3);}
 else speed=0;
 if(animator){animator.SetFloat("Speed",speed,.15f,dt);animator.speed=Mathf.Lerp(.28f,.8f,Mathf.Clamp01(speed/.5f));}
 yield return null;
 }
 if(animator)animator.speed=originalSpeed;
 if(!a.Dead){a.condition.blood=Mathf.Min(a.condition.blood,38);a.Tick(.1f);}collapsed=true;
 }

 IEnumerator LeverDoor(){levering=true;owner.player.cinematicLocked=true;var p=owner.player;p.rifle.gameObject.SetActive(false);p.adsRifle.gameObject.SetActive(false);p.focusRifle.gameObject.SetActive(false);yield return MedicalLeverMotion.Play(this);levering=false;p.cinematicLocked=false;if(p.anatomy.Incapacitated){jammedDoor.localRotation=Quaternion.identity;jammedDoor.GetComponent<MedicalInteractable>().used=false;yield break;}breached=true;atmosphere.PlayOneShot(clang,.6f);p.RenderPose(.1f,0);}
 public void BeginChase(){if(chasing||finished)return;chasing=true;foreach(var voice in GetComponentsInChildren<MedicalVoice>(true))voice.SilenceForEncounter();owner.player.battery=Mathf.Min(owner.player.battery,12);owner.player.inventory.Notify("Headtorch battery low · B for spare battery · Y glow stick");thornback.localPosition=new Vector3(0,0,29.5f);thornback.localRotation=Quaternion.identity;thornback.gameObject.SetActive(true);thornback.GetComponent<MedicalThornbackCrawl>().Begin(this);threatAudio.transform.SetParent(thornback,true);threatAudio.transform.localPosition=Vector3.up*1.8f;threatAudio.Stop();threatAudio.loop=false;threatAudio.clip=null;}
 public void OnShot(){if(chasing&&!finished){shotsDuringChase++;var mimic=thornback.GetComponent<MedicalHumanMimic>();if(mimic)mimic.AfterGunshot();}}
 void ShowEnding(){
  if(finished)return;finished=true;EndAge=0;TitleVisible=false;TitleAlpha=ComingSoonAlpha=0;endingExitRequested=false;
  var p=owner.player;thornback.GetComponent<MedicalThornbackCrawl>().StopPresentation();
  owner.stage="THE SICKNESS";p.cinematicLocked=true;p.objectiveMessage="";
  if(p.fieldKit&&p.fieldKit.Open)p.fieldKit.Close();
  p.rifle.gameObject.SetActive(false);p.adsRifle.gameObject.SetActive(false);p.focusRifle.gameObject.SetActive(false);
  foreach(var h in hostiles)if(h)h.enabled=false;
  // Local source muting cuts reverb and breathing at the same instant as the image.
  // Remember state for the persistent player; never change the global listener.
  foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None)){
   if(source.gameObject.scene!=gameObject.scene&&source.gameObject.scene!=p.gameObject.scene&&!source.transform.IsChildOf(p.transform))continue;
   endingAudio.Add(new EndingAudioState{source=source,muted=source.mute,playing=source.isPlaying});source.mute=true;source.Stop();
  }
  // Load at scene entry, then start on the exact black-cut frame. Create this after
  // the encounter sources have been stopped, so its tail is the only ending audio.
  if(endingThemeClip){
   var music=new GameObject(EndingThemeSourceName);music.transform.SetParent(transform,false);
   endingTheme=music.AddComponent<AudioSource>();endingTheme.playOnAwake=false;endingTheme.loop=false;endingTheme.clip=endingThemeClip;
   endingTheme.volume=.65f;endingTheme.spatialBlend=0;endingTheme.priority=0;endingTheme.bypassEffects=true;endingTheme.bypassListenerEffects=true;endingTheme.bypassReverbZones=true;endingTheme.ignoreListenerPause=true;
   ThemeStartedAt=EndAge;ThemePlayCount++;endingTheme.Play();
  }else Debug.LogWarning("Ending theme is missing: Medical/ThornbackFX/ending-theme",this);
  foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))if(canvas.enabled&&(canvas.gameObject.scene==gameObject.scene||canvas.gameObject.scene==p.gameObject.scene||canvas.transform.IsChildOf(p.transform))){hiddenCanvases.Add(canvas);canvas.enabled=false;}
  endingControls=Object.FindFirstObjectByType<WoodlandDemoControls>();if(endingControls){controlsWereEnabled=endingControls.enabled;var trailer=GetComponent<MedicalTrailerSequence>();if(trailer)controlsWereEnabled|=trailer.ControlsOriginallyEnabled;endingControls.enabled=false;}
  Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
  var camera=p.eye;endingClearFlags=camera.clearFlags;endingBackground=camera.backgroundColor;
  int uiLayer=LayerMask.NameToLayer("UI");if(uiLayer<0)uiLayer=5;
  camera.cullingMask=1<<uiLayer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  // Camera-space UI is visible in both gameplay and the camera-rendered encounter video.
  endingCanvas=new GameObject("THE SICKNESS ending",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));endingCanvas.layer=uiLayer;endingCanvas.transform.SetParent(transform,false);
  var screen=endingCanvas.GetComponent<Canvas>();screen.renderMode=RenderMode.ScreenSpaceCamera;screen.worldCamera=camera;screen.planeDistance=camera.nearClipPlane+.02f;screen.sortingOrder=32760;
  var scaler=endingCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  var black=new GameObject("Hard cut to black",typeof(RectTransform),typeof(Image));black.layer=uiLayer;black.transform.SetParent(endingCanvas.transform,false);Stretch((RectTransform)black.transform);black.GetComponent<Image>().color=Color.black;black.GetComponent<Image>().raycastTarget=false;
  endingTitle=EndingText("THE SICKNESS",128,FontStyle.Bold,new Vector2(0,105),new Vector2(1210,190),new Color(.86f,.018f,.028f,0),uiLayer);
  endingTitle.horizontalOverflow=HorizontalWrapMode.Overflow;endingTitle.verticalOverflow=VerticalWrapMode.Overflow;
  // Extend the actual glyph ink, not disconnected screen-space decoration.
  // This effect adds a small number of quads to the existing camera-space text.
  endingInk=endingTitle.gameObject.AddComponent<MedicalTitleInk>();endingInk.SetAge(0,ThemeDuration);
  endingComingSoon=EndingText("COMING SOON",26,FontStyle.Normal,new Vector2(0,-240),new Vector2(900,60),new Color(.70f,.055f,.065f,0),uiLayer);
  var subtitleOutline=endingComingSoon.gameObject.AddComponent<UnityEngine.UI.Outline>();subtitleOutline.effectColor=Color.black;subtitleOutline.effectDistance=new Vector2(2,-2);subtitleOutline.useGraphicAlpha=true;
  Canvas.ForceUpdateCanvases();
 }
 void UpdateEnding(){
  // Time.deltaTime respects captureDeltaTime, keeping rendered fades and music in sync.
  EndAge+=Time.deltaTime;
  TitleAlpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((EndAge-EndingBlackBeat)/2));
  ComingSoonAlpha=Mathf.SmoothStep(0,1,Mathf.Clamp01((EndAge-3)/1.6f));
  TitleVisible=TitleAlpha>0;
  SetTextAlpha(endingTitle,TitleAlpha);SetTextAlpha(endingComingSoon,ComingSoonAlpha);
  if(endingInk)endingInk.SetAge(EndAge,ThemeDuration);
  if(!endingExitRequested&&EndAge>=5&&((Keyboard.current!=null&&Keyboard.current.enterKey.wasPressedThisFrame)||(Gamepad.current!=null&&Gamepad.current.buttonSouth.wasPressedThisFrame))){endingExitRequested=true;StartCoroutine(owner.Leave());}
 }
 Text EndingText(string words,int size,FontStyle style,Vector2 position,Vector2 dimensions,Color color,int layer){
  var label=new GameObject(words,typeof(RectTransform),typeof(Text));label.layer=layer;label.transform.SetParent(endingCanvas.transform,false);
  var rect=(RectTransform)label.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=dimensions;
  var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=words;text.fontSize=size;text.fontStyle=style;text.alignment=TextAnchor.MiddleCenter;text.color=color;text.raycastTarget=false;return text;
 }
 static void SetTextAlpha(Text text,float alpha){if(!text)return;var color=text.color;color.a=alpha;text.color=color;}
 static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
 void OnDestroy(){
  if(endingTheme)endingTheme.Stop();
  if(owner){RenderSettings.ambientLight=previousAmbient;RenderSettings.ambientMode=previousAmbientMode;RenderSettings.reflectionIntensity=previousReflection;}
  if(owner&&owner.player){owner.player.eye.cullingMask=previousCameraMask;if(finished){owner.player.eye.clearFlags=endingClearFlags;owner.player.eye.backgroundColor=endingBackground;}}
  foreach(var state in endingAudio)if(state.source){state.source.mute=state.muted;if(state.playing&&state.source.loop&&!state.source.transform.IsChildOf(transform))state.source.Play();}
  foreach(var canvas in hiddenCanvases)if(canvas)canvas.enabled=true;
  if(endingControls)endingControls.enabled=controlsWereEnabled;
 }
}
