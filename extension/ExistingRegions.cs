using System;using System.Linq;using UnityEngine;using MDPro3.DuelEditor;
using GUI=DuelStructure.NativeUI.Gui;using GUILayout=DuelStructure.NativeUI.Layout;
namespace DuelStructure {
 public partial class EditorBehaviour {
  static bool regionCardsOpen;static uint regionCardsPlayer,regionCardsZone;static Vector2 regionCardsScroll;
  static readonly uint[] existingZones={4,8,0x100,0x200,2,16,32,1,64};
  static uint ExistingZone(EditorCard c)=>c.Location==8?(c.Sequence==5?0x100u:c.PendulumZone!=0||c.Sequence>=6?0x200u:8u):c.Location;
  static string ExistingZoneName(uint region)=>region==0x100?"场地区":region==0x200?"灵摆区":region==4?"怪兽区":region==8?"魔法陷阱区":region==2?"手牌":region==16?"墓地":region==32?"除外区":region==1?"主卡组":"额外卡组";
  static string ExistingRegionTitle(uint player,uint region,int count)=>"玩家 "+(player+1)+" · "+ExistingZoneName(region)+"（"+count+" 张）";
  static void ExistingRegions(){var cards=Host.Session.Cards().Where(c=>c.Location!=128).ToArray();
   LegacyList(15,65,380,160,ref existingScroll,()=>{bool any=false;for(uint player=0;player<2;player++)foreach(uint region in existingZones){int count=cards.Count(c=>c.Player==player&&ExistingZone(c)==region);if(count==0)continue;any=true;if(GUILayout.Button(ExistingRegionTitle(player,region,count),GUILayout.Height(32))){regionCardsPlayer=player;regionCardsZone=region;regionCardsScroll=Vector2.zero;regionCardsOpen=true;GUI.FocusControl(null);}}if(!any)GUILayout.Label("当前双方区域内没有卡片");});
  }
  static void PreviewExistingCard(EditorCard c){previewCode=c.Code;previewFrame=Time.frameCount+1;if(!Host.DuelUIReady)return;var desc=MDPro3.Program.instance.ocgcore.GetUI<MDPro3.UI.ServantUI.OcgCoreUI>().CardDescription;if(desc.data==null||desc.data.Id!=(int)c.Code)desc.Show(null,null,(int)c.Code,null);}
  static void RegionCardPopup(){if(!regionCardsOpen)return;if(!open||page!=1||legacyMode!=1||Host.Session==null){regionCardsOpen=false;return;}
   var cards=Host.Session.Cards().Where(c=>c.Player==regionCardsPlayer&&ExistingZone(c)==regionCardsZone&&c.Location!=128);cards=regionCardsZone==1?cards.OrderByDescending(c=>c.Sequence):cards.OrderBy(c=>c.Sequence);var list=cards.ToArray();
   GUI.enabled=true;var r=WindowDrag.Place("region-cards",new Rect(Math.Max(12,(Screen.width-640)/2),Math.Max(35,(Screen.height-510)/2),Math.Min(640,Screen.width-24),Math.Min(510,Screen.height-70)));GUI.Box(r,"");NativeUI.Renderer.Label(new Rect(r.x+15,r.y+7,r.width-30,30),ExistingRegionTitle(regionCardsPlayer,regionCardsZone,list.Length));
   GUILayout.BeginArea(new Rect(r.x+15,r.y+50,r.width-30,r.height-115));regionCardsScroll=GUILayout.BeginScrollView(regionCardsScroll,GUILayout.Height(r.height-123));
   for(int i=0;i<list.Length;i++){var c=list[i];uint display=c.EffectiveCode==0?c.Code:c.EffectiveCode;string position=regionCardsZone==4?"位置 "+(c.Sequence+1):regionCardsZone==8?"位置 "+(c.Sequence+1):regionCardsZone==1?"牌顶第 "+(i+1)+" 张":"第 "+(i+1)+" 张";bool choose=GUILayout.Button(position+" · "+CardName(display)+" ["+c.Code+"]",GUILayout.Height(34));if(GUILayout.IsLastHovered())PreviewExistingCard(c);if(choose){LoadCard(c);legacyMode=1;regionCardsOpen=false;PreviewExistingCard(c);break;}}
   if(list.Length==0)GUILayout.Label("这个区域已经没有卡片");GUILayout.EndScrollView();GUILayout.EndArea();if(NativeUI.Renderer.Button(new Rect(r.x+15,r.y+r.height-48,r.width-30,33),"返回区域列表")){regionCardsOpen=false;GUI.FocusControl(null);}
  }
  public static void ProbeExistingRegions(){open=true;page=1;legacyMode=1;existingScroll=Vector2.zero;regionCardsOpen=false;}
  public static void ProbeRegionScroll(float y){existingScroll.y=y;}
  public static bool RegionCardsOpen=>regionCardsOpen;public static ulong SelectedExistingId=>selected;
 }
}
