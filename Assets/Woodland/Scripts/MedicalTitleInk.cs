using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin, straight runs of red ink growing from the real lower glyph contours.
/// The font atlas is sampled only when it changes; the animation then adds a
/// few inexpensive quads to the title's existing camera-space UI draw call.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Text))]
public sealed class MedicalTitleInk : UnityEngine.UI.BaseMeshEffect {
 struct Streak {
  public Vector2 root,inkUV;
  public float width,length,delay;
 }
 readonly List<Streak> streaks=new List<Streak>(36);
 readonly UIVertex[] quad=new UIVertex[4];
 UnityEngine.UI.Text title;
 Texture2D atlasSnapshot;
 Texture atlasSource;
 int atlasWidth,atlasHeight,cachedVertexCount;
 Vector2 cachedSize;
 float age,duration=17.507f;
 bool needsGlyphs=true;
 public int StreakCount=>streaks.Count;
 public float MaximumLength {get;private set;}

 protected override void OnEnable(){
  base.OnEnable();title=GetComponent<UnityEngine.UI.Text>();
  Font.textureRebuilt+=FontRebuilt;needsGlyphs=true;
 }
 protected override void OnDisable(){
  Font.textureRebuilt-=FontRebuilt;ReleaseSnapshot();base.OnDisable();
 }
 protected override void OnRectTransformDimensionsChange(){
  base.OnRectTransformDimensionsChange();needsGlyphs=true;
 }
 void FontRebuilt(Font font){
  if(title&&title.font==font){needsGlyphs=true;ReleaseSnapshot();}
 }
 public void SetAge(float seconds,float themeDuration){
  age=Mathf.Max(0,seconds);duration=themeDuration>3?themeDuration:17.507f;
  if(graphic)graphic.SetVerticesDirty();
 }
 public override void ModifyMesh(UnityEngine.UI.VertexHelper mesh){
  if(!IsActive()||!title||mesh.currentVertCount<4)return;
  var source=graphic.mainTexture;
  if(!source)return;
  var size=((RectTransform)transform).rect.size;
  if(source!=atlasSource||source.width!=atlasWidth||source.height!=atlasHeight||
     mesh.currentVertCount!=cachedVertexCount||size!=cachedSize)needsGlyphs=true;
  if(needsGlyphs)BuildStreaks(mesh,source,size);
  MaximumLength=0;
  UIVertex first=new UIVertex();mesh.PopulateUIVertex(ref first,0);
  Color color=first.color;
  foreach(var streak in streaks){
   // Different starting moments and flow rates, but every run advances until
   // the supplied theme ends. No sideways wobble or detached falling drops.
   float start=2.55f+streak.delay;
   float progress=Mathf.Clamp01((age-start)/Mathf.Max(.1f,duration-start));
   float length=streak.length*Mathf.Pow(progress,.93f);
   MaximumLength=Mathf.Max(MaximumLength,length);
   if(length<.2f)continue;
   float half=streak.width*.5f;
   Vector2 top=streak.root+Vector2.up*.8f;
   Vector2 bottom=streak.root-Vector2.up*length;
   // A narrow straight tapered end reads as ink pulled downward by gravity.
   float tip=Mathf.Max(.28f,half*.52f);
   AddVertex(0,new Vector2(top.x-half,top.y),streak.inkUV,color);
   AddVertex(1,new Vector2(top.x+half,top.y),streak.inkUV,color);
   var lower=color;lower.r*=.74f;lower.g*=.74f;lower.b*=.74f;
   AddVertex(2,new Vector2(bottom.x+tip,bottom.y),streak.inkUV,lower);
   AddVertex(3,new Vector2(bottom.x-tip,bottom.y),streak.inkUV,lower);
   mesh.AddUIVertexQuad(quad);
  }
 }
 void AddVertex(int index,Vector2 point,Vector2 uv,Color color){
  quad[index]=UIVertex.simpleVert;quad[index].position=point;
  quad[index].uv0=uv;quad[index].color=color;
 }
 void BuildStreaks(UnityEngine.UI.VertexHelper mesh,Texture source,Vector2 size){
  needsGlyphs=false;streaks.Clear();cachedVertexCount=mesh.currentVertCount;cachedSize=size;
  if(source!=atlasSource||source.width!=atlasWidth||source.height!=atlasHeight||!atlasSnapshot){
   ReleaseSnapshot();atlasSource=source;atlasWidth=source.width;atlasHeight=source.height;
   // Dynamic font atlases are not necessarily CPU-readable. Read once after
   // font rebuild, preserving the camera's target and active render texture.
   var previous=RenderTexture.active;
   var target=RenderTexture.GetTemporary(atlasWidth,atlasHeight,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
   try{
    Graphics.Blit(source,target);RenderTexture.active=target;
    atlasSnapshot=new Texture2D(atlasWidth,atlasHeight,TextureFormat.RGBA32,false,true);
    atlasSnapshot.ReadPixels(new Rect(0,0,atlasWidth,atlasHeight),0,0);atlasSnapshot.Apply(false,false);
   }finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);}
  }
  if(!atlasSnapshot)return;
  int glyph=0;
  for(int offset=0;offset+3<cachedVertexCount;offset+=4){
   var v=new UIVertex[4];for(int n=0;n<4;n++)mesh.PopulateUIVertex(ref v[n],offset+n);
   float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
   for(int n=0;n<4;n++){minX=Mathf.Min(minX,v[n].position.x);maxX=Mathf.Max(maxX,v[n].position.x);minY=Mathf.Min(minY,v[n].position.y);maxY=Mathf.Max(maxY,v[n].position.y);}
   float width=maxX-minX,height=maxY-minY;if(width<2||height<2)continue;
   // Resolve real quad corners instead of assuming the font atlas orientation.
   Vector2 bl=Corner(v,minX,minY),br=Corner(v,maxX,minY),tl=Corner(v,minX,maxY),tr=Corner(v,maxX,maxY);
   int columns=width<30?2:3;
   for(int c=0;c<columns;c++){
    float random=Hash(glyph*13+c*17+2);
    float fraction=columns==2?.28f+c*.44f:.18f+c*.31f;
    fraction+=Mathf.Lerp(-.035f,.035f,random);
    float lineWidth=Mathf.Lerp(1.35f,4.1f,Hash(glyph*23+c*7+11));
    // Seek the actual opaque bottom at this x. All three samples must be
    // inside the letter so the entire streak starts attached to its glyph.
    int steps=Mathf.CeilToInt(height*1.5f);
    for(int row=0;row<=steps;row++){
     float y=row/(float)steps;
     float side=lineWidth*.45f/width;
     Vector2 uv=UV(bl,br,tl,tr,fraction,y);
     if(Alpha(uv)<.90f||Alpha(UV(bl,br,tl,tr,fraction-side,y))<.85f||Alpha(UV(bl,br,tl,tr,fraction+side,y))<.85f)continue;
     float rootY=Mathf.Lerp(minY,maxY,y);
     streaks.Add(new Streak{
      root=new Vector2(Mathf.Lerp(minX,maxX,fraction),rootY),inkUV=uv,width=lineWidth,
      length=Mathf.Lerp(355,560,Hash(glyph*31+c*41+3)),delay=Mathf.Lerp(0,1.75f,random)
     });break;
    }
   }
   glyph++;
  }
 }
 float Alpha(Vector2 uv)=>atlasSnapshot.GetPixelBilinear(uv.x,uv.y).a;
 static Vector2 UV(Vector2 bl,Vector2 br,Vector2 tl,Vector2 tr,float x,float y)=>Vector2.Lerp(Vector2.Lerp(bl,br,x),Vector2.Lerp(tl,tr,x),y);
 static Vector2 Corner(UIVertex[] vertices,float x,float y){
  int nearest=0;float distance=float.MaxValue;
  for(int n=0;n<4;n++){float d=Vector2.SqrMagnitude((Vector2)vertices[n].position-new Vector2(x,y));if(d<distance){distance=d;nearest=n;}}
  return vertices[nearest].uv0;
 }
 static float Hash(int n)=>Mathf.Repeat(Mathf.Sin(n*12.9898f)*43758.5453f,1);
 void ReleaseSnapshot(){if(atlasSnapshot)Destroy(atlasSnapshot);atlasSnapshot=null;atlasSource=null;}
}
