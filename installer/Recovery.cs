using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;

static class EditorRecovery {
 static string Hash(string file){using(var input=File.OpenRead(file))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(input)).Replace("-", "").ToLowerInvariant();}
 public static string Recover(string directory){
  string root=Path.GetFullPath(directory),config=Path.Combine(root,"doorstop_config.ini"),receipt=Path.Combine(root,"BepInEx/DuelStructureInstaller/install.xml"),loader=Path.Combine(root,"winhttp.dll");
  if(!File.Exists(Path.Combine(root,"MDPro3.exe")))throw new IOException("请选择包含 MDPro3.exe 的游戏目录。");
  foreach(var process in Process.GetProcessesByName("MDPro3")){try{throw new IOException("请先关闭 MDPro3；若游戏无响应，请先在任务管理器结束进程。");}finally{process.Dispose();}}
  if(!File.Exists(receipt))throw new IOException("未找到决构编辑器安装记录，为避免修改其他加载器，未执行恢复。");
  var document=XDocument.Load(receipt);
  if((string)document.Root.Element("Product")!="cn.duelstructure.mdpro3")throw new IOException("安装记录不属于决构编辑器。");
  var entry=document.Descendants("InstalledFile").SingleOrDefault(f=>(string)f.Element("Path")=="winhttp.dll");
  if(entry==null||!File.Exists(loader)||Hash(loader)!=(string)entry.Element("Sha"))throw new IOException("当前加载器与安装记录不符，未修改，请提供日志进一步检查。");
  if(!File.Exists(config))throw new IOException("未找到加载器配置，未执行恢复。");
  if((File.GetAttributes(config)&FileAttributes.ReparsePoint)!=0)throw new IOException("配置是重定向文件，未执行恢复。");
  string text=File.ReadAllText(config);
  if(!Regex.IsMatch(text,@"(?im)^target_assembly\s*=\s*BepInEx[\\/]core[\\/]BepInEx\.Preloader\.dll\s*$"))throw new IOException("当前配置不是已知的 Mono 加载器配置，未修改。");
  var section=new Regex(@"(?ms)(^\[General\][^\S\r\n]*\r?\n)(.*?)(?=^\[|\z)");
  if(!section.IsMatch(text))throw new IOException("加载器配置格式无法识别。");
  string updated=section.Replace(text,m=>m.Groups[1].Value+(Regex.IsMatch(m.Groups[2].Value,@"(?im)^enabled\s*=")?Regex.Replace(m.Groups[2].Value,@"(?im)^enabled\s*=.*$","enabled = false"):"enabled = false\r\n"+m.Groups[2].Value));
  string backup=config+".before-recovery-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);
  File.Copy(config,backup,false);
  string archive=Path.Combine(root,"决构编辑器-故障日志-"+Guid.NewGuid().ToString("N")+".zip");
  using(var stream=File.Create(archive))using(var zip=new ZipArchive(stream,ZipArchiveMode.Create)){
   foreach(string relative in new[]{"BepInEx/DuelStructureInstaller/install.xml","BepInEx/LogOutput.log","doorstop_config.ini","output_log.txt"}.Concat(Directory.GetFiles(root,"preloader_*.log").Select(Path.GetFileName))){
    string file=Path.Combine(root,relative);if(!File.Exists(file)||new FileInfo(file).Length>16*1024*1024)continue;
    var item=zip.CreateEntry(relative.Replace('\\','/'));using(var input=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var output=item.Open())input.CopyTo(output);
   }
  }
  string temporary=config+".recovery-"+Guid.NewGuid().ToString("N");
  try{File.WriteAllText(temporary,updated,new UTF8Encoding(false));File.Replace(temporary,config,null);}finally{if(File.Exists(temporary))File.Delete(temporary);}
  return archive;
 }
 [STAThread]static int Main(string[] args){
  try{
   if(args.Length==2&&args[0]=="--recover"){Recover(args[1]);return 0;}
   Application.EnableVisualStyles();
   if(MessageBox.Show("此工具会备份配置并停用 Mono 版 BepInEx 加载器，所有依赖它的插件也会暂停加载。不会删除卡组、存档或程序文件。继续后请选择游戏文件夹。", "决构编辑器 · 启动恢复",MessageBoxButtons.OKCancel,MessageBoxIcon.Information)!=DialogResult.OK)return 0;
   using(var dialog=new FolderBrowserDialog{Description="选择包含 MDPro3.exe 的游戏文件夹"}){
    if(dialog.ShowDialog()!=DialogResult.OK)return 0;
    string archive=Recover(dialog.SelectedPath);
    MessageBox.Show("已停用加载器，请重新启动游戏验证。原配置备份在游戏目录，可恢复。\r\n\r\n故障日志：\r\n"+archive+"\r\n\r\n日志可能包含用户名或目录，请检查后再提供给开发者。","启动恢复",MessageBoxButtons.OK,MessageBoxIcon.Information);
   }
   return 0;
  }catch(Exception error){if(args.Length==0)MessageBox.Show(error.Message,"未完成恢复",MessageBoxButtons.OK,MessageBoxIcon.Information);else Console.Error.WriteLine(error.Message);return 1;}
 }
}
