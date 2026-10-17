using System;using System.IO;using System.Linq;using UnityEngine;using MDPro3.DuelEditor;using MDPro3.Servant;
namespace DuelStructure {
 static class DiameterSmoke {
  static int stage,frame;static bool pot,baronne,diameter;static int switches,handBefore;
  public static bool Enabled=>File.Exists(Path.Combine(BepInEx.Paths.PluginPath,"DuelStructure","smoke.diameter"));
  static void Check(bool value,string text){if(!value)throw new Exception("SMOKE FAIL "+text);Plugin.Instance.Log.LogInfo("SMOKE PASS "+text);}
  static void Reply(byte[] value){var core=MDPro3.Program.instance.ocgcore;core.DuelBGManager.ClearResponse();core.messageDispatcher.playerResponed=true;Host.Session.Respond(value);Host.Session.Paused=false;frame=Time.frameCount+90;}
  static NativeChainState.Link[] Chains(){var data=new byte[1048576];int n=EditorNative.query_field_info(Host.Session.Handle,data);return NativeChainState.Read(data.Skip(1).Take(n-1).ToArray());}
  public static void Tick(){if(stage<0||Time.frameCount<frame)return;try{
   if(stage==0){if(MDPro3.Program.instance?.menu==null||!MDPro3.Program.instance.menu.showing||MDPro3.Duel.YGOSharp.CardsManager._cards.Count==0)return;Host.Start(new EditorSetup{Decks=new[]{Enumerable.Repeat(89631139u,40).ToArray(),new uint[0],Enumerable.Repeat(89631139u,40).ToArray(),new uint[0]},Hands=new uint[]{0,0},Seeds=new uint[]{1,2,3,4,5,6,7,8}});stage=1;return;}
   var s=Host.Session;if(!Host.DuelUIReady)return;
   if(stage==1){s.Timings[0]=s.Timings[1]=1;s.Paused=false;stage=2;return;}
   if(stage==2){if(!s.Waiting)return;if(s.CurrentPrompt[0]!=11){if(s.CurrentPrompt[0]==16){s.RespondAutomatic(s.PromptPlayer,BitConverter.GetBytes(-1));return;}throw new Exception("Unexpected setup prompt");}
    var c=EditCommand.Create(1);c.Code=84815190;c.Player=c.Owner=1;c.Location=4;c.Position=1;c.Value=32u|(64u<<8);Check(s.Apply(c)==0,"diameter opponent Baronne");
    c=EditCommand.Create(1);c.Code=88021907;c.Location=4;c.Position=1;c.Value=32u|(64u<<8);var begin=EditCommand.Create(12);begin.Value=3;var m=EditCommand.Create(13);m.Code=17946349;m.Location=4;m.Sequence=1;m.Position=1;m.Value=128;m.Level=1;var m2=m;m2.Code=89631139;m2.Sequence=2;var m3=m2;m3.Sequence=3;var end=EditCommand.Create(14);Check(s.ApplyGroup(new[]{c,begin,m,m2,m3,end})==0,"diameter Xyz summon and three real materials");
    c=EditCommand.Create(1);c.Code=55144522;c.Location=2;c.Position=8;Check(s.Apply(c)==0,"diameter own Pot of Greed");s.Paused=true;stage=3;frame=Time.frameCount+150;return;
   }
   if(stage==3){Check(s.Waiting&&s.CurrentPrompt[0]==11,"diameter main phase ready");using(var r=new BinaryReader(new MemoryStream(s.CurrentPrompt))){r.ReadBytes(2);for(int i=0;i<5;i++)r.ReadBytes(r.ReadByte()*7);int n=r.ReadByte(),index=-1;for(int i=0;i<n;i++){int code=r.ReadInt32();r.ReadBytes(7);if(code==55144522)index=i;}Check(index>=0,"diameter native Pot activation available");handBefore=s.Cards().Count(c=>c.Player==0&&c.Location==2);pot=true;Reply(BitConverter.GetBytes(5|(index<<16)));}stage=4;return;}
   if(stage==5){var links=Chains();Check(OcgCore.codesInChain.Count==links.Length&&OcgCore.cardsInChain.Count==links.Length,"view switch restores active chain length "+links.Length);for(int i=0;i<links.Length;i++)Check(OcgCore.cardsInChain[i]!=null&&OcgCore.cardsInChain[i].GetData().Id==links[i].Code&&OcgCore.codesInChain[i]==links[i].Code,"view switch chain "+(i+1)+" retains card "+links[i].Code);var log=MDPro3.Program.instance.ocgcore.messageDispatcher.log;Check(log.cardsInChain.Count==links.Length,"effect log restores same chain length");stage=4;}
   if(stage==4){if(!s.Waiting){s.Paused=false;return;}s.Paused=true;var p=s.CurrentPrompt;
    if(p[0]==11){if(NativeChainState.LastDisabledCode==0){frame=Time.frameCount+90;return;}Check(pot&&baronne&&diameter&&switches>=2,"three-link response completed across both perspectives");Check(NativeChainState.LastDisabledCode==84815190,"native negate marker identifies opponent Baronne, not Laplacian");Check(s.Cards().Count(c=>c.Player==0&&c.Location==2)==handBefore+1,"own Pot resolves and draws two after Diameter negates opponent");Check(Host.Error=="","diameter no host error");Plugin.Instance.Log.LogInfo("DIAMETER_CHAIN_UI_TEST_COMPLETE");stage=-1;return;}
    if(s.View!=s.PromptPlayer){s.SwitchView();switches++;stage=5;frame=Time.frameCount+150;return;}
    if(p[0]==16){int index=-1;using(var r=new BinaryReader(new MemoryStream(p))){r.ReadByte();r.ReadByte();int n=r.ReadByte();r.ReadBytes(9);for(int i=0;i<n;i++){r.ReadBytes(2);uint code=r.ReadUInt32();r.ReadInt32();uint desc=r.ReadUInt32();if(code==84815190&&!baronne){index=i;baronne=true;break;}if(code==88021907&&desc==17946349u*16+1&&!diameter){index=i;diameter=true;break;}}}Reply(BitConverter.GetBytes(index));return;}
    if(p[0]==18){Reply(new byte[]{0,8,0});return;}if(p[0]==12){Reply(BitConverter.GetBytes(0));return;}if(p[0]==13){Reply(BitConverter.GetBytes(0));return;}if(p[0]==14){Reply(BitConverter.GetBytes(0));return;}if(p[0]==15){Reply(new byte[]{1,0});return;}if(p[0]==19){Reply(BitConverter.GetBytes(1));return;}throw new Exception("Unexpected diameter prompt "+p[0]);
   }
  }catch(Exception e){Plugin.Instance.Log.LogError(e);stage=-1;if(Host.Session!=null)Host.Session.Paused=true;}}
 }
}
