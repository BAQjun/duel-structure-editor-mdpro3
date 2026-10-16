using System;using System.Linq;using System.Collections.Generic;using System.IO;using Mono.Cecil;
namespace DuelStructureMonoAccess {
 public static class Patcher {
  public static IEnumerable<string> TargetDLLs{get{yield return "Assembly-CSharp.dll";}}
  static IEnumerable<TypeDefinition> Types(TypeDefinition type){yield return type;foreach(var child in type.NestedTypes)foreach(var item in Types(child))yield return item;}
  public static int Promoted;
  public static void Patch(AssemblyDefinition assembly){var file=Path.Combine(BepInEx.Paths.PluginPath,"DuelStructure","DuelStructure.Mono.dll");if(!File.Exists(file))return;Apply(assembly,file);}
  public static void Apply(AssemblyDefinition assembly,string file){using(var plugin=ModuleDefinition.ReadModule(file)){
   var required=new Dictionary<string,HashSet<string>>();foreach(var type in plugin.GetTypeReferences()){var scope=type.GetElementType().Scope as AssemblyNameReference;if(scope!=null&&scope.Name=="Assembly-CSharp"&&!required.ContainsKey(type.GetElementType().FullName))required[type.GetElementType().FullName]=new HashSet<string>();}foreach(var member in plugin.GetMemberReferences()){var type=member.DeclaringType.GetElementType();var scope=type.Scope as AssemblyNameReference;if(scope==null||scope.Name!="Assembly-CSharp")continue;HashSet<string> names;if(!required.TryGetValue(type.FullName,out names)){names=new HashSet<string>();required[type.FullName]=names;}names.Add(member.Name);}
   foreach(var type in assembly.MainModule.Types.SelectMany(Types)){HashSet<string> names;if(!required.TryGetValue(type.FullName,out names))continue;for(var parent=type;parent!=null;parent=parent.DeclaringType){if(parent.IsNested)parent.IsNestedPublic=true;else parent.IsPublic=true;}
    foreach(var field in type.Fields.Where(f=>names.Contains(f.Name))){if(!field.IsPublic){if(!field.CustomAttributes.Any(a=>a.AttributeType.FullName=="UnityEngine.SerializeField"))field.IsNotSerialized=true;field.IsPublic=true;Promoted++;}}
    foreach(var method in type.Methods.Where(m=>names.Contains(m.Name))){if(!method.IsPublic){method.IsPublic=true;Promoted++;}}
   }
  }}
 }
}
