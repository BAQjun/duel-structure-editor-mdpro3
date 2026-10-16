using System;using System.Linq;using UnityEngine;using HarmonyLib;using MDPro3;using MDPro3.Duel;using MDPro3.DuelEditor;
namespace DuelStructure {
 static class NativeAttachmentVisuals {
  static bool pending;
  public static void Request(){pending=Host.Session!=null;}
  public static void Tick(){if(!pending||!Host.DuelUIReady)return;var s=Host.Session;var core=Program.instance.ocgcore;var cards=s.Cards().Where(c=>(c.Location&12)!=0).ToArray();var counters=s.Counters();foreach(var entry in cards){var card=core.GCS_Get(new GPS{controller=entry.Player==(uint)s.View?0u:1u,location=entry.Location,sequence=entry.Sequence});if(card==null)return;}
   foreach(var entry in cards){var card=core.GCS_Get(new GPS{controller=entry.Player==(uint)s.View?0u:1u,location=entry.Location,sequence=entry.Sequence});foreach(int type in card.cardCounters.Keys)card.tails.Remove(StringHelper.Get("counter",type),true);card.ClearCounter();foreach(var counter in counters.Where(c=>c.CardId==entry.Id)){card.cardCounters.Add((int)counter.Type,(int)counter.Count);string name=StringHelper.Get("counter",(int)counter.Type);if(!string.IsNullOrWhiteSpace(name)){card.tails.Remove(name,true);card.tails.Add(name);foreach(var part in card.tails.strings)if(part.str==name)part.count=(int)counter.Count;}}card.tails.ReCreateString();card.RefreshLabel();}pending=false;
  }
 }
 [HarmonyPatch(typeof(DuelMessage),nameof(DuelMessage.GameMessage_ReloadField))]class SyncEditorCounterVisuals{static void Postfix(){NativeAttachmentVisuals.Request();}}
}
