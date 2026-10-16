using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MDPro3.DuelEditor;
using MDPro3.Servant;
using UnityEngine;
using CardDB=MDPro3.Duel.YGOSharp.CardsManager;

namespace DuelStructure {
 [BepInPlugin("cn.duelstructure.mdpro3","决构编辑器","0.2.1")]
 public class Plugin:BasePlugin {
  public static Plugin Instance;
  public override void Load(){
   Instance=this;
   var dir=Path.GetDirectoryName(GetType().Assembly.Location);
   foreach(var name in new[]{"libunwind.dll","libc++.dll","ocgcore_editor.dll"}) NativeLibrary.Load(Path.Combine(dir,name));
   EditorNative.Register(ReadCard,ReadScript,s=>Log.LogWarning("编辑核心: "+s));
   var harmony=new Harmony("cn.duelstructure.mdpro3");harmony.PatchAll(typeof(Plugin).Assembly);
   AddComponent<EditorBehaviour>();Log.LogInfo("DUEL_EDITOR_READY 0.2.1");
  }
  static NativeCardData ReadCard(uint code){var c=CardDB.Get((int)code,false);var sets=new ushort[16];for(int i=0;i<4;i++)sets[i]=(ushort)((ulong)c.Setcode>>(16*i));return new NativeCardData{Code=(uint)c.Id,Alias=(uint)c.Alias,RuleCode=(uint)c.RuleCode,SetCode=sets,Type=(uint)c.Type,Level=(uint)c.Level,Race=(uint)c.Race,Attribute=(uint)c.Attribute,Attack=c.Attack,Defense=c.Defense,LScale=(uint)c.LScale,RScale=(uint)c.RScale,LinkMarker=(uint)c.LinkMarker};}
  static byte[] ReadScript(string name){name=name.TrimStart('.','/');var bytes=MDPro3.IO.FileManager.GetFile(name);return bytes==null?null:bytes.ToArray();}
 }
 public static class Host {
  public static EditorSession Session;public static MDPro3.PercyOCG Owner;public static bool Starting;public static string Error="";
  static byte[] queuedResponse;static float responseAt;static int responseView,responseHold;
  static int refreshFrame=-1;static bool suppress;public static bool ResettingTiming;
  public static bool Own(Percy.Ygopro obj)=>Session!=null&&Owner!=null&&obj.Pointer==Owner.ygopro.Pointer;
  public static void Run(Action action){try{action();Error="";}catch(Exception e){Error=e.Message;Plugin.Instance.Log.LogError(e);}}
  public static void Start(EditorSetup setup){
   if(MDPro3.Program.instance==null||MDPro3.Program.instance.menu==null)throw new InvalidOperationException("请等待开始菜单加载完成");
   if(Session!=null)throw new InvalidOperationException("请先退出当前编辑对局");
   NativeLogHistory.Reset();Session=new EditorSession(setup);NativeLogHistory.SetOpening(Session.Cards());Owner=new MDPro3.PercyOCG();Session.SaveHistory=NativeLogHistory.SaveArchive;Session.ValidateHistory=NativeLogHistory.ValidateArchive;Session.LoadHistory=NativeLogHistory.LoadArchive;Session.HistoryLoaded=NativeLogHistory.Loaded;Session.Applied=EditorChangeLog.Record;Session.Message=Deliver;Session.Restored=Refresh;Session.Branched=NativeLogHistory.DiscardAfter;
   try{Starting=true;Owner.StartPuzzle(Path.Combine(Paths.PluginPath,"DuelStructure","editor.lua"));Starting=false;
    var core=MDPro3.Program.instance.ocgcore;core.returnServant=MDPro3.Program.instance.menu;
    OcgCore.name_0=OcgCore.name_0_c=setup.Names[0];OcgCore.name_1=OcgCore.name_1_c=setup.Names[1];
    OcgCore.inPuzzle=false;OcgCore.playerType=0;OcgCore.chainCondition=OcgCore.ChainCondition.All;Refresh();Plugin.Instance.Log.LogInfo("DUEL_EDITOR_STARTED turn="+EditorNative.editor_turn_id(Session.Handle));
   }catch{Starting=false;Session.Dispose();Session=null;Owner=null;throw;}
  }
  public static void TogglePause(){if(Session==null||NativeReplay.Pending)return;var ui=MDPro3.Program.instance.ocgcore.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>();if(Session.Paused)ui.OnPlay();else ui.OnStop();}
  public static bool InDuel=>Session!=null&&MDPro3.Program.instance.ocgcore.showing;
  public static bool DuelUIReady{get{if(!InDuel)return false;var core=MDPro3.Program.instance.ocgcore;if(core.servantUI==null||!core.DuelBGManager.loaded)return false;var ui=core.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>();return ui.gameObject.activeInHierarchy&&ui.Buttons.activeInHierarchy&&ui.CG.alpha>=.99f&&ui.CG.blocksRaycasts;}}
  public static void Cleanup(){if(Session==null)return;NativeTransport.Restore();NativeDeckIntro.Reset();TurnVisuals.Reset();NativeLogHistory.Reset();var session=Session;var owner=Owner;Session=null;Owner=null;refreshFrame=-1;queuedResponse=null;suppress=false;Starting=false;Error="";EditorBehaviour.CloseSessionUI();session.Dispose();owner?.Dispose();Plugin.Instance.Log.LogInfo("DUEL_EDITOR_EXIT cleaned");}
  public static void End(){MDPro3.Program.instance.ocgcore.OnExit();}
  public static void Refresh(){if(Session==null)return;NativeLogHistory.Stash();TurnVisuals.Request();queuedResponse=null;OcgCore.NoMoreWait=true;var core=MDPro3.Program.instance.ocgcore;core.messageDispatcher.playerResponed=true;if(core.currentPopup!=null){core.currentPopup.whenQuitDo=null;UnityEngine.Object.Destroy(core.currentPopup.gameObject);core.currentPopup=null;core.returnAction=null;}if(core.DuelBGManager.loaded)core.DuelBGManager.ClearResponse();while(OcgCore.packages.Count>1)OcgCore.packages.RemoveAt(OcgCore.packages.Count-1);if(OcgCore.packages.Count==1&&OcgCore.packages[0].Function!=(int)OcgCore.currentMessage)OcgCore.packages.Clear();MDPro3.TcpHelper.datas.Clear();refreshFrame=Time.frameCount+3;}
  public static void Tick(){if(Session==null||NativeReplay.Pending)return;var core=MDPro3.Program.instance.ocgcore;
   if(!core.showing&&refreshFrame<0&&MDPro3.Program.instance.menu.showing){Cleanup();return;}
   if(!core.showing&&refreshFrame<0)return;
   if(refreshFrame>=0){if(Time.frameCount<refreshFrame||!core.showing||!core.DuelBGManager.loaded)return;refreshFrame=-1;
    MDPro3.Program.instance.menu.GetUI<MDPro3.UI.ServantUI.MainMenuUI>().gameObject.SetActive(false);core.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>().CG.alpha=1;core.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>().CG.blocksRaycasts=true;Percy.Ygopro.isFirst=OcgCore.isFirst=Session.View==0;OcgCore.myTurn=EditorNative.editor_turn_player(Session.Handle)==Session.View;OcgCore.chainCondition=(OcgCore.ChainCondition)Session.Timings[Session.View];Owner.ygopro.godMode=true;
    OcgCore.NoMoreWait=false;var ui=core.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>();ui.TextPlayer0Name.text=Session.Setup.Names[Session.View];ui.TextPlayer1Name.text=Session.Setup.Names[1-Session.View];NativeTimingVisual.Sync();core.SetFace();
    var buf=new byte[1024*1024];int count=EditorNative.query_field_info(Session.Handle,buf);NativeLogPerspective.SyncReload(buf);Owner.ReceiveHandler((Il2CppStructArray<byte>)buf.Take(count).ToArray());Owner.ygopro.Refresh();Session.PublishPrompt();Plugin.Instance.Log.LogInfo("DUEL_EDITOR_REFRESH view="+Session.View+" cards="+Session.Cards().Length);return;
   }
   NativeLogHistory.EnsureOpening();NativeAttachmentVisuals.Tick();
   var uiPause=core.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>();uiPause.ButtonStop.gameObject.SetActive(!Session.Paused);uiPause.ButtonPlay.gameObject.SetActive(Session.Paused);OcgCore.pause=false;NativeTransport.Prepare(uiPause);
   if(Session.Ended){Session.Paused=true;NativeReplay.RequestExit();return;}
   OcgCore.chainCondition=(OcgCore.ChainCondition)Session.Timings[Session.View];
   if(queuedResponse!=null&&Time.unscaledTime>=responseAt){var bytes=queuedResponse;queuedResponse=null;if(Session.Waiting&&Session.View==responseView&&Session.PromptPlayer==responseView){core.DuelBGManager.ClearResponse();core.messageDispatcher.playerResponed=true;Session.Respond(bytes);Plugin.Instance.Log.LogInfo("EDITOR_RESPONSE_ACCEPTED view="+responseView);responseHold=Time.frameCount+2;}}
   if(Time.frameCount<responseHold)return;
   if(!Session.Paused&&Session.Waiting&&Session.PromptPlayer>=0&&Session.PromptPlayer<2&&EditorPrompt.ShouldPassChain(Session.CurrentPrompt,Session.Timings[Session.PromptPlayer])){int player=Session.PromptPlayer;Session.RespondAutomatic(player,BitConverter.GetBytes(-1));Plugin.Instance.Log.LogInfo("EDITOR_TIMING_PASS player="+player+" view="+Session.View);}
   NativeTimingVisual.Sync();Session.Tick();
  }
  public static void Deliver(byte[] data){if(Session==null||data.Length==0)return;var stream=new Il2CppSystem.IO.MemoryStream((Il2CppStructArray<byte>)data);var reader=new Il2CppSystem.IO.BinaryReader(stream);try{while(stream.Position<data.Length){long before=stream.Position;if(data[(int)before]==90)NativeLogHistory.Stamp(90);Owner.ygopro.Analyse(reader);int size=(int)(stream.Position-before);if(size>0&&EditorPrompt.RequiresResponse(data[(int)before]))Session.RememberPrompt(data.Skip((int)before).Take(size).ToArray());if(stream.Position<=before)throw new InvalidDataException("MDPro3 未能读取核心消息 "+data[(int)before]);}}finally{reader.Close();stream.Close();}}
  public static bool BeforeAnalyse(Il2CppSystem.IO.BinaryReader reader){long pos=reader.BaseStream.Position;int message=reader.ReadByte();suppress=false;if(EditorPrompt.RequiresResponse(message)){if(message==23)reader.ReadByte();int player=reader.ReadByte();Session.ObservePrompt(player);suppress=player!=Session.View;if(message==16){int count=reader.ReadByte(),sp=reader.ReadByte();reader.ReadInt32();reader.ReadInt32();bool forced=false;for(int i=0;i<count;i++){reader.ReadByte();forced|=reader.ReadByte()!=0;reader.ReadBytes(12);}uint timing=Session.Timings[player];if(!forced&&(count==0||timing==0||(timing==2&&sp==0))){suppress=true;}}}reader.BaseStream.Position=pos;if(message==162){reader.ReadByte();reader.ReadByte();for(int p=0;p<2;p++){reader.ReadInt32();for(int i=0;i<7;i++)if(reader.ReadByte()!=0){reader.ReadByte();reader.ReadByte();}for(int i=0;i<8;i++)if(reader.ReadByte()!=0)reader.ReadByte();reader.ReadBytes(6);}int chains=reader.ReadByte();reader.ReadBytes(chains*15);int size=(int)(reader.BaseStream.Position-pos);reader.BaseStream.Position=pos;Owner.ReceiveHandler(reader.ReadBytes(size));return false;}return true;}
  public static void QueueResponse(byte[] bytes,float delay){Plugin.Instance.Log.LogInfo("EDITOR_RESPONSE_QUEUE view="+Session?.View+" player="+Session?.PromptPlayer+" refresh="+refreshFrame+" delay="+delay);if(Session==null||!Session.Waiting||Session.PromptPlayer!=Session.View||refreshFrame>=0)return;queuedResponse=(byte[])bytes.Clone();responseAt=Time.unscaledTime+Math.Max(0,delay);responseView=Session.View;}
  public static bool Suppress=>suppress;
  public static Il2CppStructArray<byte> Query(int player,Percy.CardLocation location,int flag){IntPtr buf=Marshal.AllocHGlobal(1024*1024);try{int n=EditorNative.query_field_card(Session.Handle,(byte)player,(byte)location,flag,buf,0);if(n<0||n>1024*1024)throw new InvalidDataException("卡片刷新长度无效");var bytes=new byte[n];Marshal.Copy(buf,bytes,0,n);return bytes;}finally{Marshal.FreeHGlobal(buf);}}
 }
 [HarmonyPatch(typeof(Percy.Ygopro),nameof(Percy.Ygopro.StartPuzzle))]class StartPatch{static bool Prefix(Percy.Ygopro __instance,ref bool __result){if(!Host.Own(__instance)||!Host.Starting)return true;__instance.godMode=true;__result=true;return false;}}
 [HarmonyPatch(typeof(Percy.Ygopro),nameof(Percy.Ygopro.QueryFieldCard))]class QueryPatch{static bool Prefix(Percy.Ygopro __instance,int player,Percy.CardLocation location,int flag,ref Il2CppStructArray<byte> __result){if(NativeReplay.Own(__instance)){__result=NativeReplay.Query(player,location,flag);return false;}if(!Host.Own(__instance))return true;__result=Host.Query(player,location,flag);return false;}}
 [HarmonyPatch(typeof(Percy.Ygopro),nameof(Percy.Ygopro.Response))]class ResponsePatch{static bool Prefix(Percy.Ygopro __instance,Il2CppStructArray<byte> resp){if(!Host.Own(__instance))return true;Host.Run(()=>Host.Session.Respond(resp.ToArray()));return false;}}
 [HarmonyPatch(typeof(Percy.Ygopro),nameof(Percy.Ygopro.Analyse))]class AnalysePatch{static bool Prefix(Percy.Ygopro __instance,Il2CppSystem.IO.BinaryReader reader,ref bool __result){if(NativeReplay.Own(__instance)){__result=false;return NativeReplay.BeforeAnalyse(reader);}if(!Host.Own(__instance))return true;__result=false;return Host.BeforeAnalyse(reader);}}
 [HarmonyPatch(typeof(Percy.Ygopro),nameof(Percy.Ygopro.Flush))]class FlushPatch{static bool Prefix(Percy.Ygopro __instance){if(!Host.Own(__instance))return true;if(Host.Suppress)return false;var bytes=__instance.currentWriter.BaseStream.Cast<Il2CppSystem.IO.MemoryStream>().ToArray();if(bytes.Length>0&&bytes[0]!=90)NativeLogHistory.Stamp(bytes[0]);return true;}}
 [HarmonyPatch(typeof(Percy.Ygopro),nameof(Percy.Ygopro.Process))]class ProcessPatch{static bool Prefix(Percy.Ygopro __instance)=>!Host.Own(__instance);}
}


