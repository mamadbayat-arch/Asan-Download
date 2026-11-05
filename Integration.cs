using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Microsoft.Win32;
namespace MehrDownload {
 static class Integration {
  public const string PipeName="AsanDownload.Desktop.v1";
  public static bool ValidUrl(string text){Uri uri;return text!=null&&text.Length<=8192&&Uri.TryCreate(text,UriKind.Absolute,out uri)&&(uri.Scheme=="http"||uri.Scheme=="https")&&String.IsNullOrEmpty(uri.UserInfo);}
  public static bool StartupEnabled {get{using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))return key!=null&&key.GetValue("AsanDownload")!=null;}set{using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")){if(value)key.SetValue("AsanDownload","\""+ReleaseInfo.InstalledPath+"\" --startup");else key.DeleteValue("AsanDownload",false);}}}
  public static bool Deliver(string url,int timeout=1500){try{using(var pipe=new NamedPipeClientStream(".",PipeName,PipeDirection.InOut)){pipe.Connect(timeout);using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true)){writer.AutoFlush=true;writer.WriteLine(url);}using(var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true)){var answer=reader.ReadLineAsync();return answer.Wait(timeout)&&answer.Result=="accepted";}}}catch{return false;}}
  public static void Listen(Window window){Task.Run(()=>{while(!window.IsDisposed){try{using(var pipe=new NamedPipeServerStream(PipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous)){pipe.WaitForConnection();using(var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true))using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),1024,true)){var read=reader.ReadLineAsync();if(!read.Wait(3000))continue;string url=read.Result;bool accepted=false;if(!window.IsDisposed)window.Invoke(new Action(()=>{if(url=="ping"){accepted=true;}else if(url=="activate"){window.Show();if(window.WindowState==FormWindowState.Minimized)window.WindowState=FormWindowState.Normal;window.Activate();accepted=true;}else if(ValidUrl(url))accepted=window.ReceiveDownload(url);}));writer.WriteLine(accepted?"accepted":window.LastDownloadDeclined?"cancelled":"rejected");writer.Flush();}}}catch{if(window.IsDisposed)return;System.Threading.Thread.Sleep(250);}}});}
  public static void RegisterBrowser(){
   string dir=Path.GetDirectoryName(ReleaseInfo.InstalledPath);var asm=System.Reflection.Assembly.GetExecutingAssembly();
   using(var input=asm.GetManifestResourceStream("NativeHost.exe"))using(var output=File.Create(Path.Combine(dir,"NativeHost.exe")))input.CopyTo(output);
   string extension=Path.Combine(dir,"BrowserExtension");Directory.CreateDirectory(extension);
   foreach(string name in new[]{"manifest.json","background.js","intent.js","popup.html","popup.js","icon128.png"})using(var input=asm.GetManifestResourceStream("Browser."+name))using(var output=File.Create(Path.Combine(extension,name)))input.CopyTo(output);
   foreach(string name in new[]{"Vazir-Regular.ttf","Vazir-OFL.txt"})using(var input=asm.GetManifestResourceStream(name))using(var output=File.Create(Path.Combine(extension,name)))input.CopyTo(output);
   var manifest=new JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string,object>>(File.ReadAllText(Path.Combine(extension,"manifest.json")));
   string key=(string)manifest["key"];byte[] hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=sha.ComputeHash(Convert.FromBase64String(key));string id="";for(int n=0;n<16;n++)id+=(char)('a'+(hash[n]>>4))+""+(char)('a'+(hash[n]&15));
   string host=Path.Combine(dir,"native-host.json");File.WriteAllText(host,new JavaScriptSerializer().Serialize(new{name="ir.hamtavar.asandownload",description="Asan Download browser bridge",path=Path.Combine(dir,"NativeHost.exe"),type="stdio",allowed_origins=new[]{"chrome-extension://"+id+"/"}}));
   foreach(string browser in new[]{@"Software\Google\Chrome",@"Software\Microsoft\Edge"})using(var registry=Registry.CurrentUser.CreateSubKey(browser+@"\NativeMessagingHosts\ir.hamtavar.asandownload"))registry.SetValue("",host);
  }
 }
}
