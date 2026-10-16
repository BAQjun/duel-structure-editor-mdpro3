using System;
using System.IO;
using System.Runtime.InteropServices;
namespace MDPro3.DuelEditor {
 public static class EditorLinkRules {
  public const uint MarkerMask=0x1ef;
  public static int QueryFlags(int flags)=>flags|0x800000;
  public static uint Markers(uint type,uint marker)=>(type&0x4000000)!=0?marker&MarkerMask:0;
  public static uint Rating(uint marker){uint count=0;for(uint bits=marker&MarkerMask;bits!=0;bits>>=1)count+=bits&1;return count;}
 }
 [StructLayout(LayoutKind.Sequential)] public struct EditCommand {
  public uint Version,Opcode,Revision,Code,Owner,Player,Location,Sequence,Position,Value,TargetLow,TargetHigh,Form,Race,Attribute,Attack,Defense,Level,MonsterFlags,NameCode,States,Restrictions;
  public static EditCommand Create(uint opcode) => new EditCommand { Version=9,Opcode=opcode,Position=1,Level=4,MonsterFlags=1 };
  public ulong Target {get=>((ulong)TargetHigh<<32)|TargetLow;set{TargetLow=(uint)value;TargetHigh=(uint)(value>>32);}}
 }
 [StructLayout(LayoutKind.Sequential)] public struct EditorCard {
  public ulong Id; public uint Code,Type,Owner,Player,Location,Sequence,Position,Flags,PendulumZone,Form,Race,Attribute,Attack,Defense,Level,MonsterFlags,NameCode,States,Restrictions,ActualStatus,EffectiveType,EffectiveCode,SummonSource,SummonMethod,LinkMarker;
 }
 [StructLayout(LayoutKind.Sequential)] public struct NativeCardData {
  public uint Code,Alias;[MarshalAs(UnmanagedType.ByValArray,SizeConst=16)]public ushort[] SetCode;
  public uint Type,Level,Attribute,Race;public int Attack,Defense;public uint LScale,RScale,LinkMarker,RuleCode;
 }
 [StructLayout(LayoutKind.Sequential)] public struct EditorSummonMaterial {
  public ulong HostId,CardId;
  public uint Code,Owner,SourcePlayer,SourceLocation,SourceSequence,SourcePosition,DestinationPlayer,DestinationLocation,DestinationSequence,DestinationPosition;
 }
 public static class EditorSummonHistoryRules {
  public static uint Kind(uint type,uint method,uint source,uint location){if(method!=1||location!=4)return 0;if((type&0x80)!=0)return 5;if(source!=64)return 0;return (type&0x40)!=0?1u:(type&0x2000)!=0?2u:(type&0x800000)!=0?3u:(type&0x4000000)!=0?4u:0;}
  public static string Name(uint kind)=>kind==1?"融合":kind==2?"同调":kind==3?"超量":kind==4?"连接":kind==5?"仪式":"特殊";
  public static uint[] Zones(uint type){var zones=new System.Collections.Generic.List<uint>();if((type&0x4802040)==0){zones.Add(1);zones.Add(2);}zones.Add(4);zones.Add(8);zones.Add(16);zones.Add(32);if((type&0x5802040)!=0)zones.Add(64);return zones.ToArray();}
 }
 [StructLayout(LayoutKind.Sequential)] public struct EditorMaterial {public ulong HostId,CardId;public uint Code,Owner;}
 [StructLayout(LayoutKind.Sequential)] public struct EditorCounter {public ulong CardId;public uint Type,Count;}
 [StructLayout(LayoutKind.Sequential)] public struct EditorEffect {public ulong CardId;public uint Code;public int Value;}
 public static class EditorNative {
  const string Library="ocgcore_editor";
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_summon_kind(IntPtr duel,ulong id);
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_list_summon_materials(IntPtr duel,ulong id,[Out]EditorSummonMaterial[] output,uint count);

 [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int editor_native_trap_profile(uint code,out EditorCard output);
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_list_materials(IntPtr duel,[Out]EditorMaterial[] output,uint count);
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_list_counters(IntPtr duel,[Out]EditorCounter[] output,uint count);
 [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_list_custom_effects(IntPtr duel,[Out]EditorEffect[] output,uint count);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]public delegate uint CardReader(uint code,IntPtr output);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]public delegate IntPtr ScriptReader([MarshalAs(UnmanagedType.LPStr)]string name,out int length);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]public delegate uint MessageReader(IntPtr duel,uint type);
  static CardReader cardReader;static ScriptReader scriptReader;static MessageReader messageReader;
  static IntPtr scriptBuffer=IntPtr.Zero;static int scriptCapacity;
  public static void Register(Func<uint,NativeCardData> card,Func<string,byte[]> script,Action<string> error){
   cardReader=(code,output)=>{var data=card(code);data.SetCode=data.SetCode??new ushort[16];Marshal.StructureToPtr(data,output,false);return code;};
   scriptReader=(string name,out int length)=>{var bytes=script(name);length=bytes?.Length??0;if(length==0)return IntPtr.Zero;if(length>scriptCapacity){if(scriptBuffer!=IntPtr.Zero)Marshal.FreeHGlobal(scriptBuffer);scriptCapacity=Math.Max(length,262144);scriptBuffer=Marshal.AllocHGlobal(scriptCapacity);}Marshal.Copy(bytes,0,scriptBuffer,length);return scriptBuffer;};
   messageReader=(duel,type)=>{var bytes=new byte[4096];get_log_message(duel,bytes);error(System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0'));return 0;};
   set_card_reader(cardReader);set_script_reader(scriptReader);set_message_handler(messageReader);
   if(editor_command_size()!=Marshal.SizeOf<EditCommand>()||editor_card_info_size()!=Marshal.SizeOf<EditorCard>())throw new InvalidOperationException("编辑核心接口版本不匹配");
  }
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]static extern void set_card_reader(CardReader callback);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]static extern void set_script_reader(ScriptReader callback);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]static extern void set_message_handler(MessageReader callback);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]static extern void get_log_message(IntPtr duel,[Out]byte[] bytes);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern IntPtr create_duel_v2([In]uint[] seeds);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern void end_duel(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern void start_duel(IntPtr duel,uint options);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint process(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int get_message(IntPtr duel,[Out]byte[] bytes);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int query_field_info(IntPtr duel,[Out]byte[] bytes);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int query_field_card(IntPtr duel,byte player,byte location,int flags,IntPtr output,int cache);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern void set_responseb(IntPtr duel,[In]byte[] bytes);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern void editor_enable(IntPtr duel,int enable);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int editor_can_edit(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_revision(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_list_cards(IntPtr duel,[Out]EditorCard[] output,uint count);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int editor_apply(IntPtr duel,ref EditCommand command);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int editor_apply_group(IntPtr duel,[In,Out]EditCommand[] commands,uint count);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int editor_setup_decks(IntPtr duel,[In]uint[] m0,uint mc0,[In]uint[] e0,uint ec0,[In]uint[] m1,uint mc1,[In]uint[] e1,uint ec1,uint h0,uint h1);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern ulong editor_state_hash(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern ulong editor_state_hash_legacy(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern int editor_prepare_initial_state(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_turn_id(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_turn_player(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_phase(IntPtr duel);
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_command_size();
  [DllImport(Library,CallingConvention=CallingConvention.Cdecl)]public static extern uint editor_card_info_size();
 }
}
