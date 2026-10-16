using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace MDPro3.DuelEditor {
 public static class EditorSummonRules {
  public static uint[] SpecialSources(uint type,uint form){bool extra=(type&0x4802040)!=0;var sources=new List<uint>();if(!extra)sources.AddRange(new uint[]{2,1});sources.AddRange(new uint[]{16,32,8});if((type&0x5802040)!=0&&form!=4&&form!=6){if(extra)sources.Insert(0,64);else sources.Add(64);}return sources.ToArray();}
 }
 public sealed class EditorSetup {
  public uint[][] Decks={Array.Empty<uint>(),Array.Empty<uint>(),Array.Empty<uint>(),Array.Empty<uint>()};
  public bool OpeningProcess=true;public uint[] Hands={0,0};public string[] Names={"玩家 1","玩家 2"};public uint[] Seeds=new uint[8];
  public static EditorSetup FromDecks(string first,string second,uint hand0,uint hand1,Func<uint,uint> type){
   if(string.IsNullOrWhiteSpace(first)||!File.Exists(first))throw new InvalidDataException("请选择玩家 1 的有效卡组；可点击刷新卡组重新载入。");if(string.IsNullOrWhiteSpace(second)||!File.Exists(second))throw new InvalidDataException("请选择玩家 2 的有效卡组；可点击刷新卡组重新载入。");
   var result=new EditorSetup();result.Hands=new[]{hand0,hand1};var random=new Random();for(int i=0;i<8;i++)result.Seeds[i]=(uint)random.Next();
   for(int player=0;player<2;player++){
    var main=new List<uint>();var extra=new List<uint>();bool side=false;
    foreach(var raw in File.ReadLines(player==0?first:second)){
     string line=raw.Trim();if(line.StartsWith("!")){side=true;continue;}if(side||line.Length==0||line.StartsWith("#"))continue;
     if(!uint.TryParse(line,out var code)||code==0)throw new InvalidDataException("无法读取卡号："+line);
     uint flags=type(code);if(flags==0||(flags&0x4000)!=0)throw new InvalidDataException("未知卡片或衍生物："+code);
     ((flags&0x4802040)!=0?extra:main).Add(code);
    }
    if(main.Count>254||extra.Count>254||result.Hands[player]>254)throw new InvalidDataException("每个卡组区域与起手数量最多 254");
    result.Decks[player*2]=main.ToArray();result.Decks[player*2+1]=extra.ToArray();
   }return result;
  }
 }
 public sealed class EditorSession:IDisposable {
  sealed class Entry {public byte Kind;public uint Result;public byte[] Bytes=Array.Empty<byte>();}
  sealed class Point {public int Index;public ulong Hash;public bool Waiting;public int PromptPlayer;public byte[] Prompt=Array.Empty<byte>();}
  public IntPtr Handle{get;private set;}public EditorSetup Setup{get;private set;}
  public bool Paused=true;public bool Waiting{get;private set;}public bool Ended{get;private set;}
  public int View{get;private set;}public int PromptPlayer{get;private set;}=-1;public uint[] Timings={1,1};
  public byte[] CurrentPrompt=>(byte[])prompt.Clone();
  public int Number=>cursor+1;public bool CanBack=>cursor>0;public bool CanForward=>cursor+1<points.Count;
  public Func<byte[]> SaveHistory;public Action<byte[],int> ValidateHistory;public Action<byte[]> LoadHistory;public Action<int> HistoryLoaded;public Action<EditCommand,EditorCard[],EditorCard[]> Applied;public Action<byte[]> Message;public Action Restored;public Action<int> Branched;public string LastError="";
  readonly List<Entry> journal=new List<Entry>();readonly List<Point> points=new List<Point>();int cursor;
  byte[] prompt=Array.Empty<byte>();readonly byte[] buffer=new byte[1024*1024];
  public bool CanEdit=>Handle!=IntPtr.Zero&&EditorNative.editor_can_edit(Handle)!=0;
  public EditorSession(EditorSetup setup){Setup=setup;Handle=Create(setup);Checkpoint();}
  static IntPtr Create(EditorSetup setup){
   IntPtr handle=EditorNative.create_duel_v2(setup.Seeds);
   try {EditorNative.editor_enable(handle,setup.OpeningProcess?3:2);
    if(EditorNative.editor_setup_decks(handle,setup.Decks[0],(uint)setup.Decks[0].Length,setup.Decks[1],(uint)setup.Decks[1].Length,setup.Decks[2],(uint)setup.Decks[2].Length,setup.Decks[3],(uint)setup.Decks[3].Length,setup.Hands[0],setup.Hands[1])==0)throw new InvalidDataException("卡组或起手配置无法载入");
    EditorNative.get_message(handle,new byte[1024*1024]);EditorNative.start_duel(handle,0);if(EditorNative.editor_prepare_initial_state(handle)==0)throw new InvalidOperationException("无法准备开局暂停状态");return handle;
   }catch{EditorNative.end_duel(handle);throw;}
  }
  void Branch(){if(CanForward){Branched?.Invoke(Number);journal.RemoveRange(points[cursor].Index,journal.Count-points[cursor].Index);points.RemoveRange(cursor+1,points.Count-cursor-1);}}
  void Checkpoint(){points.Add(new Point{Index=journal.Count,Hash=EditorNative.editor_state_hash(Handle),Waiting=Waiting,PromptPlayer=PromptPlayer,Prompt=(byte[])prompt.Clone()});cursor=points.Count-1;}
  public EditorCard[] Cards(){uint count=EditorNative.editor_list_cards(Handle,null,0);if(count>65536)throw new InvalidDataException("局面卡片超过容量");var cards=new EditorCard[count];EditorNative.editor_list_cards(Handle,cards,count);return cards;}
  public void ObservePrompt(int player){if(player<0||player>1)throw new InvalidDataException("无效的操作玩家");PromptPlayer=player;}
  public void RememberPrompt(byte[] data){int player=EditorPrompt.Player(data);if(player<0)return;PromptPlayer=player;if(Waiting)prompt=(byte[])data.Clone();}

  public void RespondAutomatic(int player,byte[] response){if(!Waiting||PromptPlayer!=player||!EditorPrompt.ShouldPassChain(prompt,Timings[player]))throw new InvalidOperationException("自动时点响应不适用");RespondCore(response);}
  public void Respond(byte[] response){
   if(!Waiting)throw new InvalidOperationException("当前没有待处理决定");
   if(PromptPlayer>=0&&PromptPlayer!=View)throw new InvalidOperationException("请切换到玩家 "+(PromptPlayer+1)+" 处理");
   RespondCore(response);
  }
  void RespondCore(byte[] response){
   if(response==null||response.Length==0||response.Length>64)throw new InvalidDataException("无效响应");
   Branch();var bytes=new byte[64];Buffer.BlockCopy(response,0,bytes,0,response.Length);
   EditorNative.set_responseb(Handle,bytes);journal.Add(new Entry{Kind=1,Bytes=bytes});Waiting=false;prompt=Array.Empty<byte>();Checkpoint();
  }
  public void Step(){if(CanForward){Restore(cursor+1);Paused=true;return;}if(Waiting&&PromptPlayer>=0&&EditorPrompt.ShouldPassChain(prompt,Timings[PromptPlayer])){RespondAutomatic(PromptPlayer,BitConverter.GetBytes(-1));Paused=true;return;}Advance();Paused=true;}
  public void Tick(){if(!Paused&&!Waiting&&!Ended)Advance();}
  void Advance(){
   if(Waiting||Ended)return;Branch();
   for(int n=0;n<100000;n++){
    uint result=EditorNative.process(Handle);int length=EditorNative.get_message(Handle,buffer);if(length<0||length>buffer.Length)throw new InvalidDataException("核心消息长度无效");
    journal.Add(new Entry{Kind=0,Result=result});Waiting=(result&0xf0000000)==0x10000000&&length>0;Ended=(result&0xf0000000)==0x20000000;
    byte[] data=buffer.Take(length).ToArray();prompt=Waiting?data:Array.Empty<byte>();
    if(length>0)Message?.Invoke(data);
    if(Waiting&&Message==null)RememberPrompt(data);
    if(length>0||Waiting||Ended){Checkpoint();return;}
   }throw new InvalidOperationException("核心未到达下一个时点");
  }
  public int Apply(EditCommand command){
   if(!CanEdit)return 2;command.Version=9;command.Revision=EditorNative.editor_revision(Handle);
   var before=Applied==null?null:Cards();bool wasWaiting=Waiting;int handBefore=command.Opcode==8?Cards().Count(c=>c.Player==command.Player&&c.Location==2):0;int result=EditorNative.editor_apply(Handle,ref command);if(result!=0)return result;
   Branch();journal.Add(new Entry{Kind=2,Bytes=ToBytes(command)});Waiting=false;prompt=Array.Empty<byte>();
   int drained=EditorNative.get_message(Handle,buffer);journal.Add(new Entry{Kind=3,Result=(uint)drained});
   if(drained>0)Message?.Invoke(buffer.Take(drained).ToArray());
   Checkpoint();Paused=true;Applied?.Invoke(command,before,Cards());
   if(wasWaiting)for(int i=0;i<10000&&!Waiting&&!Ended;i++)Advance();else if(command.Opcode==8)for(int i=0;i<10000&&!Waiting&&!Ended&&Cards().Count(c=>c.Player==command.Player&&c.Location==2)==handBefore;i++)Advance();
   Restored?.Invoke();return 0;
  }
  public EditorMaterial[] Materials(){uint n=EditorNative.editor_list_materials(Handle,null,0);var result=new EditorMaterial[n];EditorNative.editor_list_materials(Handle,result,n);return result;}
  public EditorCounter[] Counters(){uint n=EditorNative.editor_list_counters(Handle,null,0);var result=new EditorCounter[n];EditorNative.editor_list_counters(Handle,result,n);return result;}
  // Validate the complete edit on a rebuilt engine before committing one undo point.
  public int ApplyGroup(IEnumerable<EditCommand> commands){
   if(!CanEdit)return 2;var list=commands.ToArray();if(list.Length==0)return 0;if(list.Length>512)throw new InvalidDataException("一次最多提交 512 项配置");for(int i=0;i<list.Length;i++)list[i].Version=9;
   IntPtr candidate=ReplayTo(Setup,journal,points[cursor]);bool wasWaiting=Waiting;var before=Cards();
   try{int result=EditorNative.editor_apply_group(candidate,list,(uint)list.Length);if(result!=0)return result;int length=EditorNative.get_message(candidate,buffer);
    byte[] data;using(var stream=new MemoryStream()){foreach(var cmd in list){var bytes=ToBytes(cmd);stream.Write(bytes,0,bytes.Length);}data=stream.ToArray();}
    Branch();EditorNative.end_duel(Handle);Handle=candidate;candidate=IntPtr.Zero;journal.Add(new Entry{Kind=4,Bytes=data});journal.Add(new Entry{Kind=3,Result=(uint)length});Waiting=false;prompt=Array.Empty<byte>();Checkpoint();Paused=true;var after=Cards();foreach(var cmd in list)Applied?.Invoke(cmd,before,after);
    if(wasWaiting)for(int i=0;i<10000&&!Waiting&&!Ended;i++)Advance();Restored?.Invoke();return 0;
   }finally{if(candidate!=IntPtr.Zero)EditorNative.end_duel(candidate);}
  }
  public void Back(){if(CanBack)Restore(cursor-1);}
  public void Forward(){if(CanForward)Restore(cursor+1);}
  public void SwitchView(){View=1-View;Restored?.Invoke();}
  public void PublishPrompt(){if(Waiting&&PromptPlayer==View&&prompt.Length>0)Message?.Invoke((byte[])prompt.Clone());}
  public void Restore(int target){
   if(target<0||target>=points.Count)throw new ArgumentOutOfRangeException(nameof(target));
   IntPtr replacement=ReplayTo(Setup,journal,points[target]);EditorNative.end_duel(Handle);Handle=replacement;cursor=target;
   Waiting=points[target].Waiting;PromptPlayer=points[target].PromptPlayer;prompt=(byte[])points[target].Prompt.Clone();Ended=false;Paused=true;Restored?.Invoke();
  }
  static IntPtr ReplayTo(EditorSetup setup,List<Entry> entries,Point point){
   IntPtr replacement=Create(setup);var scratch=new byte[1024*1024];
   try{for(int i=0;i<point.Index;i++){
    var entry=entries[i];switch(entry.Kind){
     case 0:if(EditorNative.process(replacement)!=entry.Result)throw new InvalidDataException("时点重建处理结果不一致");EditorNative.get_message(replacement,scratch);break;
     case 1:EditorNative.set_responseb(replacement,entry.Bytes);break;
     case 4:var group=Enumerable.Range(0,entry.Bytes.Length/88).Select(n=>FromBytes(entry.Bytes.Skip(n*88).Take(88).ToArray())).ToArray();if(EditorNative.editor_apply_group(replacement,group,(uint)group.Length)!=0)throw new InvalidDataException("时点重建素材／指示物失败");break;
     case 2:var command=FromBytes(entry.Bytes);if(EditorNative.editor_apply(replacement,ref command)!=0)throw new InvalidDataException("时点重建编辑失败");break;
     case 3:if(EditorNative.get_message(replacement,scratch)!=entry.Result)throw new InvalidDataException("时点重建刷新结果不一致");break;
     default:throw new InvalidDataException("未知时点操作");
    }}if(EditorNative.editor_state_hash(replacement)!=point.Hash&&EditorNative.editor_state_hash_legacy(replacement)!=point.Hash)throw new InvalidDataException("时点重建场面、队列或随机状态不一致");return replacement;
   }catch{EditorNative.end_duel(replacement);throw;}
  }
  // Reconstruct only the selected timeline branch on an isolated core for native replay export.
  public void ExportMessages(Action<IntPtr,byte[]> emit,Action<IntPtr> refresh){
   IntPtr replay=Create(Setup);var packet=new byte[1024*1024];
   try{refresh(replay);for(int i=0;i<points[cursor].Index;i++){
    var e=journal[i];switch(e.Kind){
     case 0:if(EditorNative.process(replay)!=e.Result)throw new InvalidDataException("录像重建处理结果不一致");int n=EditorNative.get_message(replay,packet);if(n>0)emit(replay,packet.Take(n).ToArray());break;
     case 1:EditorNative.set_responseb(replay,e.Bytes);break;
     case 2:var command=FromBytes(e.Bytes);if(EditorNative.editor_apply(replay,ref command)!=0)throw new InvalidDataException("录像重建编辑失败");break;
     case 4:var group=Enumerable.Range(0,e.Bytes.Length/88).Select(k=>FromBytes(e.Bytes.Skip(k*88).Take(88).ToArray())).ToArray();if(EditorNative.editor_apply_group(replay,group,(uint)group.Length)!=0)throw new InvalidDataException("录像重建素材／指示物失败");break;
     case 3:if(EditorNative.get_message(replay,packet)!=e.Result)throw new InvalidDataException("录像重建刷新结果不一致");refresh(replay);break;
     default:throw new InvalidDataException("未知录像操作");
    }
   }if(EditorNative.editor_state_hash(replay)!=points[cursor].Hash&&EditorNative.editor_state_hash_legacy(replay)!=points[cursor].Hash)throw new InvalidDataException("录像重建最终状态不一致");
   }finally{EditorNative.end_duel(replay);}
  }
  public void Save(string file,string label){
   if(System.Text.Encoding.UTF8.GetByteCount(label)>256)throw new InvalidDataException("存档名称过长");
   byte[] history=SaveHistory?.Invoke();string temporary=file+".writing";try{
    using(var writer=new BinaryWriter(File.Create(temporary))){writer.Write(0x4544504d);writer.Write(history==null?2:3);writer.Write(Setup.OpeningProcess);writer.Write(label);foreach(uint seed in Setup.Seeds)writer.Write(seed);foreach(uint hand in Setup.Hands)writer.Write(hand);foreach(string name in Setup.Names)writer.Write(name);
     foreach(var deck in Setup.Decks){writer.Write(deck.Length);foreach(uint code in deck)writer.Write(code);}
     writer.Write(View);foreach(uint timing in Timings)writer.Write(timing);
     int limit=points[cursor].Index;writer.Write(limit);for(int i=0;i<limit;i++){var e=journal[i];writer.Write(e.Kind);writer.Write(e.Result);writer.Write(e.Bytes.Length);writer.Write(e.Bytes);}
     writer.Write(cursor+1);for(int i=0;i<=cursor;i++){var p=points[i];writer.Write(p.Index);writer.Write(p.Hash);writer.Write(p.Waiting);writer.Write(p.PromptPlayer);writer.Write(p.Prompt.Length);writer.Write(p.Prompt);}if(history!=null){writer.Write(history.Length);writer.Write(history);}}
    if(File.Exists(file))throw new IOException("存档已存在，请使用不同名称");File.Move(temporary,file);
   }finally{if(File.Exists(temporary))File.Delete(temporary);}
  }
  static byte[] ReadBytes(BinaryReader reader,int cap){int length=reader.ReadInt32();if(length<0||length>cap)throw new InvalidDataException("无效数据长度");var bytes=reader.ReadBytes(length);if(bytes.Length!=length)throw new EndOfStreamException();return bytes;}
  public void Load(string file){
   if(new FileInfo(file).Length>64*1024*1024)throw new InvalidDataException("存档过大");var setup=new EditorSetup();var entries=new List<Entry>();var loadedPoints=new List<Point>();int view;byte[] history=null;uint[] timings=new uint[2];
   using(var reader=new BinaryReader(File.OpenRead(file))){if(reader.ReadInt32()!=0x4544504d)throw new InvalidDataException("不是 MDPro3 决构存档");int version=reader.ReadInt32();if(version!=1&&version!=2&&version!=3)throw new InvalidDataException("不支持的决构存档版本");setup.OpeningProcess=version>=2&&reader.ReadBoolean();if(reader.ReadString().Length>256)throw new InvalidDataException("名称无效");
    for(int i=0;i<8;i++)setup.Seeds[i]=reader.ReadUInt32();for(int i=0;i<2;i++){setup.Hands[i]=reader.ReadUInt32();if(setup.Hands[i]>254)throw new InvalidDataException("起手无效");}for(int i=0;i<2;i++){setup.Names[i]=reader.ReadString();if(setup.Names[i].Length>100)throw new InvalidDataException("玩家名称无效");}
    for(int i=0;i<4;i++){int count=reader.ReadInt32();if(count<0||count>254)throw new InvalidDataException("牌组数量无效");setup.Decks[i]=new uint[count];for(int j=0;j<count;j++)setup.Decks[i][j]=reader.ReadUInt32();}
    view=reader.ReadInt32();if(view<0||view>1)throw new InvalidDataException("视角无效");for(int i=0;i<2;i++){timings[i]=reader.ReadUInt32();if(timings[i]>2)throw new InvalidDataException("时点设置无效");}
    int records=reader.ReadInt32();if(records<0||records>1000000)throw new InvalidDataException("操作数量无效");for(int i=0;i<records;i++){var e=new Entry{Kind=reader.ReadByte(),Result=reader.ReadUInt32(),Bytes=ReadBytes(reader,512*88)};if(e.Kind>4||(e.Kind!=4&&e.Bytes.Length>88)||(e.Kind==4&&(e.Bytes.Length==0||e.Bytes.Length%88!=0))||(e.Kind==1&&e.Bytes.Length!=64)||(e.Kind==2&&e.Bytes.Length!=88))throw new InvalidDataException("操作无效");entries.Add(e);}
    int countPoints=reader.ReadInt32();if(countPoints<1||countPoints>100000)throw new InvalidDataException("时点数量无效");int previous=0;
    for(int i=0;i<countPoints;i++){var p=new Point{Index=reader.ReadInt32(),Hash=reader.ReadUInt64(),Waiting=reader.ReadBoolean(),PromptPlayer=reader.ReadInt32(),Prompt=ReadBytes(reader,1024*1024)};if(p.Index<previous||p.Index>records||p.PromptPlayer < -1||p.PromptPlayer>1)throw new InvalidDataException("时点无效");previous=p.Index;loadedPoints.Add(p);}
    if(version>=3){history=ReadBytes(reader,32*1024*1024);ValidateHistory?.Invoke(history,loadedPoints.Count);}
    if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new InvalidDataException("存档尾部数据无效");}
   var final=loadedPoints.Last();IntPtr replacement=ReplayTo(setup,entries,final);int shared=0;while(shared<points.Count&&shared<loadedPoints.Count&&points[shared].Index==loadedPoints[shared].Index&&points[shared].Hash==loadedPoints[shared].Hash)shared++;EditorNative.end_duel(Handle);Handle=replacement;Setup=setup;journal.Clear();journal.AddRange(entries);points.Clear();points.AddRange(loadedPoints);cursor=points.Count-1;View=view;Timings=timings;Waiting=final.Waiting;PromptPlayer=final.PromptPlayer;prompt=final.Prompt;Paused=true;Ended=false;HistoryLoaded?.Invoke(shared);if(history!=null)LoadHistory?.Invoke(history);Restored?.Invoke();
  }
  static byte[] ToBytes(EditCommand command){int size=Marshal.SizeOf<EditCommand>();IntPtr ptr=Marshal.AllocHGlobal(size);try{Marshal.StructureToPtr(command,ptr,false);var bytes=new byte[size];Marshal.Copy(ptr,bytes,0,size);return bytes;}finally{Marshal.FreeHGlobal(ptr);}}
  static EditCommand FromBytes(byte[] bytes){IntPtr ptr=Marshal.AllocHGlobal(88);try{Marshal.Copy(bytes,0,ptr,88);return Marshal.PtrToStructure<EditCommand>(ptr);}finally{Marshal.FreeHGlobal(ptr);}}
  public void Dispose(){if(Handle!=IntPtr.Zero){EditorNative.end_duel(Handle);Handle=IntPtr.Zero;}}
 }
 public static class EditorPrompt {
  public static bool RequiresResponse(int message)=>(message>=10&&message<=26)||(message>=140&&message<=143);
  public static bool ShouldPassChain(byte[] data,uint timing){if(data==null||data.Length<12||data[0]!=16||Player(data)<0||timing>2)return false;int count=data[2];if(data.Length<12+count*14)return false;for(int i=0;i<count;i++)if(data[13+i*14]!=0)return false;return count==0||timing==0||(timing==2&&data[3]==0);}
  public static int Player(byte[] data){if(data==null||data.Length<2||!RequiresResponse(data[0]))return -1;int offset=data[0]==23?2:1;if(data.Length<=offset||data[offset]>1)return -1;return data[offset];}
 }

}
