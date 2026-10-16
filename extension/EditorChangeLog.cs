using System;using System.Linq;using System.Collections.Generic;using MDPro3.DuelEditor;using DB=MDPro3.Duel.YGOSharp.CardsManager;
namespace DuelStructure {
 static class EditorChangeLog {
  static string Player(uint p)=>"玩家 "+(p+1);
  static string Name(uint code){var card=DB.Get((int)code,false);return "「"+(card==null?code.ToString():card.Name)+"」["+code+"]";}
  static string Card(EditorCard c)=>Name(c.EffectiveCode==0?c.Code:c.EffectiveCode);
  static string Zone(uint z)=>z==1?"卡组":z==2?"手牌":z==4?"怪兽区":z==8?"魔法陷阱区":z==16?"墓地":z==32?"除外区":z==64?"额外卡组":z==256?"场地区":z==512?"灵摆区":z==128?"超量素材":"其他区域";
  static string Place(EditorCard c)=>Player(c.Player)+"的"+Zone(c.Location)+((c.Location==4||c.Location==8)?"第 "+(c.Sequence+1)+" 格":"");
  static string Position(uint p)=>p==1?"表侧攻击":p==2?"里侧攻击":p==4?"表侧守备":p==8?"里侧守备":"表示形式 "+p;
  static string Method(uint m)=>m==1?"特殊召唤":m==2?"通常召唤":m==3?"反转召唤":"未设置召唤方式";
  static string Form(uint f)=>f==0?"原本卡片形态":f==1?"怪兽作为永续魔法":f==2?"怪兽作为永续陷阱":f==4?"自身效果陷阱怪兽":"自定义怪兽";
  static string BitNames(uint value,uint[] bits,string[] names)=>string.Join("／",bits.Select((bit,i)=>(value&bit)!=0?names[i]:null).Where(n=>n!=null));
  static string Race(uint value)=>BitNames(value,Enumerable.Range(0,26).Select(i=>1u<<i).ToArray(),new[]{"战士","魔法师","天使","恶魔","不死","机械","水","炎","岩石","鸟兽","植物","昆虫","雷","龙","兽","兽战士","恐龙","鱼","海龙","爬虫类","念动力","幻神兽","创造神","幻龙","电子界","幻想魔"});
  static string Attribute(uint value)=>BitNames(value,new uint[]{1,2,4,8,16,32,64},new[]{"地","水","炎","风","光","暗","神"});
  static string Type(uint value)=>BitNames(value,new uint[]{1,2,4,0x10,0x20,0x40,0x80,0x200,0x400,0x800,0x1000,0x2000,0x200000,0x400000,0x800000,0x1000000,0x2000000,0x4000000},new[]{"怪兽","魔法","陷阱","通常","效果","融合","仪式","灵魂","同盟","二重","调整","同调","反转","卡通","超量","灵摆","特殊召唤专用","连接"});
  public static void Record(EditCommand cmd,EditorCard[] before,EditorCard[] after){
   var old=before.FirstOrDefault(c=>c.Id==cmd.Target);if(old.Id==0)old=after.FirstOrDefault(c=>c.Id==cmd.Target);var now=cmd.Opcode==1?after.FirstOrDefault(c=>!before.Any(b=>b.Id==c.Id)):after.FirstOrDefault(c=>c.Id==cmd.Target);string text;
   switch(cmd.Opcode){
    case 1:text="在"+Place(now)+"生成"+((now.EffectiveType&1)!=0?"怪兽":"卡片")+Card(now);if((now.Location&12)!=0)text+="（"+Position(now.Position)+(now.Location==4?"；"+Method(now.SummonMethod)+"状态；攻击力 "+(int)now.Attack+"／守备力 "+(int)now.Defense:"")+"）";if(now.Owner!=now.Player)text+="；原持有者为"+Player(now.Owner);break;
    case 2:text="将"+Player(cmd.Player)+"的 LP 设为 "+cmd.Value;break;
    case 3:text="从"+Place(old)+"删除"+Card(old);break;
    case 4:var changes=new List<string>();
     if(old.Player!=now.Player||old.Location!=now.Location||old.Sequence!=now.Sequence)changes.Add("位置："+Place(old)+" → "+Place(now));
     if(old.Owner!=now.Owner)changes.Add("持有者："+Player(old.Owner)+" → "+Player(now.Owner));
     if(old.Position!=now.Position)changes.Add("表示："+Position(old.Position)+" → "+Position(now.Position));
     if(old.Form!=now.Form)changes.Add("形态："+Form(old.Form)+" → "+Form(now.Form));
     if(old.EffectiveCode!=now.EffectiveCode)changes.Add("卡名："+Card(old)+" → "+Card(now));
     if(old.Attack!=now.Attack)changes.Add("攻击力："+(int)old.Attack+" → "+(int)now.Attack);
     if(old.Defense!=now.Defense)changes.Add("守备力："+(int)old.Defense+" → "+(int)now.Defense);
     if(old.Level!=now.Level)changes.Add("等级／阶级／连接数："+old.Level+" → "+now.Level);
     if(old.Race!=now.Race)changes.Add("种族："+Race(old.Race)+" → "+Race(now.Race));
     if(old.Attribute!=now.Attribute)changes.Add("属性："+Attribute(old.Attribute)+" → "+Attribute(now.Attribute));
     if(old.EffectiveType!=now.EffectiveType)changes.Add("类型："+Type(old.EffectiveType)+" → "+Type(now.EffectiveType));
     if(old.LinkMarker!=now.LinkMarker)changes.Add("连接箭头："+Arrows(old.LinkMarker)+" → "+Arrows(now.LinkMarker));
     if(old.SummonMethod!=now.SummonMethod||old.SummonSource!=now.SummonSource)changes.Add("召唤信息："+Method(now.SummonMethod)+((now.SummonMethod==1)?"，来源"+Zone(now.SummonSource):""));
     if(old.Flags!=now.Flags)changes.Add("召唤／送墓标记："+Flags(now.Flags));
     if((old.States&0x4000000)!=(now.States&0x4000000))changes.Add("被禁止令适用："+Yes((now.States&0x4000000)!=0));
     if((old.States&0x10)!=(now.States&0x10))changes.Add("当回合盖放："+Yes((now.States&0x10)!=0));
     for(int i=0;i<32;i++)if(((old.Restrictions^now.Restrictions)&(1u<<i))!=0)changes.Add(EditorBehaviour.restrictionLabels[i]+"："+Yes((now.Restrictions&(1u<<i))!=0));
     text="修改"+Place(old)+"的"+Card(old)+(changes.Count>0?"\n"+string.Join("；",changes):"（保存卡片配置）");break;
    case 5:text="将"+Place(old)+"的"+Card(old)+"移至从顶端第 "+(cmd.Sequence+1)+" 张";break;
    case 6:text="为"+Place(old)+"的"+Card(old)+(cmd.Position==0?"移除":"添加／更新")+"状态「"+Effects.Name(cmd.Code)+"」"+(cmd.Position==0?"":"，数值 "+unchecked((int)cmd.Value));break;
    case 7:text="立即洗切"+Player(cmd.Player)+"的"+Zone(cmd.Location);break;
    case 8:text="让"+Player(cmd.Player)+" 抽 "+cmd.Value+" 张卡";break;
    case 9:text="为"+Place(old)+"的"+Card(old)+"添加超量素材"+Name(cmd.Code)+" × "+cmd.Value;break;
    case 10:text="将"+Place(old)+"的"+Card(old)+"上的「"+EditorBehaviour.CounterName(cmd.Code)+"」设为 "+cmd.Value+" 个";break;
    default:return;
   }
   string metadata="【编辑器】"+text;string detail="";if(cmd.Opcode==4&&text.Contains("\n"))detail=text.Substring(text.IndexOf('\n')+1);if(cmd.Opcode==9)detail=Name(cmd.Code)+" × "+cmd.Value+"；素材持有者为"+Player(cmd.Owner);if(cmd.Opcode==10)detail=EditorBehaviour.CounterName(cmd.Code)+"："+cmd.Value; if(cmd.Opcode==6)detail=Effects.Name(cmd.Code)+(cmd.Position==0?"":"："+unchecked((int)cmd.Value));
   NativeLogHistory.RecordEdit(metadata,log=>{
    if(cmd.Opcode==2){int local=(int)cmd.Player==Host.Session.View?0:1;if(local==0)MDPro3.Duel.LogMessage.life0=(int)cmd.Value;else MDPro3.Duel.LogMessage.life1=(int)cmd.Value;log.AddLpChangeMessageToLog(local,"编辑器设置 LP",(int)cmd.Value,false,false);return;}
    EditorCard target=cmd.Opcode==1||cmd.Opcode==4?now:old;MDPro3.GPS from=null,to;int cardCode=0;string reason;
    if(cmd.Opcode==8){from=GPS(cmd.Player,1);to=GPS(cmd.Player,2);reason="编辑器抽 "+cmd.Value+" 张";}
    else if(cmd.Opcode==7){to=GPS(cmd.Player,cmd.Location);reason="编辑器洗牌";}
    else{cardCode=(int)(target.EffectiveCode==0?target.Code:target.EffectiveCode);to=GPS(target.Player,target.Location,target.Sequence,target.Position);reason=cmd.Opcode==9?"编辑器添加素材":cmd.Opcode==10?"编辑器设置指示物":cmd.Opcode==1?"编辑器生成":cmd.Opcode==3?"编辑器删除":cmd.Opcode==5?"编辑器排序":cmd.Opcode==6?(cmd.Position==0?"编辑器移除效果":"编辑器设置效果"):"编辑器修改";if(cmd.Opcode==5||(cmd.Opcode==4&&(old.Player!=now.Player||old.Location!=now.Location||old.Sequence!=now.Sequence)))from=GPS(old.Player,old.Location,old.Sequence,old.Position);if(cmd.Opcode==5)to=GPS(now.Player,now.Location,now.Sequence,now.Position);}
    log.AddSingleCardMessageToLog(cardCode,from,to,reason,false,false);
   },detail);
  }
  static MDPro3.GPS GPS(uint player,uint location,uint sequence=0,uint position=1)=>new MDPro3.GPS{controller=player==(uint)Host.Session.View?0u:1u,location=location==256?8u:location==512?8u:location,sequence=location==256?5u:sequence,position=(int)position};
  static string Yes(bool v)=>v?"是":"否";
  static string Arrows(uint value){var names=BitNames(value,new uint[]{0x40,0x80,0x100,8,0x20,1,2,4},new[]{"↖","↑","↗","←","→","↙","↓","↘"});return names==""?"无":names;}
  static string Flags(uint value)=>"当回合召唤 "+Yes((value&14)!=0)+"，正规召唤 "+Yes((value&1)!=0)+"，当回合送墓 "+Yes((value&16)!=0);
 }
}
