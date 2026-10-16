using System;using System.Collections.Generic;using System.IO;using UnityEngine;using Il2CppInterop.Runtime.InteropTypes.Arrays;
namespace DuelStructure {
 static class EditorIcons {
  static readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
  static Sprite white;public static Sprite White {get{if(white!=null)return white;var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(tex,(Il2CppStructArray<byte>)File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location),"assets","icons","white.png")),false);white=Sprite.Create(tex,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);return white;}}
  public static Sprite[] Get(string name){if(sprites.TryGetValue(name,out var s))return s;s=new Sprite[3];int i=0;foreach(string state in new[]{"normal","hover","pressed"}){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);var path=Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location),"assets","icons",name+"-"+state+".png");if(!ImageConversion.LoadImage(texture,(Il2CppStructArray<byte>)File.ReadAllBytes(path),false))throw new InvalidDataException("图标载入失败："+name);s[i++]=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),160);UnityEngine.Object.DontDestroyOnLoad(texture);}sprites[name]=s;return s;}
 }
}
