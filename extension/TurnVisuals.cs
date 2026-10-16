using UnityEngine;using MDPro3.Servant;using MDPro3.UI;using MDPro3.DuelEditor;
namespace DuelStructure {
 static class TurnVisuals {
  static int view=-1;static uint turn=uint.MaxValue,phase=uint.MaxValue;static bool mine;static bool pending=true;
  public static void Reset(){view=-1;turn=phase=uint.MaxValue;pending=true;}
  public static void Request(){pending=true;}
  public static void Tick(){var s=Host.Session;if(!Host.InDuel||s==null)return;var bg=MDPro3.Program.instance.ocgcore.DuelBGManager;if(!bg.loaded||PhaseButtonHandler.playerPart==null||PhaseButtonHandler.textAbove==null||bg.playableGuide==null||!bg.playableGuide.loaded)return;
   uint actualTurn=EditorNative.editor_turn_id(s.Handle),actualPhase=EditorNative.editor_phase(s.Handle);bool actualMine=EditorNative.editor_turn_player(s.Handle)==s.View;bool changed=pending||view!=s.View||turn!=actualTurn||mine!=actualMine;
   OcgCore.myTurn=actualMine;OcgCore.turns=(int)actualTurn;OcgCore.duelPhase=(MDPro3.Duel.YGOSharp.DuelPhase)actualPhase;
   if(changed||PhaseButtonHandler.textAbove.text!="Turn "+actualTurn){PhaseButtonHandler.TurnChange(actualMine,(int)actualTurn);bg.SetPlayableGuide(actualMine);Plugin.Instance.Log.LogInfo("VIEW_TURN_EFFECTS view="+s.View+" turnPlayer="+EditorNative.editor_turn_player(s.Handle)+" turn="+actualTurn+" mine="+actualMine);}
   if(changed||phase!=actualPhase)PhaseButtonHandler.SetTextMain(actualPhase==0?"Ready":actualPhase==1?"Draw":actualPhase==2?"Standby":actualPhase==4?"Main1":actualPhase==0x100?"Main2":actualPhase==0x200?"End":"Battle");
   view=s.View;turn=actualTurn;phase=actualPhase;mine=actualMine;pending=false;
  }
 }
}
