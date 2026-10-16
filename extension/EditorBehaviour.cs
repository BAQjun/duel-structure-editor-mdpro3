using System;using System.Collections.Generic;using System.IO;using System.Linq;
using UnityEngine;using MDPro3.DuelEditor;
using GUI=DuelStructure.NativeUI.Gui;using GUILayout=DuelStructure.NativeUI.Layout;using GUIUtility=DuelStructure.NativeUI.Utility;using GUILayoutUtility=DuelStructure.NativeUI.Layout;using Event=DuelStructure.NativeUI.UiEvent;
using DB=MDPro3.Duel.YGOSharp.CardsManager;
namespace DuelStructure {
 public partial class EditorBehaviour:MonoBehaviour {
  public EditorBehaviour(IntPtr p):base(p){}
  static bool isOpen;static int currentPage;static bool open{get=>isOpen;set{isOpen=value;if(!value)DismissChildren();}}static int page{get=>currentPage;set{if(currentPage!=value)DismissChildren();currentPage=value;}}static Vector2 scroll;static string name0="玩家 1",name1="玩家 2",deck0="",deck1="",hand0="5",hand1="5";
  static string search="",nameSearch="",atk="0",def="0",level="4",slot="1",lp="8000",draw="1";static uint code,controller,owner,zone=4,position=1,form,kind,race=1,attribute=1,other,rename,arrows;static bool effect=true,keep=true,selfTrap,turn,regular,forbidden;static uint restrictions,method=2,source=2;static ulong selected;
  static readonly Dictionary<string,uint> choices=new Dictionary<string,uint>();static string popupKey="",popupSearch="";static string[] popupLabels=Array.Empty<string>();static uint[] popupValues=Array.Empty<uint>();static Vector2 popupScroll;static int popupOwnerPage;
  static readonly Dictionary<uint,EditorCard> trapProfiles=new Dictionary<uint,EditorCard>();
  static readonly Dictionary<uint,string> catalog=new Dictionary<uint,string>();static string[] decks=Array.Empty<string>();
  static string folder="",saveName="",folderName="",deletePath="";static bool deleteFolder;static string status="";static uint orderPlayer,orderZone=1;static ulong orderId;static string orderTarget="1",effectValue="1";static uint extraEffect;
  static bool subtypePage;static int editorTab,shapeTab;static Vector2 searchScroll,existingScroll,orderScroll,savesScroll;
  static Rect mainRect;static uint previewCode;static int previewFrame;
  static readonly uint[] otherBits={0x1000,0x800,0x200,0x400,0x200000,0x400000,0x1000000,0x2000000};
  static readonly string[] otherLabels={"调整","二重","灵魂","同盟","反转","卡通","灵摆","特殊召唤专用"};
  internal static readonly string[] restrictionLabels={"卡片被无效","效果无效","不能发动效果","不能攻击","不能宣言攻击","不能直接攻击","不能改变表示形式","不能变成里侧","不被战斗破坏","不被效果破坏","不能成为攻击对象","不能成为效果对象","不能作召唤解放","不能作其他解放","不能回手牌","不能回卡组","不能送去墓地","不能除外","不能作为代价","不能作送墓代价","不能转移控制权","不能被无效","发动不能被无效","效果不能被无效","陷阱可从手牌发动","陷阱盖放回合可发动","可以直接攻击","已是二重效果怪兽","不能作融合素材","不能作同调素材","不能作超量素材","不能作连接素材"};
  static string SaveRoot=>Path.Combine(BepInEx.Paths.GameRootPath,"Data","DuelStructureSaves");
  static string SaveDir=>folder==""?SaveRoot:Path.Combine(SaveRoot,folder);
  public void Update(){
   try{NativeReplay.Tick();Host.Tick();TurnVisuals.Tick();}catch(Exception e){if(Host.Error!=e.Message){Host.Error=e.Message;Plugin.Instance.Log.LogError(e);}if(Host.Session!=null)Host.Session.Paused=true;}
   if(catalog.Count==0&&DB._cards!=null&&DB._cards.Count>0){foreach(var pair in DB._cards)catalog[(uint)pair.Key]=pair.Value.Name;}
   if(Host.Session!=null&&PanelOpen&&MDPro3.Program.instance.ocgcore.greenOn)MDPro3.Program.instance.ocgcore.GreenBackgroundOff();
   if(Host.DuelUIReady&&Foreground()&&Pressed(0x20)&&!Typing())Host.TogglePause();
   if(open&&previewCode!=0&&Host.DuelUIReady&&Time.frameCount>=previewFrame&&MDPro3.Program.instance.ocgcore.DuelBGManager.loaded){var desc=MDPro3.Program.instance.ocgcore.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>().CardDescription;if(!desc.showing){desc.Show(null,null,(int)previewCode,null);previewFrame=Time.frameCount+20;}}
   EditorMenu.Tick();NativeUI.Renderer.BeginFrame();DrawUI();NativeUI.Renderer.EndFrame();Smoke.Tick();
  }
  public static void OpenSetup(){if(Host.Session!=null)return;RefreshDecks();open=true;page=0;popupKey="";status="";Host.Error="";GUI.FocusControl(null);}
  static void RefreshDecks(){try{string root=Path.Combine(BepInEx.Paths.GameRootPath,"Deck");decks=Directory.Exists(root)?Directory.GetFiles(root,"*.ydk",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray():Array.Empty<string>();if(!decks.Contains(deck0))deck0=decks.FirstOrDefault()??"";if(!decks.Contains(deck1))deck1=decks.FirstOrDefault()??"";choices.Remove("deck0");choices.Remove("deck1");Plugin.Instance.Log.LogInfo("EDITOR_DECKS root="+root+" count="+decks.Length);}catch(IOException){Host.Error="无法读取 Deck 文件夹，请刷新卡组重试。";}catch(UnauthorizedAccessException){Host.Error="无法访问 Deck 文件夹。";}}
  public static string[] SelectedDecks=>new[]{deck0,deck1};
  public static void CloseSessionUI(){open=false;page=0;popupKey="";subtypePage=drawDialog=false;deletePath="";previewCode=0;selected=0;choices.Clear();status="";}
  public static bool PanelOpen=>open||regionCardsOpen||popupKey!=""||subtypePage||deletePath!="";public static void ProbeBottom(){scroll.y=100000;}
  public static void ProbeSearch(string term){search=term;}public static void ProbeResultScroll(float offset){searchScroll.y=offset;}public static void ProbeSelect(uint id){NewCard(id);}
  public static void ProbeDrag(Vector2 delta){var title=mainRect.position+new Vector2(20,10);WindowDrag.Move("page"+page,mainRect,title,true,true,32,true);WindowDrag.Move("page"+page,mainRect,title+delta,false,true,32,true);}
  public static void ProbePage(int target){page=target;shapeScroll=Vector2.zero;if(target==2)form=5;}public static void ShowForSmoke(){open=true;page=1;NewCard(89631139);}
  static bool Typing()=>GUIUtility.keyboardControl!=0;
  [System.Runtime.InteropServices.DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
  [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
  [System.Runtime.InteropServices.DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr handle,out uint process);
  static bool spaceDown;static bool Pressed(int key){bool now=(GetAsyncKeyState(key)&0x8000)!=0;bool press=now&&!spaceDown;spaceDown=now;return press;}
  static bool Foreground(){GetWindowThreadProcessId(GetForegroundWindow(),out var id);return id==(uint)System.Diagnostics.Process.GetCurrentProcess().Id;}
  static void Act(Action action){Host.Run(action);if(Host.Error!="")status=Host.Error;else status="";GUI.FocusControl(null);}
  static bool Circle(Rect r,string label,Sprite[] sprites){return !NativeOcclusion.Covers(r)&&NativeUI.Renderer.Circle(r,label,sprites[0],sprites[1],sprites[2]);}
  static uint Num(string s,uint max=uint.MaxValue){if(!uint.TryParse(s,out uint n)||n>max)throw new InvalidDataException("请输入 0～"+max+" 范围的整数");return n;}
  static void Row(string label,ref string text,string hint=""){GUILayout.BeginHorizontal();GUILayout.Label(label,GUILayout.Width(155));text=GUILayout.TextField(text,GUILayout.MinWidth(120));if(hint!=""&&Event.current!=null&&GUILayoutUtility.IsLastHovered())GUI.tooltip=hint;GUILayout.EndHorizontal();}
  static uint Pick(string key,string title,uint value,string[] labels,uint[] values){if(choices.TryGetValue(key,out uint next)){value=next;choices.Remove(key);}int i=Array.IndexOf(values,value);if(i<0){i=0;value=values[0];}if(GUILayout.Setting(title,labels[i])){OpenPopup(key,labels,values);}return value;}
  static void DismissPopup(){popupKey="";popupLabels=Array.Empty<string>();popupValues=Array.Empty<uint>();popupSearch="";popupScroll=Vector2.zero;GUI.FocusControl(null);}
  static void DismissChildren(){regionCardsOpen=false;DismissPopup();subtypePage=drawDialog=false;deletePath="";choices.Clear();}
  static void OpenPopup(string key,string[] labels,uint[] values){popupKey=key;popupOwnerPage=page;if(key=="rename"){var names=catalog.OrderBy(c=>c.Key).ToArray();labels=new[]{"原本卡名"}.Concat(names.Select(c=>c.Value+" ["+c.Key+"]")).ToArray();values=new uint[]{0}.Concat(names.Select(c=>c.Key)).ToArray();}popupLabels=labels;popupValues=values;popupSearch="";popupScroll=Vector2.zero;GUI.FocusControl(null);}
  public static bool PopupOpen=>popupKey!="";public static bool HasPendingChoice=>choices.Count>0;public static void ProbeRename(){OpenPopup("rename",Array.Empty<string>(),Array.Empty<uint>());}public static bool ProbeAttack(string value)=>atk==value;public static bool ProbeRenamed(uint value)=>rename==value;
  static void Popup(){
   if(popupKey=="")return;if(!open||popupOwnerPage!=page){DismissPopup();return;}
   var r=new Rect(Math.Max(20,(Screen.width-620)/2),Math.Max(60,(Screen.height-500)/2),Math.Min(620,Screen.width-40),Math.Min(500,Screen.height-120));r=WindowDrag.Place("popup",r);GUI.enabled=true;GUI.Box(r,"");
   NativeUI.Renderer.Label(new Rect(r.x+12,r.y+8,r.width-115,28),"选择选项");if(NativeUI.Renderer.Button(new Rect(r.x+r.width-90,r.y+6,78,30),"关闭")){DismissPopup();return;}
   popupSearch=NativeUI.Renderer.Input(new Rect(r.x+12,r.y+45,r.width-24,38),popupSearch);
   GUILayout.BeginArea(new Rect(r.x+12,r.y+92,r.width-24,r.height-105));popupScroll=GUILayout.BeginScrollView(popupScroll,GUILayout.Height(r.height-113));
   int displayed=0;for(int i=0;i<popupLabels.Length&&displayed<200;i++)if(popupSearch==""||popupLabels[i].IndexOf(popupSearch,StringComparison.OrdinalIgnoreCase)>=0){displayed++;if(GUILayout.Button(popupLabels[i])){choices[popupKey]=popupValues[i];DismissPopup();break;}}
   GUILayout.EndScrollView();GUILayout.EndArea();
  }

  void DrawUI(){
   try{

    if(NativeOcclusion.FullCover||Host.Session!=null&&!Host.DuelUIReady)return;
    if(Host.InDuel){
     if(Circle(new Rect(365,Screen.height-84,64,64),"编辑局面",EditorIcons.Get("edit"))){open=!open;page=1;}
     if(Circle(new Rect(437,Screen.height-84,64,64),"排序卡组",EditorIcons.Get("deck"))){open=true;page=3;}
     if(Circle(new Rect(509,Screen.height-84,64,64),"当前玩家："+(Host.Session.View+1),EditorIcons.Get("player")))Act(Host.Session.SwitchView);
     if(Circle(new Rect(581,Screen.height-84,64,64),"全局存档",EditorIcons.Get("save"))){open=true;page=4;}
     NativeTransport.Draw();

    }
    if(open&&(Host.InDuel||Host.Session==null&&MDPro3.Program.instance.menu.showing))LegacyDraw();
    Popup();RegionCardPopup();LegacyOtherTypes();ConfirmDelete();if(GUI.tooltip!=""){var mouse=Event.current.mousePosition;float width=Math.Min(450,Math.Max(110,GUI.tooltip.Length*19+24));float height=GUI.tooltip.Length>22?60:38;GUI.Box(new Rect(Math.Clamp(mouse.x+16,4,Math.Max(4,Screen.width-width-4)),Math.Max(4,mouse.y-height-14),width,height),GUI.tooltip);}
   }catch(Exception e){if(Host.Error!=e.Message){Host.Error=e.Message;Plugin.Instance.Log.LogWarning(e);}}
  }
  static void SetupPage(){
   Row("玩家 1 名称",ref name0);Row("玩家 2 名称",ref name1);
   uint d0=(uint)Math.Max(0,Array.IndexOf(decks,deck0)),d1=(uint)Math.Max(0,Array.IndexOf(decks,deck1));
   if(decks.Length>0){var labels=decks.Select(d=>Path.GetRelativePath(Path.Combine(BepInEx.Paths.GameRootPath,"Deck"),d)).ToArray();var vals=Enumerable.Range(0,decks.Length).Select(i=>(uint)i).ToArray();d0=Pick("deck0","玩家 1 卡组",d0,labels,vals);d1=Pick("deck1","玩家 2 卡组",d1,labels,vals);deck0=decks[d0];deck1=decks[d1];}else GUILayout.Label("Deck 文件夹中没有可选的 .ydk 卡组");
   Row("玩家 1 起手数量",ref hand0);Row("玩家 2 起手数量",ref hand1);
   if(GUILayout.Button("载入存档")){page=4;Directory.CreateDirectory(SaveRoot);}
   GUILayout.Space(18);
   if(GUILayout.Button("开始对局")){Act(()=>{if(name0.Length>100||name1.Length>100)throw new InvalidDataException("玩家名称最多 100 个字符");var setup=EditorSetup.FromDecks(deck0,deck1,Num(hand0,254),Num(hand1,254),id=>(uint)DB.Get((int)id,false).Type);setup.Names=new[]{name0,name1};Host.Start(setup);page=1;open=true;NewCard(0);});}
  }
  static string CardName(uint id)=>catalog.TryGetValue(id,out var name)?name:"卡片 "+id;
  static string Region(uint loc,uint seq=0)=>loc==1?"卡组":loc==2?"手牌":loc==4?"怪兽区 "+(seq+1):loc==8?(seq==5?"场地区":seq>=6?"灵摆区":"魔陷区 "+(seq+1)):loc==16?"墓地":loc==32?"除外":loc==64?"额外卡组":"叠放素材";
  static void NewCard(uint id){ResetAttachments();selected=0;code=id;previewCode=id;previewFrame=Time.frameCount+4;form=kind=other=rename=arrows=restrictions=0;race=attribute=1;controller=owner=(uint)(Host.Session?.View??0);zone=4;position=1;slot="1";setTurn=regular=turn=forbidden=selfTrap=false;method=2;source=2;effect=true;keep=true;ApplyBase();}
  static void ApplyBase(){if(code==0)return;var c=DB.Get((int)code,false);atk=Math.Max(0,c.Attack).ToString();def=Math.Max(0,c.Defense).ToString();level=Math.Max(1,c.Level).ToString();race=(uint)Math.Max(1,c.Race);attribute=(uint)Math.Max(1,c.Attribute);effect=(c.Type&0x20)!=0;kind=(uint)c.Type&0x48020c0;other=(uint)c.Type&otherBits.Aggregate(0u,(a,b)=>a|b);arrows=(uint)c.LinkMarker;}
  static void LoadCard(EditorCard c){NewCard(c.Code);selected=c.Id;form=c.Form;controller=c.Player;owner=c.Owner;zone=c.Location==8&&c.Sequence==5?0x100u:c.PendulumZone!=0?0x200u:c.Location;position=c.Position;slot=(zone==0x200?(c.Sequence==0?1u:2u):c.Location==1||c.Location==64?1u:c.Sequence+1).ToString();atk=((int)c.Attack).ToString();def=((int)c.Defense).ToString();level=Math.Max(1,c.Level).ToString();race=c.Race;attribute=c.Attribute;kind=c.EffectiveType&0x48020c0;other=c.EffectiveType&otherBits.Aggregate(0u,(a,b)=>a|b);effect=(c.EffectiveType&0x20)!=0;keep=(c.MonsterFlags&0x80000000)!=0;selfTrap=c.Form==4&&c.NameCode!=0;rename=c.Form>=5?c.NameCode:0;arrows=c.LinkMarker;restrictions=c.Restrictions;setTurn=(c.States&0x10)!=0;forbidden=(c.States&0x4000000)!=0;turn=c.Location==16?(c.Flags&16)!=0:(c.Flags&14)!=0||(c.States&0x10)!=0;regular=(c.Flags&1)!=0;method=c.SummonMethod==0?2:c.SummonMethod;source=c.SummonSource==0?2:c.SummonSource;page=1;}
  static uint EffectiveType{get{var baseType=code==0?1u:(uint)DB.Get((int)code,false).Type;if(form==1)return 0x20002;if(form==2)return 0x20004;if(form==4)return TrapProfile().EffectiveType;if(form>=5)return 1|kind|other|(effect?0x20u:kind!=0?0u:0x10u);return baseType;}}
  static EditorCard TrapProfile(){if(!trapProfiles.TryGetValue(code,out var p)){if(EditorNative.editor_native_trap_profile(code,out p)==0)p=default;trapProfiles[code]=p;}return p;}
  static bool NormalAllowed(uint type)=>form!=4&&form!=6&&(type&1)!=0&&(type&0x68060c0)==0&&(type&0x4000)==0;
  static void CardPage(){
   var session=Host.Session;if(session==null){page=0;return;}
   if(GUILayout.Button("新增卡片"))NewCard(0);
   Row("卡名 / 卡号搜索",ref search);
   if(search.Length>0&&selected==0){searchScroll=GUILayout.BeginScrollView(searchScroll,GUILayout.Height(180));foreach(var c in catalog.Where(c=>c.Value.Contains(search)||c.Key.ToString().Contains(search)).Take(50)){bool choose=GUILayout.Button(c.Value+" ["+c.Key+"]",GUILayout.Height(32));if(GUILayoutUtility.IsLastHovered()){previewCode=c.Key;previewFrame=Time.frameCount+1;var desc=MDPro3.Program.instance.ocgcore.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>().CardDescription;if(desc.data==null||desc.data.Id!=(int)c.Key)desc.Show(null,null,(int)c.Key,null);}if(choose){NewCard(c.Key);search="";searchScroll=Vector2.zero;break;}}GUILayout.EndScrollView();}
   {GUILayout.Label(code==0?"请选择一张卡片":CardName(code)+" ["+code+"]"+(selected!=0?" · 编辑现有卡片":""));
    if(GUILayout.Button("卡片形态 / 状态 / 属性")){page=2;scroll=Vector2.zero;}
    controller=Pick("control","控制方玩家",controller,new[]{"玩家 1","玩家 2"},new uint[]{0,1});owner=Pick("owner","原本拥有者",owner,new[]{"玩家 1","玩家 2"},new uint[]{0,1});
    uint type=EffectiveType;bool monster=(type&1)!=0;var zones=new List<uint>();var labels=new List<string>();Action<uint,string> add=(n,label)=>{zones.Add(n);labels.Add(label);};var baseType=code==0?1u:(uint)DB.Get((int)code,false).Type;
    if(form==1||form==2)add(8,"魔法陷阱区");else if(form==4||form>=5)add(4,"怪兽区");else{
     if(monster)add(4,"怪兽区");if((baseType&0x4806040)==0){add(2,"手牌");add(1,"卡组");}
     if((baseType&0x80000)!=0)add(0x100,"场地区");else if((baseType&6)!=0)add(8,"魔法陷阱区");
     if((baseType&0x1000000)!=0)add(0x200,"灵摆区");if((baseType&0x5802040)!=0)add(64,"额外卡组");if((baseType&0x4000)==0){add(16,"墓地");add(32,"除外");}
    }
    zone=Pick("zone","放置区域",zone,labels.ToArray(),zones.ToArray());
    if(monster&&zone==4){var ms=new List<uint>{1};var mt=new List<string>{"是否特殊召唤"};if(NormalAllowed(type)){ms.Add(2);mt.Add("是否通常召唤");}if((type&0x4004000)==0&&(position&5)!=0){ms.Add(3);mt.Add("是否反转召唤");}method=Pick("method","召唤方式",method,mt.ToArray(),ms.ToArray());
     if(form==4&&selfTrap){method=1;source=8;}else if(method==1){var sources=EditorSummonRules.SpecialSources(type,form);var sourceLabels=sources.Select(loc=>loc==8?"魔法陷阱区":Region(loc)).ToArray();source=Pick("source","特殊召唤来源",source,sourceLabels,sources);}else source=method==2?2u:4u;
     turn=GUILayout.Toggle(turn,"是否当回合召唤");if((type&0x68020c0)!=0)regular=GUILayout.Toggle(regular,"是否正规召唤");else regular=false;
    }else if(zone==16)turn=GUILayout.Toggle(turn,"是否当回合送去墓地");else if(zone==8||zone==0x100)turn=GUILayout.Toggle(turn,"是否当回合盖放");else turn=false;
    bool exZone=zone==4&&method==1&&source==64&&form!=4&&form!=6;bool numbered=zone==1||zone==64||zone==4||zone==8||zone==0x200;
    if(numbered)Row("位置",ref slot,zone==4?(exZone?"1～5：主要怪兽区；6、7：额外怪兽区":"1～5：主要怪兽区的位置"):zone==8?"1～5：魔法与陷阱区的位置":zone==0x200?"1：左侧灵摆区；2：右侧灵摆区":"1：顶端；2：底端；3：随机插入，保留其他卡片顺序");
    if(zone==4){if((type&0x4000000)!=0)position=1;else position=Pick("pos","表示形式",position,new[]{"表侧攻击表示","表侧守备表示","里侧守备表示"},new uint[]{1,4,8});}else if(zone==64){if((baseType&0x1000000)==0)position=8;else if((baseType&0x802040)==0)position=1;else position=Pick("posExtra","表示形式",position,new[]{"里侧表示","表侧表示"},new uint[]{8,1});}else if(zone==8||zone==0x100){if((type&(0x20000|0x80000))!=0)position=Pick("posBack","表示形式",position,new[]{"盖放","表侧放置"},new uint[]{8,1});else position=8;}else if(zone==32)position=Pick("posBan","表示形式",position,new[]{"表侧表示","里侧表示"},new uint[]{1,8});else position=1;
    GUILayout.Space(14);
    if(GUILayout.Button(selected==0?"生成卡片":"保存修改")){Act(Submit);}
    if(selected!=0&&GUILayout.Button("删除这张卡片")){Act(()=>{var cmd=EditCommand.Create(3);cmd.Target=selected;Apply(cmd);NewCard(0);});}
    if(GUILayout.Button("继续对局")){open=false;session.Paused=false;}
   }

  }
  static void PlayersPage(){   GUILayout.Space(15);controller=Pick("lpPlayer","操作玩家",controller,new[]{"玩家 1","玩家 2"},new uint[]{0,1});Row("目标 LP",ref lp);if(GUILayout.Button("设置 LP"))Act(()=>{var c=EditCommand.Create(2);c.Player=controller;c.Value=Num(lp,int.MaxValue);Apply(c);});Row("立即抽卡数量",ref draw);if(GUILayout.Button("控制方立即抽卡"))Act(()=>{var c=EditCommand.Create(8);c.Player=controller;c.Value=Num(draw,254);Apply(c);});if(GUILayout.Button("退出编辑对局"))Act(()=>{Host.End();page=0;});
  }
  static void ExistingPage(){var session=Host.Session;controller=Pick("existingPlayer","玩家",controller,new[]{"玩家 1","玩家 2"},new uint[]{0,1});existingScroll=GUILayout.BeginScrollView(existingScroll,GUILayout.Height(280));foreach(var c in session.Cards().Where(c=>c.Player==controller&&c.Location!=1&&c.Location!=64)){if(GUILayout.Button(Region(c.Location,c.Sequence)+" · "+CardName(c.EffectiveCode==0?c.Code:c.EffectiveCode),GUILayout.Height(32))){LoadCard(c);editorTab=0;scroll=Vector2.zero;}}GUILayout.EndScrollView();}
  static void EditorTabBar(){GUILayout.BeginHorizontal();if(GUILayout.Button("卡片编辑")){editorTab=0;scroll=Vector2.zero;}if(GUILayout.Button("已有卡片")){editorTab=1;scroll=Vector2.zero;}if(GUILayout.Button("玩家操作")){editorTab=2;scroll=Vector2.zero;}GUILayout.EndHorizontal();GUILayout.Space(10);}
  static void EditorPage(){if(editorTab==0)CardPage();else if(editorTab==1)ExistingPage();else PlayersPage();}

  static void ShapeTabBar(){GUILayout.BeginHorizontal();if(GUILayout.Button("形态与属性")){shapeTab=0;scroll=Vector2.zero;}if(GUILayout.Button("状态与限制")){shapeTab=1;scroll=Vector2.zero;}if(GUILayout.Button("更多效果")){shapeTab=2;scroll=Vector2.zero;}GUILayout.EndHorizontal();}
  static void ShapePage(){if(code==0){page=1;return;}if(shapeTab==0)ShapeOptions();else if(shapeTab==1)StateOptions();else MoreEffects();GUILayout.Space(16);if(GUILayout.Button("返回卡片编辑")){page=1;scroll=Vector2.zero;}}
  static void OtherTypesDialog(){if(!subtypePage)return;GUI.enabled=true;var r=new Rect((Screen.width-460)/2,(Screen.height-470)/2,460,470);GUI.Box(r,"");GUILayout.BeginArea(new Rect(r.x+16,r.y+16,r.width-32,r.height-32));GUILayout.Label("其他怪兽类型");for(int i=0;i<otherBits.Length;i++){bool enabled=GUILayout.Toggle((other&otherBits[i])!=0,otherLabels[i]);other=enabled?other|otherBits[i]:other&~otherBits[i];}if(GUILayout.Button("返回形态与属性"))subtypePage=false;GUILayout.EndArea();}
  static void ShapeOptions(){if(code==0){page=1;return;}uint baseType=(uint)DB.Get((int)code,false).Type;var values=new List<uint>{0};var labels=new List<string>{"原本卡片形态"};if((baseType&1)!=0){values.AddRange(new uint[]{1,2,5});labels.AddRange(new[]{"怪兽作为永续魔法","怪兽作为永续陷阱","自定义怪兽属性 / 类型 / 卡名"});}else{values.Add(6);labels.Add("魔法 / 陷阱作为自定义怪兽");if((baseType&4)!=0&&TrapProfile().EffectiveType!=0){values.Add(4);labels.Add("自身可变怪兽的陷阱（沿用原设定）");}}
   form=Pick("form","卡片形态",form,labels.ToArray(),values.ToArray());
   if(form==4){selfTrap=GUILayout.Toggle(selfTrap,"是否由自身效果特殊召唤上场");keep=GUILayout.Toggle(keep,"也当作陷阱卡使用");}
   if(form>=5){effect=GUILayout.Toggle(effect,"作为效果怪兽（取消则为效果以外怪兽）");if(form==6)keep=GUILayout.Toggle(keep,"也当作魔法陷阱卡使用");kind=Pick("kind","怪兽类别",kind,new[]{"不属于这五类","融合","同调","超量","连接","仪式"},new uint[]{0,0x40,0x2000,0x800000,0x4000000,0x80});if(GUILayout.Button("其他怪兽类型"))subtypePage=!subtypePage;
    Row("攻击力",ref atk);if(kind==0x4000000){GUILayout.Label("连接箭头");uint[] bits={0x40,0x80,0x100,8,0x20,1,2,4};string[] marks={"↖","↑","↗","←","→","↙","↓","↘"};GUILayout.BeginHorizontal();for(int i=0;i<bits.Length;i++){bool b=GUILayout.Toggle((arrows&bits[i])!=0,marks[i]);arrows=b?arrows|bits[i]:arrows&~bits[i];}GUILayout.EndHorizontal();}else{Row("守备力",ref def);Row(kind==0x800000?"阶级":"等级",ref level);}
    race=Pick("race","种族",race,new[]{"战士","魔法师","天使","恶魔","不死","机械","水","炎","岩石","鸟兽","植物","昆虫","雷","龙","兽","兽战士","恐龙","鱼","海龙","爬虫类","念动力","幻神兽","创造神","幻龙","电子界","幻想魔"},Enumerable.Range(0,26).Select(i=>1u<<i).ToArray());attribute=Pick("attr","属性",attribute,new[]{"地","水","炎","风","光","暗","神"},new uint[]{1,2,4,8,16,32,64});
    Row("卡名搜索",ref nameSearch);var found=catalog.Where(c=>nameSearch==""?c.Key==rename:c.Value.Contains(nameSearch)||c.Key.ToString().Contains(nameSearch)).Take(100).ToArray();if(rename!=0&&!found.Any(c=>c.Key==rename))found=found.Concat(new[]{new KeyValuePair<uint,string>(rename,CardName(rename))}).ToArray();var names=new[]{"原本卡名"}.Concat(found.Select(c=>c.Value+" ["+c.Key+"]")).ToArray();var ids=new uint[]{0}.Concat(found.Select(c=>c.Key)).ToArray();rename=Pick("rename","卡名",rename,names,ids);
   }

  }
  static void StateOptions(){uint baseType=(uint)DB.Get((int)code,false).Type;   GUILayout.Space(12);GUILayout.Label("状态与限制");uint type=EffectiveType;bool monster=(type&1)!=0;forbidden=GUILayout.Toggle(forbidden,"被禁止令适用");for(int i=0;i<32;i++){bool onlyMonster=(i>=3&&i<=8)||i==10||i==12||i==26||i>=28;bool trap=i==24||i==25;if((onlyMonster&&!monster)||(trap&&(baseType&4)==0)||(i==27&&(type&0x800)==0)||((i==6||i==7)&&(type&0x4000000)!=0)||(i==7&&(type&0x4000)!=0))continue;bool b=GUILayout.Toggle((restrictions&(1u<<i))!=0,restrictionLabels[i]);restrictions=b?restrictions|(1u<<i):restrictions&~(1u<<i);}

  }
  static void MoreEffects(){   if(selected!=0){GUILayout.Space(10);GUILayout.Label("更多效果状态");extraEffect=Pick("extra","状态效果",extraEffect,Effects.Labels,Effects.Codes);Row("效果数值",ref effectValue);if(GUILayout.Button("添加 / 更新状态效果"))Act(()=>{if(!int.TryParse(effectValue,out int val))throw new InvalidDataException("请输入有符号整数");var c=EditCommand.Create(6);c.Target=selected;c.Code=extraEffect;c.Value=(uint)val;c.Position=1;Apply(c);});var records=new EditorEffect[1024];uint count=EditorNative.editor_list_custom_effects(Host.Session.Handle,records,1024);for(int i=0;i<count;i++)if(records[i].CardId==selected){var e=records[i];GUILayout.BeginHorizontal();GUILayout.Label(Effects.Name(e.Code)+" · "+e.Value);if(GUILayout.Button("删除",GUILayout.Width(70)))Act(()=>{var c=EditCommand.Create(6);c.Target=selected;c.Code=e.Code;c.Position=0;Apply(c);});GUILayout.EndHorizontal();}}

  }

  static void Submit(){if(code==0)throw new InvalidOperationException("请先搜索并选择一张卡片");if(zone==4&&method==1){var validSources=EditorSummonRules.SpecialSources(EffectiveType,form);if(!validSources.Contains(source))source=validSources[0];}var cmd=EditCommand.Create(selected==0?1u:4u);cmd.Code=code;cmd.Target=selected;cmd.Player=controller;cmd.Owner=owner;cmd.Location=zone;cmd.Form=form;cmd.Position=position;cmd.Race=race;cmd.Attribute=attribute;cmd.Attack=Num(atk,int.MaxValue);cmd.Defense=Num(def,int.MaxValue);cmd.Level=Num(level,13);cmd.MonsterFlags=1;cmd.NameCode=form==4&&selfTrap?1u:0;cmd.Restrictions=restrictions;cmd.States=(forbidden?0x4000000u:0)|(zone==4&&(position&8)!=0&&setTurn?0x10u:0);
   if(form==4){cmd.MonsterFlags=keep?3u:1u;}if(form>=5){cmd.MonsterFlags=EffectiveType|(form==6&&keep?0x80000000u:0);cmd.NameCode=rename;if(kind==0x4000000){if(arrows==0)throw new InvalidDataException("请至少选择一个连接箭头");cmd.Defense=arrows;cmd.Level=(uint)Enumerable.Range(0,9).Count(i=>(arrows&(1u<<i))!=0);}}
   uint type=EffectiveType;if(zone==4){uint max=method==1&&source==64&&form!=4&&form!=6?7u:5u;cmd.Sequence=Num(slot,max);if(cmd.Sequence==0)throw new InvalidDataException("位置从 1 开始");cmd.Sequence--;cmd.Value=method==1?32u:method==2?64u:128u;cmd.Value|=(method==1?source:method==2?2u:4u)<<8;if(turn)cmd.Value|=method==1?4u:method==2?2u:8u;if(regular&&(type&0x68020c0)!=0)cmd.Value|=1;
   }else if(zone==16){if(turn)cmd.Value|=16;}else if(zone==8||zone==0x100){if(turn)cmd.States|=0x10;}
   if(zone==1||zone==64||zone==8||zone==0x200){uint max=zone==8?5u:zone==0x200?2u:3u;cmd.Sequence=Num(slot,max);if(cmd.Sequence==0)throw new InvalidDataException("位置从 1 开始");cmd.Sequence--;}
   bool generated=selected==0;SubmitWithAttachments(cmd);status=generated?"卡片已生成":"修改已保存";
  }
  static void Apply(EditCommand c){int result=Host.Session.Apply(c);if(result!=0)throw new InvalidOperationException(new[]{"成功","编辑器未启用","当前核心尚未到可编辑时点","场面已经变化，请重试","参数无效","找不到卡片","该卡片不能放置在此位置","位置已被占用"}[Math.Min(result,7)]);}
  static void OrderPage(){var s=Host.Session;if(s==null){page=0;return;}orderPlayer=Pick("orderPlayer","玩家",orderPlayer,new[]{"玩家 1","玩家 2"},new uint[]{0,1});orderZone=Pick("orderZone","卡组区域",orderZone,new[]{"主卡组","额外卡组"},new uint[]{1,64});var list=s.Cards().Where(c=>c.Player==orderPlayer&&c.Location==orderZone);list=orderZone==1?list.OrderByDescending(c=>c.Sequence):list.OrderBy(c=>c.Sequence);var cards=list.ToArray();orderScroll=GUILayout.BeginScrollView(orderScroll,GUILayout.Height(300));for(int i=0;i<cards.Length;i++){var card=cards[i];if(GUILayout.Button((orderId==card.Id?"▶ ":"")+(i+1)+" · "+CardName(card.EffectiveCode==0?card.Code:card.EffectiveCode)+(orderZone==64?(card.Position==8?" · 里侧":" · 表侧"):""))){orderId=card.Id;LoadCard(card);page=3;}}GUILayout.EndScrollView();
   Row("移至序号",ref orderTarget);if(GUILayout.Button("移动选中卡片"))Act(()=>{var c=EditCommand.Create(5);c.Target=orderId;c.Player=orderPlayer;uint n=Num(orderTarget,(uint)cards.Length);if(n==0)throw new InvalidDataException("序号从 1 开始");c.Sequence=n-1;Apply(c);});if(GUILayout.Button("编辑选中卡片")){var c=cards.FirstOrDefault(c=>c.Id==orderId);if(c.Id!=0)LoadCard(c);}if(GUILayout.Button("删除选中卡片"))Act(()=>{var c=EditCommand.Create(3);c.Target=orderId;Apply(c);orderId=0;});if(orderZone==1&&GUILayout.Button("立即洗牌"))Act(()=>{var c=EditCommand.Create(7);c.Player=orderPlayer;Apply(c);});
  }
  static string SafeName(string s){s=s.Trim();if(s==""||s=="."||s==".."||s.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new InvalidDataException("请输入有效名称");return s;}
  static string Guard(string path){string root=Path.GetFullPath(SaveRoot)+Path.DirectorySeparatorChar;string p=Path.GetFullPath(path);if(!p.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("存档路径超出允许范围");return p;}
  static void SavesPage(){Directory.CreateDirectory(SaveRoot);var dirs=Directory.GetDirectories(SaveRoot).Select(Path.GetFileName).OrderBy(n=>n).ToArray();uint f=(uint)Math.Max(0,Array.IndexOf(dirs,folder)+1);f=Pick("folder","存档文件夹",f,new[]{"根文件夹"}.Concat(dirs).ToArray(),Enumerable.Range(0,dirs.Length+1).Select(i=>(uint)i).ToArray());folder=f==0?"":dirs[f-1];Row("新文件夹名称",ref folderName);if(GUILayout.Button("建立文件夹"))Act(()=>{Directory.CreateDirectory(Guard(Path.Combine(SaveRoot,SafeName(folderName))));folder=folderName.Trim();folderName="";});if(folder!=""&&GUILayout.Button("删除当前文件夹")){deletePath=SaveDir;deleteFolder=true;}
   if(Host.Session!=null){Row("存档名称",ref saveName);if(GUILayout.Button("储存当前时点"))Act(()=>{Host.Session.Save(Guard(Path.Combine(SaveDir,SafeName(saveName)+".mdges")),saveName);status="存档已保存";});}
   savesScroll=GUILayout.BeginScrollView(savesScroll,GUILayout.Height(280));foreach(var path in Directory.GetFiles(SaveDir,"*.mdges").OrderByDescending(File.GetLastWriteTimeUtc)){GUILayout.BeginHorizontal();GUILayout.Label(Path.GetFileNameWithoutExtension(path));if(GUILayout.Button("读取",GUILayout.Width(75)))Act(()=>{if(Host.Session==null){var tmp=new EditorSetup();for(int i=0;i<8;i++)tmp.Seeds[i]=(uint)(i+1);Host.Start(tmp);}Host.Session.Load(path);page=1;});if(GUILayout.Button("删除",GUILayout.Width(75))){deletePath=path;deleteFolder=false;}GUILayout.EndHorizontal();}GUILayout.EndScrollView();
  }
  static void ConfirmDelete(){if(deletePath=="")return;GUI.enabled=true;var r=new Rect((Screen.width-580)/2,(Screen.height-170)/2,580,170);r=WindowDrag.Place("delete",r);GUI.Box(r,"");GUILayout.BeginArea(new Rect(r.x+12,r.y+12,r.width-24,r.height-24));GUILayout.Label("删除"+(deleteFolder?"文件夹及其中存档":"存档")+"："+Path.GetFileName(deletePath));GUILayout.BeginHorizontal();if(GUILayout.Button("确定删除")){string p=deletePath;bool dir=deleteFolder;Act(()=>{p=Guard(p);if(dir){foreach(var f in Directory.GetFiles(p,"*",SearchOption.AllDirectories))if(Path.GetExtension(f)!=".mdges")throw new InvalidDataException("文件夹含非存档文件，无法整批删除");Directory.Delete(p,true);folder="";}else File.Delete(p);deletePath="";});}if(GUILayout.Button("取消"))deletePath="";GUILayout.EndHorizontal();GUILayout.EndArea();}
 }

}