namespace DuelStructure {[HarmonyPatch(typeof(MDPro3.Duel.DuelMessage),nameof(MDPro3.Duel.DuelMessage.ResetState))]class TimingResetPatch{static void Prefix(){Host.ResettingTiming=true;if(Host.Session!=null)NativeLogHistory.Stash();}static void Postfix(){Host.ResettingTiming=false;if(Host.Session!=null)NativeLogHistory.Restore(Host.Session.Number);if(Host.Session!=null)MDPro3.Servant.OcgCore.chainCondition=(MDPro3.Servant.OcgCore.ChainCondition)Host.Session.Timings[Host.Session.View];}}}

namespace DuelStructure {
 [HarmonyPatch(typeof(MDPro3.UI.ServantUI.OcgCoreUI),nameof(MDPro3.UI.ServantUI.OcgCoreUI.OnStop))]class PausePatch{static void Postfix(){if(Host.Session!=null){Host.Session.Paused=true;MDPro3.Servant.OcgCore.pause=false;}}}
 [HarmonyPatch(typeof(MDPro3.UI.ServantUI.OcgCoreUI),nameof(MDPro3.UI.ServantUI.OcgCoreUI.OnPlay))]class PlayPatch{static void Postfix(){if(Host.Session!=null){Host.Session.Paused=NativeReplay.Pending;MDPro3.Servant.OcgCore.pause=false;}}}
}

