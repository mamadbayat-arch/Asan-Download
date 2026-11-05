using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using MehrDownload;
class InterfaceTests {
 static void Check(bool value,string description){if(!value)throw new Exception(description);Console.WriteLine("PASS: "+description);}
 static object Field(Window window,string name){return typeof(Window).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);}
 [STAThread] static void Main(){
  System.Net.ServicePointManager.DefaultConnectionLimit=32;Application.EnableVisualStyles();
  string root=Path.Combine(Environment.CurrentDirectory,"interface-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  string settings=Path.Combine(root,"session.json"), folder=Path.Combine(root,"downloads");Directory.CreateDirectory(folder);
  var saved=new Saved{Folder=folder,Url="http://localhost/old.zip",Target=Path.Combine(folder,"old.zip")};saved.Save(settings);
  Check(Saved.Load(settings).Folder==folder,"default folder survives restart");
  var calendarSample=new CompletedFile{Finished=new DateTime(2026,3,21,9,5,0),DurationSeconds=3661,AverageBytesPerSecond=1000000};
  Check(calendarSample.PersianDate()=="۱۴۰۵/۰۱/۰۱ ۰۹:۰۵","Persian calendar date handles the new-year boundary");Check(calendarSample.Duration()=="۰۱:۰۱:۰۱"&&calendarSample.Average(false)=="۱.۰۰ MB/s"&&calendarSample.Average(true)=="۸.۰۰ Mb/s","duration and average preserve hours and byte-to-bit conversion");Check(new CompletedFile().Duration()=="—"&&new CompletedFile().Average(false)=="—","legacy history never invents download metrics");
  Lang.English=true;Check(calendarSample.PersianDate()=="2026/03/21 09:05","English history uses a real Gregorian timestamp");Lang.English=false;
  File.WriteAllText(Path.Combine(folder,"new.zip"),"existing");
  Check(saved.TargetFor("http://localhost/new.zip")==Path.Combine(folder,"new (1).zip"),"duplicate names preserve existing downloads");
  Directory.CreateDirectory(saved.Target+".mehrparts");
  Check(saved.TargetFor(saved.Url)==saved.Target,"paused target reused for the same link");
  using(var window=new Window(true,settings)){
   window.Show();Application.DoEvents();
   Check(window.Text=="آسان دانلود" && window.Icon!=null,"new app name and icon applied");
   Check(window.Font.Name.Contains("Vazir")&&!window.Font.Bold,"regular Vazir font loaded from embedded resources");
   Check(!window.RightToLeftLayout,"native window mirroring disabled to prevent clipped sidebar and broken painting");
   Check(window.FormBorderStyle==FormBorderStyle.None,"custom modern title bar replaces native window buttons");
   var bar=(TitleBar)typeof(GlassForm).GetField("titleBar",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
   var close=(WindowActionButton)typeof(TitleBar).GetField("close",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bar);
   var maximize=(WindowActionButton)typeof(TitleBar).GetField("maximize",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bar);
   var minimize=(WindowActionButton)typeof(TitleBar).GetField("minimize",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(bar);
   Check(close.Left<maximize.Left && maximize.Left<minimize.Left && close.Left>=0,"close, maximize and minimize arranged at the left edge for right-to-left reading");
   var normalBounds=window.Bounds;maximize.PerformClick();Application.DoEvents();Check(window.WindowState==FormWindowState.Maximized && maximize.Restored,"maximize button switches to restore icon");maximize.PerformClick();Application.DoEvents();Check(window.WindowState==FormWindowState.Normal && window.Bounds==normalBounds,"restore returns original bounds");
   minimize.PerformClick();Application.DoEvents();Check(window.WindowState==FormWindowState.Minimized,"minimize button works");window.WindowState=FormWindowState.Normal;Application.DoEvents();
   var composer=(TableLayoutPanel)typeof(Window).GetField("composer",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
   Check(composer.RowCount==1,"the link bar keeps a single row, the redundant destination row is gone");
   var summary=(GlassButton)Field(window,"linkSummary");
   Check(!typeof(System.Windows.Forms.Button).IsAssignableFrom(summary.GetType()),"buttons use custom controls without native Windows button frames");
   var link=(TextBox)Field(window,"link");var target=(TextBox)Field(window,"destination");
   link.Text="https://example.com/file.zip?token=keep-the-entire-link";
   Check(!summary.Text.Contains("token=") && link.Text.EndsWith("token=keep-the-entire-link"),"compact link hides query without changing actual download URL");
   link.Text="https://example.com/new.zip";
   Check(target.Text==Path.Combine(folder,"new (1).zip"),"pasted new URL immediately updates filename");
   link.Text="http://127.0.0.1:18743/range";
   Check(target.Text==Path.Combine(folder,"range"),"same folder reused without a dialog");
   // FDM style structure
   var table=(DownloadTable)Field(window,"history");
   Check(table!=null && table.RowHeight>0,"downloads are rendered by the FDM styled table");
   var nav=typeof(Window).GetField("navItems",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window) as System.Collections.Generic.List<NavItem>;
   Check(nav!=null && nav.Count==4,"sidebar exposes the four download filters");
   Check(nav[0].Active,"the all-downloads filter is selected by default");
   var tickerType=typeof(Ticker);
   Check(tickerType.GetMethod("Add")!=null,"the shared animation ticker is available to every control");
   var toggle=(GlassButton)Field(window,"toggleSidebar");
   Check(toggle!=null,"the sidebar can be collapsed from the toolbar");
   Check(window.RightToLeft==RightToLeft.Yes,"Persian runs the window from right to left");
   var language=(NavItem)typeof(Window).GetField("languageItem",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
   Check(language!=null,"the language switcher lives in the sidebar");
   var summaryBefore=summary.Text;
   language.PerformClick();Application.DoEvents();
   Check(Lang.English,"the language switcher flips the interface to English");
   Check(window.RightToLeft==RightToLeft.No,"English runs the window from left to right");
   Check(Lang.T("action.add")=="Add download","the English strings are available");
   language.PerformClick();Application.DoEvents();
   Check(Lang.English==false,"the language switcher flips the interface back to Persian");
   Check(window.RightToLeft==RightToLeft.Yes,"Persian restores the right to left direction");
   Check(summary.Text==summaryBefore,"the link summary keeps its text after switching back");
   using(var prompt=new DownloadPrompt("https://example.com/sample.zip","sample.zip",false,false)){prompt.Show(window);Application.DoEvents();Check(prompt.Visible&&prompt.CancelButton!=null,"download confirmation shows before starting and allows cancellation");prompt.DialogResult=DialogResult.Cancel;prompt.Close();}
   var work=(Task)typeof(Window).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{true});
   while(!work.IsCompleted){Application.DoEvents();Thread.Sleep(10);}work.GetAwaiter().GetResult();
   Check(table.Items.Count==1 && File.Exists(Path.Combine(folder,"range")),"successful download appears in the completed list");
   Check(!((Label)Field(window,"emptyHint")).Visible,"empty state disappears after successful download");
   Check(table.Menu!=null && table.Menu.Items.Count==2,"file open and reveal-in-folder commands exist");
   Check(Saved.Load(settings).History.Count==1,"completed list saved on disk");
   var recorded=Saved.Load(settings).History[0];Check(recorded.DurationSeconds>0&&recorded.AverageBytesPerSecond>0,"actual download duration and average speed persist on disk");
   var row=table.Items[0];
   Check(row.Title=="range"&&row.Kind=="completed"&&row.DateText.StartsWith("۱۴")&&row.SpeedText.EndsWith("MB/s")&&row.StatusText.Contains("مدت"),"table row shows the Persian date, duration and average speed");
   Check(table.EmptyMessage.Length==0,"the table stops showing the empty state once a file exists");
   using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(0,0,window.Width,window.Height));image.Save(Path.Combine(Environment.CurrentDirectory,"preview.png"));}
   typeof(Window).GetField("dark",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,true);
   typeof(Window).GetMethod("PaintTheme",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,new object[]{window});
   Application.DoEvents();
   using(var image=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(image,new Rectangle(0,0,window.Width,window.Height));image.Save(Path.Combine(Environment.CurrentDirectory,"preview-dark.png"));}
   close.PerformClick();Application.DoEvents();Check(window.IsDisposed,"close button closes the window");
  }
  using(var restored=new Window(true,settings))Check(((DownloadTable)Field(restored,"history")).Items.Count==1,"completed list restored after restart");
  // batch processing
  string batchSettings=Path.Combine(root,"batch.json");var batchSaved=new Saved{Folder=folder};batchSaved.Save(batchSettings);
  using(var batchWindow=new Window(true,batchSettings)){
   batchWindow.Show();Application.DoEvents();
   var parse=(MethodInfo)typeof(Window).GetMethod("ParseLinks",BindingFlags.Instance|BindingFlags.NonPublic);
   var parsed=(System.Collections.Generic.List<string>)parse.Invoke(batchWindow,new object[]{"http://127.0.0.1:18743/range\nhttp://127.0.0.1:18743/plain\nnot-a-link\nhttp://127.0.0.1:18743/range"});
   Check(parsed.Count==2,"the batch parser keeps every valid link once and drops the broken ones");
   var add=(MethodInfo)typeof(Window).GetMethod("AddBatch",BindingFlags.Instance|BindingFlags.NonPublic);
   int added=(int)add.Invoke(batchWindow,new object[]{"http://127.0.0.1:18743/range\nhttp://127.0.0.1:18743/plain"});
   Check(added==2,"a batch adds every link to the download queue");
   var batchTable=(DownloadTable)Field(batchWindow,"history");
   bool queued=false;foreach(var item in batchTable.Items)if(item.Kind=="queued")queued=true;
   Check(queued,"queued batch links show up in the download table");
   Check(Saved.Load(batchSettings).Queue.Count==1&&Saved.Load(batchSettings).Queue[0].EndsWith("/plain"),"remaining batch link is persisted before the first download starts");
   var batchState=(Saved)Field(batchWindow,"saved");var batchEngine=(DownloadEngine)Field(batchWindow,"engine");
   DateTime until=DateTime.UtcNow.AddSeconds(20);
   while(DateTime.UtcNow<until&&(batchState.History.Count<2||batchEngine.Running||batchState.Queue.Count>0)){Application.DoEvents();Thread.Sleep(10);}
   Check(batchState.History.Count==2&&File.Exists(batchState.History[0].Path)&&File.Exists(batchState.History[1].Path),"both batch downloads finish and create files");
   Check(Path.GetFileName(batchState.History[0].Path)=="plain"&&Path.GetFileName(batchState.History[1].Path).StartsWith("range"),"batch downloads complete in queue order");
  }
  var completedBatch=Saved.Load(batchSettings);Check(completedBatch.Queue.Count==0&&completedBatch.History.Count==2,"completed batch queue and both history records survive restart");
  string failedSettings=Path.Combine(root,"failed-batch.json");new Saved{Folder=folder,Queue=new System.Collections.Generic.List<string>{"http://127.0.0.1:18743/plain"}}.Save(failedSettings);
  using(var failedWindow=new Window(true,failedSettings)){
   failedWindow.Show();Application.DoEvents();
   var failedLink=(TextBox)Field(failedWindow,"link");failedLink.Text="http://127.0.0.1:18743/missing";
   var failedWork=(Task)typeof(Window).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(failedWindow,new object[]{true});
   DateTime limit=DateTime.UtcNow.AddSeconds(15);while(!failedWork.IsCompleted&&DateTime.UtcNow<limit){Application.DoEvents();Thread.Sleep(10);}Check(failedWork.IsCompleted,"failed batch download returns control without a blocking native dialog");failedWork.GetAwaiter().GetResult();
   Check(!String.IsNullOrEmpty(((MehrDownload.StatusStrip)Field(failedWindow,"statusStrip")).Hint)&&!String.IsNullOrEmpty((string)Field(failedWindow,"downloadError")),"download error remains visible in the status strip for retry");
   var failedState=Saved.Load(failedSettings);Check(failedState.Queue.Count==1&&failedState.Queue[0].EndsWith("/plain")&&failedState.History.Count==0,"a failed download leaves later queued links untouched");
   Check(failedState.Url.EndsWith("/missing")&&failedLink.Text.EndsWith("/missing"),"a failed download retains its retry URL and destination");
  }
  string legacy=Path.Combine(root,"legacy.json");File.WriteAllText(legacy,"{\"Url\":\"https://example.com/file.zip\",\"Target\":"+new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(Path.Combine(folder,"file.zip"))+",\"Connections\":8}");
  Check(Saved.Load(legacy).Folder==folder,"previous version folder migrated");
  string durable=Path.Combine(root,"durable.json");var first=new Saved{Folder=folder};first.Save(durable);var stale=Saved.Load(durable);
  first.History.Add(new CompletedFile{Path=Path.Combine(folder,"removed.zip"),Size=123,Finished=DateTime.Now,DurationSeconds=10,AverageBytesPerSecond=12.3});first.Save(durable);
  stale.History.Add(new CompletedFile{Path=Path.Combine(folder,"new-download.zip"),Size=456,Finished=DateTime.Now.AddSeconds(1)});stale.Save(durable);
  Check(Saved.Load(durable).History.Count==2,"stale settings writer cannot erase earlier completed downloads");
  first.Save(durable);first.Save(durable);Check(Saved.Load(durable).History.Count==2,"repeated saves do not duplicate history and retain moved or deleted files");
  first.Complete(Path.Combine(folder,"new.zip"));first.Save(durable);first.Complete(Path.Combine(folder,"new.zip"));first.Save(durable);Check(Saved.Load(durable).History.Count==4,"downloading to the same path again preserves both completed events");
  File.WriteAllText(durable,"broken JSON");var recovered=Saved.Load(durable);Check(recovered.History.Count==4&&recovered.Folder==folder,"damaged settings recover from backup and independent history records");
  File.WriteAllText(durable+".bak","broken backup");Check(Saved.Load(durable).History.Count==4,"history survives loss of both settings copies");recovered.Save(durable);
  Check(Directory.GetFiles(root,"durable.json.corrupt-*").Length==1,"malformed settings preserved as recovery evidence");
  var restoredRecords=Saved.Load(durable).History;Check(restoredRecords.Exists(f=>f.DurationSeconds==10&&f.AverageBytesPerSecond==12.3),"history recovery retains recorded duration and average speed");
  for(int n=0;n<3;n++)using(var reopened=new Window(true,durable))Check(((DownloadTable)Field(reopened,"history")).Items.Count==4,"history survives closing and reopening the app repeatedly");
  string large=Path.Combine(root,"large.json");var lots=new Saved();for(int n=0;n<2200;n++)lots.History.Add(new CompletedFile{Id=n.ToString(),Path=new string('a',1000)+n.ToString(),Finished=DateTime.Now});
  File.WriteAllText(large,new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=Int32.MaxValue}.Serialize(lots));Check(Saved.Load(large).History.Count==2200,"large history exceeds the old two-megabyte serializer limit without disappearing");
  Console.WriteLine("INTERFACE TESTS PASSED");
 }
}
