using System;using System.IO;using System.Linq;using System.Runtime.InteropServices;using HarmonyLib;using MDPro3;using MDPro3.Servant;using MDPro3.DuelEditor;using Il2CppInterop.Runtime.InteropTypes.Arrays;
namespace DuelStructure {
 static class NativeReplay {
  static PercyOCG converter;static BinaryWriter writer;static IntPtr handle;static bool requested,namesWritten,popupQueued;static float popupAt;
  public static bool AllowExit{get;private set;}public static bool Pending=>requested;
  public static bool Own(Percy.Ygopro obj)=>converter!=null&&obj.Pointer==converter.ygopro.Pointer;
  public static bool Own(PercyOCG obj)=>converter!=null&&obj.Pointer==converter.Pointer;
  public static void Capture(byte[] packet){if(packet==null||packet.Length==0)return;writer.Write(packet[0]);writer.Write((uint)(packet.Length-1));writer.Write(packet,1,packet.Length-1);}
  public static Il2CppStructArray<byte> Query(int player,Percy.CardLocation location,int flags){IntPtr buf=Marshal.AllocHGlobal(1024*1024);try{int n=EditorNative.query_field_card(handle,(byte)player,(byte)location,EditorLinkRules.QueryFlags(flags),buf,0);if(n<0||n>1024*1024)throw new InvalidDataException("录像卡片刷新长度无效");var bytes=new byte[n];Marshal.Copy(buf,bytes,0,n);return bytes;}finally{Marshal.FreeHGlobal(buf);}}
  public static bool BeforeAnalyse(Il2CppSystem.IO.BinaryReader reader){long pos=reader.BaseStream.Position;int message=reader.ReadByte();reader.BaseStream.Position=pos;if(message!=162)return true;
   reader.ReadByte();reader.ReadByte();for(int p=0;p<2;p++){reader.ReadInt32();for(int i=0;i<7;i++)if(reader.ReadByte()!=0){reader.ReadByte();reader.ReadByte();}for(int i=0;i<8;i++)if(reader.ReadByte()!=0)reader.ReadByte();reader.ReadBytes(6);}int chains=reader.ReadByte();reader.ReadBytes(chains*15);int n=(int)(reader.BaseStream.Position-pos);reader.BaseStream.Position=pos;Capture(reader.ReadBytes(n).ToArray());return false;
  }
  static void Emit(IntPtr current,byte[] packet){handle=current;var stream=new Il2CppSystem.IO.MemoryStream((Il2CppStructArray<byte>)packet);var reader=new Il2CppSystem.IO.BinaryReader(stream);try{while(stream.Position<packet.Length){long before=stream.Position;converter.ygopro.Analyse(reader);if(stream.Position<=before)throw new InvalidDataException("录像消息无法转换");}}finally{reader.Close();stream.Close();}}
  static void Refresh(IntPtr current){handle=current;var bytes=new byte[1024*1024];int n=EditorNative.query_field_info(current,bytes);Emit(current,bytes.Take(n).ToArray());converter.ygopro.Refresh();var cards=new EditorCard[EditorNative.editor_list_cards(current,null,0)];EditorNative.editor_list_cards(current,cards,(uint)cards.Length);var counters=new EditorCounter[EditorNative.editor_list_counters(current,null,0)];EditorNative.editor_list_counters(current,counters,(uint)counters.Length);foreach(var counter in counters){var card=cards.First(c=>c.Id==counter.CardId);using(var data=new MemoryStream()){using(var packet=new BinaryWriter(data)){packet.Write((byte)101);packet.Write((ushort)counter.Type);packet.Write((byte)card.Player);packet.Write((byte)card.Location);packet.Write((byte)card.Sequence);packet.Write((ushort)counter.Count);Capture(data.ToArray());}}}if(!namesWritten){namesWritten=true;string a=OcgCore.name_0,b=OcgCore.name_1,ac=OcgCore.name_0_c,bc=OcgCore.name_1_c;try{OcgCore.name_0=OcgCore.name_0_c=Host.Session.Setup.Names[0];OcgCore.name_1=OcgCore.name_1_c=Host.Session.Setup.Names[1];var names=Program.instance.ocgcore.GetNamePacket();writer.Write((byte)names.Function);var data=names.Data.Get().ToArray();writer.Write((uint)data.Length);writer.Write(data);}finally{OcgCore.name_0=a;OcgCore.name_1=b;OcgCore.name_0_c=ac;OcgCore.name_1_c=bc;}}}
  public static string Save(string name){
   if(Host.Session==null)throw new InvalidOperationException("没有可保存的编辑对局");name=(name??"").Trim();if(name.Length==0||name.Length>100||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0||name=="."||name=="..")throw new InvalidDataException("请输入有效的录像名称");
   string root=Path.Combine(BepInEx.Paths.GameRootPath,"Replay");Directory.CreateDirectory(root);string path=Path.Combine(root,name+Program.EXPANSION_YRP3D);if(File.Exists(path))throw new IOException("该录像名称已存在，请使用其他名称");string temp=path+".writing";
   bool first=Percy.Ygopro.isFirst;try{namesWritten=false;converter=new PercyOCG();converter.ygopro.godMode=true;Percy.Ygopro.isFirst=true;using(writer=new BinaryWriter(File.Create(temp))){
    Host.Session.ExportMessages(Emit,Refresh);
   }writer=null;File.Move(temp,path);Plugin.Instance.Log.LogInfo("EDITOR_REPLAY_SAVED "+path);return path;
   }finally{writer=null;handle=IntPtr.Zero;if(converter!=null){converter.Dispose();converter=null;}Percy.Ygopro.isFirst=first;if(File.Exists(temp))File.Delete(temp);}
  }
  public static void RequestExit(){
   if(Host.Session==null||requested)return;requested=true;Host.Session.Paused=true;EditorBehaviour.CloseSessionUI();
   var p=Program.instance;var core=p.ocgcore;
   if(p.currentSubServant!=null){if(p.currentSubServant.Pointer==p.setting.Pointer)p.setting.OnExit();else{p.currentSubServant.Hide(-1);p.currentSubServant=null;}}
   if(p.setting.showing)p.setting.OnExit();
   // Surrender hides settings through ShutDown(), bypassing its normal HideEvent cleanup.
   MDPro3.UIManager.HideBlackBack(.2f);MDPro3.UIManager.HideExitButton(.2f);
   if(core.currentPopup!=null){core.currentPopup.whenQuitDo=null;core.currentPopup.Hide();}core.returnAction=null;
   popupQueued=true;popupAt=UnityEngine.Time.unscaledTime+.4f;
  }
  public static void Tick(){if(Host.Session==null){requested=popupQueued=false;return;}if(!requested||!popupQueued||UnityEngine.Time.unscaledTime<popupAt)return;popupQueued=false;Program.instance.ocgcore.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>().OnForcedSaveReplay();}
  public static void Finish(){requested=popupQueued=false;AllowExit=true;try{Program.instance.ocgcore.returnAction=null;Program.instance.ocgcore.OnExit();}finally{AllowExit=false;}}
  public static bool SaveAndExit(string name){if(Host.Session==null)return true;try{Save(name);Finish();}catch(Exception e){Host.Error=e.Message;Plugin.Instance.Log.LogError(e);requested=false;RequestExit();}return false;}
 }
 [HarmonyPatch(typeof(PercyOCG),"ReceiveHandler")]class CaptureEditorReplayPacket{static bool Prefix(PercyOCG __instance,Il2CppStructArray<byte> buffer){if(!NativeReplay.Own(__instance))return true;NativeReplay.Capture(buffer.ToArray());return false;}}
 [HarmonyPatch(typeof(MDPro3.UI.ServantUI.OcgCoreUI),"OnSaveReplay")]class SaveEditorReplay{static bool Prefix(string replayName)=>NativeReplay.SaveAndExit(replayName);}
 [HarmonyPatch(typeof(MDPro3.UI.ServantUI.OcgCoreUI),"OnGiveUpReplay")]class DiscardEditorReplay{static bool Prefix(){if(Host.Session==null)return true;NativeReplay.Finish();return false;}}
 [HarmonyPatch(typeof(MDPro3.UI.ServantUI.OcgCoreUI),nameof(MDPro3.UI.ServantUI.OcgCoreUI.ShowSaveReplay))]class PromptEditorReplay{static bool Prefix(){if(Host.Session==null)return true;NativeReplay.RequestExit();return false;}}
}
