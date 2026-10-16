using System;using System.Collections.Generic;using UnityEngine;
namespace DuelStructure {
 static class WindowDrag {
  static readonly Dictionary<string,Vector2> positions=new Dictionary<string,Vector2>();static string dragging="";static Vector2 grab;
  public static Rect Place(string key,Rect initial,float header=32,bool enabled=true){var mouse=UnityEngine.InputSystem.Mouse.current;return Move(key,initial,mouse==null?Vector2.zero:new Vector2(mouse.position.ReadValue().x,Screen.height-mouse.position.ReadValue().y),mouse!=null&&mouse.leftButton.wasPressedThisFrame,mouse!=null&&mouse.leftButton.isPressed,header,enabled);}
  public static Rect Move(string key,Rect initial,Vector2 pointer,bool pressed,bool held,float header,bool enabled){if(positions.TryGetValue(key,out var p))initial.position=p;if(!held)dragging="";var title=new Rect(initial.x,initial.y,Math.Max(0,initial.width-90),header);if(enabled&&pressed&&title.Contains(pointer)){dragging=key;grab=pointer-initial.position;}if(enabled&&held&&dragging==key)initial.position=pointer-grab;initial.x=Math.Clamp(initial.x,0,Math.Max(0,Screen.width-initial.width));initial.y=Math.Clamp(initial.y,0,Math.Max(0,Screen.height-Math.Min(initial.height,header)));positions[key]=initial.position;return initial;}
 }
 static class NativeOcclusion {
  public static bool FullCover{get{var p=MDPro3.Program.instance;return NativeReplay.Pending||p!=null&&(p.setting!=null&&p.setting.showing||p.currentSubServant!=null&&p.currentSubServant.showing);}}
  static bool Over(Rect target,RectTransform rect){if(rect==null||!rect.gameObject.activeInHierarchy)return false;var corners=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);rect.GetWorldCorners(corners);var camera=MDPro3.Program.instance.camera_.cameraUI;var a=RectTransformUtility.WorldToScreenPoint(camera,corners[1]);var b=RectTransformUtility.WorldToScreenPoint(camera,corners[3]);return target.Overlaps(new Rect(a.x,Screen.height-a.y,b.x-a.x,a.y-b.y));}
  public static bool Covers(Rect r){if(FullCover)return true;if(!Host.InDuel||MDPro3.Program.instance.ocgcore.servantUI==null||!MDPro3.Program.instance.ocgcore.DuelBGManager.loaded)return false;var ui=MDPro3.Program.instance.ocgcore.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>();if(ui.DuelLog.showing&&ui.DuelLog.cg.alpha>.01f&&Over(r,ui.DuelLog.baseRect))return true;if(ui.CardList.showing&&ui.CardList.cg.alpha>.01f&&Over(r,ui.CardList.baseRect))return true;var side=MDPro3.Program.instance.ui_.currentSidePanel;return side!=null&&Over(r,side.GetComponent<RectTransform>());}
 }
}
