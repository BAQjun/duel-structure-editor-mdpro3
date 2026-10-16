from pathlib import Path
import re
r=Path(__file__).resolve().parents[1];out=r/'work/mono-generated';out.mkdir(parents=True,exist_ok=True)
for p in list((r/'extension').glob('*.cs'))+[r/'upstream/Assets/Scripts/MDPro3/DuelEditor'/name for name in ['EditorNative.cs','EditorSession.cs']]:
 s=p.read_text(encoding='utf-8-sig');s=re.sub(r'using Il2Cpp[^;]+;','',s);s=s.replace('using BepInEx.Unity.IL2CPP;','')
 s=re.sub(r'new (?:Il2CppInterop.Runtime.InteropTypes.Arrays\.)?Il2CppStructArray<([^>]+)>\(([^)]+)\)',r'new \1[\2]',s)
 s=re.sub(r'Il2CppStructArray<([^>]+)>',r'\1[]',s).replace('Il2CppInterop.Runtime.InteropTypes.Arrays.','').replace('Il2CppSystem.','System.')
 s=s.replace('.Pointer','.MonoHandle()').replace('.TryCast<','.MonoTryAs<').replace('.Cast<','.MonoAs<').replace('Il2CppType.Of<T>()','typeof(T)')
 s=re.sub(r'Il2CppType.Of<([^>]+)>\(\)',r'typeof(\1)',s)
 s=s.replace('File.ReadLines(', 'MonoCompat.ReadLines(').replace('File.ReadAllLines(', 'MonoCompat.ReadAllLines(').replace('File.WriteAllLines(', 'MonoCompat.WriteLines(').replace('otherBits.Aggregate(0u,(a,b)=>a|b)', 'MonoCompat.OrTypes(otherBits)').replace('buttons.Max(', 'buttons.MonoMax(').replace('.Sum(', '.MonoSum(').replace('.DefaultIfEmpty(', '.MonoDefaultIfEmpty(').replace('System.IO.Compression.CompressionLevel.Optimal,true', 'System.IO.Compression.CompressionMode.Compress,true')
 s=s.replace('NativeLibrary.Load(', 'MonoCompat.LoadLibrary(').replace('Math.Clamp(', 'MonoCompat.Clamp(').replace('Path.GetRelativePath(', 'MonoCompat.RelativePath(').replace('(UnityAction)(Action)', '(UnityAction)').replace('(UnityAction<float>)(Action<float>)', '(UnityAction<float>)')
 if p.name=='Plugin.cs':
  s=s.replace('public class Plugin:BasePlugin','public class Plugin:BaseUnityPlugin').replace('public override void Load(){','public BepInEx.Logging.ManualLogSource Log=>Logger;\n  void Awake(){').replace('AddComponent<EditorBehaviour>();','gameObject.AddComponent<EditorBehaviour>();').replace('public static void Cleanup(){if(Session==null)return;', 'public static void Cleanup(){if(Session==null)return;MonoCompat.ClearHandles();')
 if p.name=='EditorBehaviour.cs':s=s.replace('public EditorBehaviour(IntPtr p):base(p){}','')
 if p.name=='NativeLogArchive.cs':
  s=s.replace('using System.Text.Json;','using Newtonsoft.Json;')
  s=s.replace('static readonly JsonSerializerOptions options=new JsonSerializerOptions{IncludeFields=true,MaxDepth=96};','static readonly JsonSerializerSettings options=new JsonSerializerSettings{MaxDepth=96,TypeNameHandling=TypeNameHandling.None};')
  s=s.replace('JsonSerializer.SerializeToUtf8Bytes(document,options)','System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(document,options))').replace('JsonSerializer.Deserialize<Document>(plain.ToArray(),options)','JsonConvert.DeserializeObject<Document>(System.Text.Encoding.UTF8.GetString(plain.ToArray()),options)')
 s=s.replace('MonoCompat.', 'DuelStructure.MonoCompat.')
 (out/p.name).write_text(s,encoding='utf-8')
