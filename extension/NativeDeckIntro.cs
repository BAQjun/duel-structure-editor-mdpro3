using System;using HarmonyLib;using Cysharp.Threading.Tasks;using MDPro3.DuelEditor;using MDPro3.Duel;
namespace DuelStructure {
 static class NativeDeckIntro {
  static EditorSession session;static IntPtr manager;public static int Played{get;private set;}public static int Skipped{get;private set;}
  public static void Reset(){session=null;manager=IntPtr.Zero;Played=Skipped=0;}
  public static bool Begin(DuelBGManager bg){
   if(Host.Session==null)return true;
   if(!ReferenceEquals(session,Host.Session)||manager!=bg.Pointer){session=Host.Session;manager=bg.Pointer;Played=Skipped=0;}
   if(bg.myDeck==null||bg.myExtra==null||bg.opDeck==null||bg.opExtra==null)return true;
   if(Played==0){Played++;return true;}
   Skipped++;return false;
  }
 }
 [HarmonyPatch(typeof(DuelBGManager),nameof(DuelBGManager.ShowAllDeckModelsAsync))]class OnceEditorDeckIntro {
  static bool Prefix(DuelBGManager __instance,ref UniTask __result){if(NativeDeckIntro.Begin(__instance))return true;__result=UniTask.CompletedTask;return false;}
 }
}
