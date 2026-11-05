using System;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace MehrDownload {
 static class ReleaseInfo {
  public static readonly string Version=typeof(ReleaseInfo).Assembly.GetName().Version.ToString(3);
  public static string InstalledPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MehrDownload","MehrDownload.exe");}}
  public static string CachePath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MehrDownload","updates");}}
 }
 class ReleaseManifest {
  public string version,downloadUrl,updateUrl,sha256,signature;
  public bool available;
  public long size,exeSize;
  public string Payload {get{return version+"\n"+sha256+"\n"+exeSize.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n"+updateUrl;}}
 }
 class UpdateClient {
  readonly string publicKey;
  public UpdateClient():this(ReadKey()){}
  internal UpdateClient(string key){publicKey=key;}
  static string ReadKey(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("UpdatePublicKey.xml"))using(var reader=new StreamReader(stream))return reader.ReadToEnd();}
  internal bool Verify(ReleaseManifest release){
   Version version;
   if(release==null||!release.available||!Version.TryParse(release.version,out version)||release.exeSize<1024||release.exeSize>20*1024*1024||!System.Text.RegularExpressions.Regex.IsMatch(release.sha256??"","^[a-f0-9]{64}$")||release.updateUrl!="/downloads/AsanDownload-Update.exe"||String.IsNullOrEmpty(release.signature)||release.signature.Length>1024)return false;
   try{using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(publicKey);return rsa.VerifyData(Encoding.UTF8.GetBytes(release.Payload),CryptoConfig.MapNameToOID("SHA256"),Convert.FromBase64String(release.signature));}}catch{return false;}
  }
  internal static bool IsNewer(string candidate,string current){Version next,previous;return Version.TryParse(candidate,out next)&&Version.TryParse(current,out previous)&&next>previous;}
  internal ReleaseManifest Check(Uri origin,CancellationToken cancellation){
   var bytes=Get(origin,"/api/public/release",32768,1800,cancellation,null);
   var release=new JavaScriptSerializer().Deserialize<ReleaseManifest>(Encoding.UTF8.GetString(bytes));
   if(!Verify(release))throw new InvalidDataException("Invalid signed update manifest");return IsNewer(release.version,ReleaseInfo.Version)?release:null;
  }
  static byte[] Get(Uri origin,string relative,int maximum,int timeout,CancellationToken cancellation,Action<long,long> report){
   var request=DownloadEngine.Request(new Uri(origin,relative).AbsoluteUri);request.AllowAutoRedirect=false;request.Timeout=request.ReadWriteTimeout=timeout;
   using(cancellation.Register(()=>request.Abort()))using(var response=(HttpWebResponse)request.GetResponse())using(var input=response.GetResponseStream())using(var output=new MemoryStream()){
    if(response.StatusCode!=HttpStatusCode.OK||response.ContentLength>maximum)throw new InvalidDataException("Invalid update response");
    byte[] buffer=new byte[32768];int read;while((read=input.Read(buffer,0,buffer.Length))>0){cancellation.ThrowIfCancellationRequested();if(output.Length+read>maximum)throw new InvalidDataException("Update exceeds allowed size");output.Write(buffer,0,read);if(report!=null)report(output.Length,response.ContentLength);}cancellation.ThrowIfCancellationRequested();return output.ToArray();
   }
  }
  internal string Download(Uri origin,ReleaseManifest release,string cache,CancellationToken cancellation,Action<long,long> report){
   if(!Verify(release))throw new InvalidDataException("Invalid update signature");
   byte[] bytes=Get(origin,release.updateUrl,(int)release.exeSize,15000,cancellation,report);
   if(bytes.LongLength!=release.exeSize||Hash(bytes)!=release.sha256||bytes[0]!='M'||bytes[1]!='Z')throw new InvalidDataException("Update integrity check failed");
   cancellation.ThrowIfCancellationRequested();Directory.CreateDirectory(cache);string path=Path.Combine(cache,Guid.NewGuid().ToString("N")+".exe");File.WriteAllBytes(path,bytes);return path;
  }
  internal static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  internal bool CheckFile(string file,ReleaseManifest release){return Verify(release)&&new FileInfo(file).Length==release.exeSize&&Hash(File.ReadAllBytes(file))==release.sha256;}
  internal static void ReplaceFile(string source,string target){
   Directory.CreateDirectory(Path.GetDirectoryName(target));string temporary=target+".update-"+Guid.NewGuid().ToString("N"),backup=target+".previous";
   try{File.Copy(source,temporary,false);if(File.Exists(target)){if(File.Exists(backup))File.Delete(backup);File.Replace(temporary,target,backup);}else File.Move(temporary,target);}finally{if(File.Exists(temporary))File.Delete(temporary);}
  }
  internal static void Rollback(string target){string backup=target+".previous";if(File.Exists(backup)){string failed=target+".failed-"+Guid.NewGuid().ToString("N");if(File.Exists(target)){File.Replace(backup,target,failed);File.Delete(failed);}else File.Move(backup,target);}}
  internal static void Apply(string[] args){
   string own=Path.GetFullPath(Application.ExecutablePath),cache=Path.GetFullPath(ReleaseInfo.CachePath)+Path.DirectorySeparatorChar;
   if(args.Length!=7||!own.StartsWith(cache,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid updater location");
   int parent;long size;if(!Int32.TryParse(args[1],out parent)||parent<=0||!Int64.TryParse(args[4],out size))throw new InvalidDataException("Invalid updater arguments");
   var release=new ReleaseManifest{available=true,version=args[2],sha256=args[3],exeSize=size,signature=args[5],updateUrl=args[6]};
   var client=new UpdateClient();if(release.version!=ReleaseInfo.Version||!client.CheckFile(own,release))throw new InvalidDataException("Invalid updater binary");
   try{using(var process=Process.GetProcessById(parent)){if(!process.WaitForExit(20000))throw new IOException("Previous application still running");}}catch(ArgumentException){}
   string target=ReleaseInfo.InstalledPath;bool replaced=false;
   try{ReplaceFile(own,target);replaced=true;Program.Install(false);Process.Start(new ProcessStartInfo(target,"--skip-update"){UseShellExecute=true});}
   catch{if(replaced)Rollback(target);throw;}
  }
 }
 class StartupUpdate : GlassForm {
  readonly Label message=new Label();readonly GlassProgress progress=new GlassProgress();readonly GlassButton proceed=new GlassButton();
  readonly CancellationTokenSource cancellation=new CancellationTokenSource();bool finished,proceedRequested;
  public bool Relaunching,ExitRequested;
  readonly string[] origins;
  public StartupUpdate():this(new[]{AdPlacement.Origin,AdPlacement.LegacyOrigin}){}
  internal StartupUpdate(string[] addresses){
   origins=addresses;
   Text=Lang.T("update.title");ClientSize=new Size(500,300);MinimumSize=MaximumSize=Size;StartPosition=FormStartPosition.CenterScreen;Font=Brand.Font(11);RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;
   var body=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(28,16,28,22),ColumnCount=1,RowCount=5};
   body.RowStyles.Add(new RowStyle(SizeType.Absolute,58));body.RowStyles.Add(new RowStyle(SizeType.Absolute,44));body.RowStyles.Add(new RowStyle(SizeType.Absolute,12));body.RowStyles.Add(new RowStyle(SizeType.Absolute,20));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   Controls.Add(body);
   var head=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
   head.RowStyles.Add(new RowStyle(SizeType.Percent,100));head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,52));head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   head.Controls.Add(new LogoView{Dock=DockStyle.Fill,Margin=new Padding(0,0,10,0)},0,0);
   var heading=new Label{Text=Lang.T("update.heading"),Font=Brand.Font(16,FontStyle.Regular),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,AutoSize=false};
   head.Controls.Add(heading,1,0);
   body.Controls.Add(head,0,0);
   message.Text=Lang.T("update.checking");message.Font=Brand.Font(10.5f);message.Dock=DockStyle.Fill;message.TextAlign=ContentAlignment.MiddleLeft;message.BackColor=Color.Transparent;message.AutoSize=false;body.Controls.Add(message,0,1);
   var bar=new GlassPanel{Dock=DockStyle.Fill,Radius=12,Padding=new Padding(10),Margin=Padding.Empty};
   progress.Dock=DockStyle.Fill;progress.Maximum=1000;bar.Controls.Add(progress);body.Controls.Add(bar,0,3);
   proceed.Text=Lang.T("update.enter");proceed.Font=Brand.Font(11);proceed.Dock=DockStyle.Right;proceed.Width=150;body.Controls.Add(proceed,0,4);proceed.Click+=(s,e)=>{proceedRequested=true;cancellation.Cancel();Close();};
   Shown+=async(s,e)=>await Run();FormClosing+=(s,e)=>{if(!finished){ExitRequested=!proceedRequested;cancellation.Cancel();}};
  }
  async Task Run(){
   string file=null;
   try{
    var client=new UpdateClient();ReleaseManifest release=null;Uri selected=null;
    cancellation.CancelAfter(5000);
    foreach(string address in origins){
     cancellation.Token.ThrowIfCancellationRequested();try{var origin=new Uri(address);release=await Task.Run(()=>client.Check(origin,cancellation.Token));selected=origin;break;}catch{if(cancellation.IsCancellationRequested)throw;}
    }
    if(release==null)return;
    if(IsDisposed)return;message.Text=Lang.T("update.downloading")+release.version+" "+Lang.T("update.receiving");cancellation.CancelAfter(30000);
    var report=new Progress<int>(value=>{if(!IsDisposed)progress.Value=Math.Max(0,Math.Min(1000,value));});
    file=await Task.Run(()=>client.Download(selected,release,ReleaseInfo.CachePath,cancellation.Token,(done,total)=>((IProgress<int>)report).Report((int)Math.Min(1000,done*1000/release.exeSize))));
    cancellation.Token.ThrowIfCancellationRequested();if(IsDisposed)return;
    message.Text=Lang.T("update.installing");proceed.Enabled=false;
    // Keep a read lock until the trusted helper has started, preventing replacement of the verified file.
    using(var held=File.Open(file,FileMode.Open,FileAccess.Read,FileShare.Read)){
     string arguments="--apply-update "+Process.GetCurrentProcess().Id+" "+release.version+" "+release.sha256+" "+release.exeSize+" "+release.signature+" "+release.updateUrl;
     Process.Start(new ProcessStartInfo(file,arguments){UseShellExecute=false,CreateNoWindow=true});Relaunching=true;
    }
   }catch{ /* Offline, rejected, cancelled or failed updates always leave the installed version usable. */ }
   finally{if(!Relaunching&&file!=null)try{File.Delete(file);}catch{}if(!IsDisposed){finished=true;Close();}}
  }
 }
}
