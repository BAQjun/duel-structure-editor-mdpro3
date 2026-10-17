using System;using System.IO;using System.Linq;using System.Reflection;using System.Windows.Forms;
class InstallerPathSmoke {
 [STAThread]static int Main(string[] args){try{var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));var type=assembly.GetType("DuelStructureSetup.SetupForm",true);using(var form=(Form)Activator.CreateInstance(type,true)){var boxes=form.Controls.OfType<TextBox>().Where(b=>!b.Multiline).ToArray();if(boxes.Length!=1||boxes[0].Text!="")throw new Exception("Standalone installer must not preset a developer path");Console.WriteLine("PASS Standalone installer has no developer-specific default directory");}return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
