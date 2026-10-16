using System;using System.Runtime.CompilerServices;using System.Runtime.InteropServices;using System.Threading;
namespace DuelStructure {
 static class MonoCompat {
  public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));public static int Clamp(int v,int min,int max)=>Math.Max(min,Math.Min(max,v));
  public static string RelativePath(string root,string path){var prefix=System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar)+System.IO.Path.DirectorySeparatorChar;var full=System.IO.Path.GetFullPath(path);return full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)?full.Substring(prefix.Length):full;}
  public static string[] ReadAllLines(string path){return System.IO.File.ReadAllText(path).Replace("\r\n","\n").Replace('\r','\n').Split('\n');}public static System.Collections.Generic.IEnumerable<string> ReadLines(string path)=>ReadAllLines(path);
  public static void WriteLines(string path,System.Collections.Generic.IEnumerable<string> lines){System.IO.File.WriteAllText(path,string.Join("\n",lines)+"\n");}
  public static uint OrTypes(uint[] values){uint result=0;foreach(uint v in values)result|=v;return result;}
  public static float MonoMax<T>(this System.Collections.Generic.IEnumerable<T> values,Func<T,float> select){float result=float.MinValue;foreach(T v in values)result=Math.Max(result,select(v));return result;}
  public static float MonoSum<T>(this System.Collections.Generic.IEnumerable<T> values,Func<T,float> select){float result=0;foreach(T v in values)result+=select(v);return result;}
  public static int MonoSum<T>(this System.Collections.Generic.IEnumerable<T> values,Func<T,int> select){int result=0;foreach(T v in values)result+=select(v);return result;}
  public static System.Collections.Generic.IEnumerable<T> MonoDefaultIfEmpty<T>(this System.Collections.Generic.IEnumerable<T> values,T fallback){bool any=false;foreach(T v in values){any=true;yield return v;}if(!any)yield return fallback;}
  sealed class RefComparer:System.Collections.Generic.IEqualityComparer<object>{public new bool Equals(object a,object b)=>ReferenceEquals(a,b);public int GetHashCode(object v)=>RuntimeHelpers.GetHashCode(v);}
  static long next=1L<<40;static readonly System.Collections.Generic.Dictionary<object,IntPtr> ids=new System.Collections.Generic.Dictionary<object,IntPtr>(new RefComparer());
  public static void ClearHandles(){ids.Clear();}
  public static IntPtr MonoHandle(this object value){if(value==null)return IntPtr.Zero;var unity=value as UnityEngine.Object;if(unity!=null)return new IntPtr(unity.GetInstanceID());IntPtr id;if(!ids.TryGetValue(value,out id)){id=new IntPtr(Interlocked.Increment(ref next));ids[value]=id;}return id;}
  public static T MonoAs<T>(this object value){return (T)value;}public static T MonoTryAs<T>(this object value)where T:class{return value as T;}
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true,EntryPoint="LoadLibraryW")]static extern IntPtr Load(string path);
  public static IntPtr LoadLibrary(string path){var value=Load(path);if(value==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),"无法载入编辑器运行库："+path);return value;}
 }
}