namespace DuelStructure {
 [HarmonyPatch(typeof(MDPro3.Servant.OcgCore),nameof(MDPro3.Servant.OcgCore.PerFrameFunction))]class EditorInputGuard{static bool Prefix(){return Host.Session==null||!EditorBehaviour.PanelOpen;}}
}

namespace DuelStructure {[HarmonyPatch(typeof(MDPro3.Servant.OcgCore),nameof(MDPro3.Servant.OcgCore.OnExit))]class EditorExitPatch{static bool Prefix(){if(Host.Session==null)return true;if(!NativeReplay.AllowExit){NativeReplay.RequestExit();return false;}Host.Cleanup();return true;}}}

namespace DuelStructure {
 [HarmonyPatch(typeof(MDPro3.Servant.OcgCore),nameof(MDPro3.Servant.OcgCore.SendReturn))]class EditorReturnPatch{static bool Prefix(Il2CppStructArray<byte> buffer,float delay){if(Host.Session==null)return true;Host.QueueResponse(buffer.ToArray(),delay);return false;}}
 [HarmonyPatch(typeof(MDPro3.Duel.MessageDispatcher),nameof(MDPro3.Duel.MessageDispatcher.Process))]class EditorDispatchPatch{static bool Prefix(MDPro3.Package p,ref Cysharp.Threading.Tasks.UniTask __result){if(Host.Session==null||!EditorPrompt.RequiresResponse(p.Function))return true;var payload=p.Data.Get().ToArray();var data=new byte[payload.Length+1];data[0]=(byte)p.Function;Buffer.BlockCopy(payload,0,data,1,payload.Length);int player=EditorPrompt.Player(data);if(player==Host.Session.View&&Host.Session.Waiting&&Host.Session.PromptPlayer==player&&Host.Session.CurrentPrompt.Length>0&&Host.Session.CurrentPrompt[0]==p.Function)return true;__result=Cysharp.Threading.Tasks.UniTask.CompletedTask;return false;}}
}

namespace DuelStructure {[HarmonyPatch(typeof(MDPro3.UI.ServantUI.OcgCoreUI),nameof(MDPro3.UI.ServantUI.OcgCoreUI.OnTiming))]class EditorTimingChoicePatch{static void Postfix(){if(Host.Session==null||Host.ResettingTiming||!Host.InDuel)return;uint timing=(uint)MDPro3.Servant.OcgCore.chainCondition;if(timing<3){Host.Session.Timings[Host.Session.View]=timing;NativeTimingVisual.Sync();Plugin.Instance.Log.LogInfo("EDITOR_TIMING_CHOICE player="+Host.Session.View+" mode="+timing);}}}}
