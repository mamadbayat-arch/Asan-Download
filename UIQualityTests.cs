using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using MehrDownload;

class UIQualityTests {
 static int failures,checks;
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 [System.Runtime.InteropServices.DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
 static void SizeForCapture(Form window,Size size){
  // Form.Size clamps to the runner's physical desktop. Native positioning gives
  // the bitmap fixture its real requested dimensions without changing the host display.
  if(!SetWindowPos(window.Handle,IntPtr.Zero,0,0,size.Width,size.Height,0x0016))throw new System.ComponentModel.Win32Exception();
 }
 static object Field(object owner,string name){return owner.GetType().GetField(name,flags).GetValue(owner);}
 static void Call(object owner,string name,params object[] args){owner.GetType().GetMethod(name,flags).Invoke(owner,args);}
 static void Check(bool value,string text){checks++;Console.WriteLine((value?"PASS: ":"FAIL: ")+text);if(!value)failures++;}
 static void Pump(int ms){var until=DateTime.UtcNow.AddMilliseconds(ms);while(DateTime.UtcNow<until){Application.DoEvents();Thread.Sleep(10);}}
 static IEnumerable<Control> Tree(Control root){foreach(Control child in root.Controls){yield return child;foreach(var descendant in Tree(child))yield return descendant;}}
 static bool TreeHasVeil(Control root){foreach(var child in Tree(root))if(child.GetType().Name=="Veil")return true;return false;}
 static void Capture(Form window,string path){Pump(700);using(var bitmap=new Bitmap(window.Width,window.Height)){window.DrawToBitmap(bitmap,window.ClientRectangle);bitmap.Save(path);}}
 [STAThread] static int Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  Check(ReleaseInfo.Version==typeof(Window).Assembly.GetName().Version.ToString(3),"reported/update version matches the built application");
  if(args.Length>0&&args[0]=="dialogs"){ReportDialogs(Path.Combine(Environment.CurrentDirectory,"qa-artifacts","after"));return failures==0?0:1;}
  string root=Path.Combine(Environment.CurrentDirectory,"ui-quality-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  string pictures=Path.Combine(Environment.CurrentDirectory,"qa-artifacts",args.Length>0?args[0]:"after");Directory.CreateDirectory(pictures);
  string settings=Path.Combine(root,"session.json");new Saved{English=true,Folder=root}.Save(settings);
  Lang.English=false;
  using(var w=new Window(true,settings)){
   w.Show();Pump(650);
   var stableInput=(TextBox)Field(w,"link");var inputHandle=stableInput.Handle;
   for(int resize=0;resize<12;resize++){SizeForCapture(w,resize%2==0?new Size(960,640):new Size(1280,940));Application.DoEvents();}
   Check(stableInput.Handle==inputHandle,"responsive breakpoints preserve the native URL input instead of reparenting controls");
   Call(w,"ToggleLanguage");Call(w,"ToggleLanguage");
   bool settled=true;foreach(var control in Tree(w)){var animated=control as AnimatedControl;if(animated!=null&&animated.EnterAmount<0.999)settled=false;}
   Check(settled,"language switches do not replay staggered entrance animations");
   Check(!TreeHasVeil(w),"language and resize changes never cover the interface with a transition layer");
   Check(Lang.English&&w.RightToLeft==RightToLeft.No&&((GlassButton)Field(w,"addButton")).Text=="Add download","saved English is applied before the first frame");
   Check(((DropZone)Field(w,"dropZone")).Visible&&((DropZone)Field(w,"dropZone")).Height>=60,"download drop zone is visible on the first frame before any resize");
   if(!Lang.English)Call(w,"ToggleLanguage");
   // Reapply after a deliberately inconsistent saved-language startup in the baseline.
   Call(w,"ToggleLanguage");if(!Lang.English)Call(w,"ToggleLanguage");Pump(300);
   var search=(SearchField)Field(w,"searchField");
   Check(search.Padding.Left>search.Padding.Right,"English search padding follows its icon");
   var sort=(GlassButton)Field(w,"sortButton");
   Check(sort.Menu.Items[0].Text==Lang.T("sort.newest"),"sort menu follows the active language");
   var placement=(AdPlacement)Field(w,"ad");var advertisement=(Advertisement)Field(placement,"ad");
   advertisement.title="تبلیغ آزمایشی";advertisement.description="توضیح آزمایشی";advertisement.ctaText="مشاهده";placement.ApplyLanguage();
   Check(((Label)Field(placement,"heading")).Text==advertisement.title&&((GlassButton)Field(placement,"action")).Text==advertisement.ctaText,"custom Persian ad copy is preserved when English translation is missing");
   advertisement.titleEn="Translated ad";placement.ApplyLanguage();Check(((Label)Field(placement,"heading")).Text=="Translated ad","explicit English advertisement copy takes precedence");
   typeof(AdPlacement).GetField("ad",flags).SetValue(placement,new Advertisement());placement.ApplyLanguage();
   var chrome=(TitleBar)typeof(GlassForm).GetField("titleBar",flags).GetValue(w);var close=(WindowActionButton)Field(chrome,"close");
   Check(close.Left>w.Width/2&&close.AccessibleName==Lang.T("win.close"),"title buttons move and localize after a language switch");
   using(var prompt=new DownloadPrompt("https://example.com/file.zip","file.zip",false,false)){
    prompt.Show(w);Pump(50);GlassButton accept=null;foreach(var c in Tree(prompt))if(c is GlassButton&&((GlassButton)c).Primary)accept=(GlassButton)c;
    var actions=accept.Parent;Rectangle actionBounds=Rectangle.Empty;foreach(Control button in actions.Controls)actionBounds=actionBounds.IsEmpty?button.Bounds:Rectangle.Union(actionBounds,button.Bounds);
    Check(Math.Abs(actionBounds.Left+actionBounds.Right-actions.ClientSize.Width)<=2&&Math.Abs(actionBounds.Top+actionBounds.Bottom-actions.ClientSize.Height)<=2,"confirmation actions are centered horizontally and vertically");
    accept.PerformClick();Check(prompt.DialogResult==DialogResult.Yes,"download confirmation accepts with its primary button");prompt.Close();
   }
   using(var prompt=new DownloadPrompt("https://example.com/file.zip","file.zip",false,false)){
    prompt.Show(w);Pump(50);((IButtonControl)prompt.CancelButton).PerformClick();Check(prompt.DialogResult==DialogResult.Cancel,"download confirmation cancels with its cancel button");prompt.Close();
   }
   var stepper=(ConnectionStepper)Field(w,"connections");int oldValue=stepper.Value;Call(stepper,"OnKeyDown",new KeyEventArgs(Keys.Up));Check(stepper.Value==oldValue+1,"connection count supports keyboard arrows");stepper.SetValue(oldValue);
   var language=(NavItem)Field(w,"languageItem");Check(language.TabStop&&language.AccessibleName==language.Caption,"sidebar actions have keyboard access and accessible names");
   bool wasEnglish=Lang.English;Call(language,"OnKeyUp",new KeyEventArgs(Keys.Space));Check(Lang.English!=wasEnglish,"sidebar language switch activates from the keyboard");Call(w,"ToggleLanguage");
   Call(w,"SetRunning",true);var pause=(GlassButton)Field(w,"pause");var start=(GlassButton)Field(w,"start");Check(pause.Visible&&pause.Enabled&&!start.Visible,"pause replaces start instead of being hidden behind it");Call(w,"SetRunning",false);
   using(var batch=new BatchDialog(true)){
    batch.Show(w);Pump(50);TextBox area=null;foreach(var c in Tree(batch))if(c is TextBox)area=(TextBox)c;area.Text="https://example.com/one.zip\nhttps://example.com/two.zip";batch.AcceptButton.PerformClick();Check(batch.Links==area.Text&&batch.DialogResult==DialogResult.OK,"batch dialog returns all typed links");Check(batch.GlassDark,"batch dialog inherits the selected theme");batch.Close();
   }
   Call(w,"ToggleTheme");Call(w,"ToggleTheme");Pump(600);Check(!(bool)Field(w,"dark"),"two rapid theme toggles return to the original theme");
   Call(w,"ToggleTheme");Pump(600);Check(stepper.Dark==(bool)Field(w,"dark"),"connection stepper uses the current theme");
   var tile=(StatTile)Field(w,"count");tile.AnimateTo(9);Pump(900);Check(Math.Abs(tile.Shown-9)<0.05,"statistic count-up reaches its target");
   Call(w,"ToggleSidebar");Call(w,"ToggleSidebar");Call(w,"ToggleSidebar");Pump(600);
   var shell=(TableLayoutPanel)Field(w,"shellLayout");Check(Math.Abs(shell.ColumnStyles[0].Width-66)<1,"rapid sidebar toggles settle at the requested width");
   Check(language.Width<=66,"collapsed navigation paints inside the narrow sidebar");
   Capture(w,Path.Combine(pictures,"en-collapsed.png"));
   Call(w,"ToggleSidebar");Pump(400);
   var state=(Saved)Field(w,"saved");
   for(int i=0;i<8;i++)state.History.Add(new CompletedFile{Id=i.ToString(),Path=Path.Combine(root,i==0?"گزارش جلسه و برنامه‌ریزی پروژه — نسخه نهایی ۱۴۰۵.zip":"Sample-download-"+i+".zip"),Size=(i+1)*1048576L,Finished=new DateTime(2026,10,22,10,i,0),DurationSeconds=61+i,AverageBytesPerSecond=124000});
   Call(w,"ReloadHistory");
   foreach(bool english in new[]{false,true}){
    if(Lang.English!=english)Call(w,"ToggleLanguage");
    foreach(bool dark in new[]{false,true}){
     if((bool)Field(w,"dark")!=dark)Call(w,"ToggleTheme");Pump(400);
     foreach(var size in new[]{new Size(1280,940),new Size(1060,760),new Size(960,640)}){
      SizeForCapture(w,size);Pump(200);
      string label=(english?"en":"fa")+"-"+(dark?"dark":"light")+"-"+size.Width;
      var link=(TextBox)Field(w,"link");var toggle=(GlassButton)Field(w,"toggleSidebar");
      Check(link.Width>=160&&search.Input.Width>=100,label+": URL and search fields stay usable");
      Rectangle toggleBounds=w.RectangleToClient(toggle.RectangleToScreen(toggle.ClientRectangle));
      var sponsor=(Control)Field(w,"ad");var sponsorBounds=w.RectangleToClient(sponsor.RectangleToScreen(sponsor.ClientRectangle));
      Check(w.ClientRectangle.Contains(toggleBounds)&&toggleBounds.Bottom<sponsorBounds.Top,label+": sidebar toggle stays above the sponsor strip");
      Check(w.Size==size,label+": requested window size is supported (actual "+w.Width+"x"+w.Height+")");
      Check(((Control)Field(w,"history")).Height>=100,label+": downloads retain usable vertical space");
      Check(!((Panel)Field(w,"sidebarScroll")).HorizontalScroll.Visible,label+": sidebar scrolls only vertically");
      Capture(w,Path.Combine(pictures,label+".png"));
     }
    }
   }
   Call(w,"ToggleLanguage");Call(w,"ToggleLanguage"); // preferences survive a real save
   if(!(bool)Field(w,"dark"))Call(w,"ToggleTheme");
   if(!(bool)Field(w,"sidebarCollapsed"))Call(w,"ToggleSidebar");Pump(300);
  }
  Lang.English=false;using(var reopened=new Window(true,settings)){reopened.Show();Pump(200);Check(Lang.English&&reopened.GlassDark&&(bool)Field(reopened,"sidebarCollapsed"),"language, theme and sidebar preference survive reopening");}
  Lang.English=true;
  using(var dialog=new BatchDialog(true)){dialog.Show();Capture(dialog,Path.Combine(pictures,"batch-en-dark.png"));dialog.Close();}
  using(var dialog=new DownloadPrompt("https://example.com/file.zip","A very long filename — گزارش نهایی پروژه.zip",false,true)){dialog.Show();Capture(dialog,Path.Combine(pictures,"prompt-en-dark.png"));dialog.Close();}
  ReportDialogs(pictures);
  Lang.English=true;
  var items=new List<DownloadItem>{new DownloadItem{Title="Zebra",Order=1,Bytes=10},new DownloadItem{Title="Alpha",Order=2,Bytes=20}};
  using(var table=new DownloadTable()){table.SetItems(items);table.SortKey="title";table.SortDescending=false;table.ApplySort();Check(table.Items[0].Title=="Alpha","name sort is alphabetical instead of sorting dates");table.SortKey="size";table.SortDescending=true;table.ApplySort();Check(table.Items[0].Bytes==20,"size sort compares byte counts");}
  using(var hostForm=new GlassForm{ClientSize=new Size(900,500)})using(var table=new DownloadTable{Dock=DockStyle.Fill}){
   hostForm.Controls.Add(table);var many=new List<DownloadItem>();for(int i=0;i<200;i++)many.Add(new DownloadItem{Title="Item "+i,Kind="completed"});table.SetItems(many);hostForm.Show();Pump(100);
   var scroll=(Panel)Field(table,"host");scroll.AutoScrollPosition=new Point(0,58*120);table.LayoutRows();Pump(100);
   DownloadRowView first=null;foreach(Control c in scroll.Controls)if(c.Visible&&c.Top>=0&&c.Top<58)first=(DownloadRowView)c;
   Check(first!=null&&first.Item.Title=="Item 120","virtualized table renders the correct rows after a deep scroll");
   Check(scroll.Controls.Count<15,"large history only allocates visible rows");
   if(first!=null){int opened=0,menus=0;first.ItemActivated+=(item)=>opened++;first.MenuRequested+=(item)=>menus++;Call(first,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,100,20,0));Check(opened==0,"a single row click never accidentally opens a file");Call(first,"OnMouseDoubleClick",new MouseEventArgs(MouseButtons.Left,2,100,20,0));Check(opened==1,"double click opens the selected row");Call(first,"OnMouseDown",new MouseEventArgs(MouseButtons.Right,1,100,20,0));Check(menus==1,"right click opens row commands");}
   hostForm.Close();
  }
  using(var row=new DownloadRowView()){row.Item=new DownloadItem{Kind="active",Indeterminate=true};Pump(600);Check(!((IAnim)row).Finished,"indeterminate rows continue animating while active");row.Item=new DownloadItem{Kind="completed",Progress=1};Pump(700);Check(((IAnim)row).Finished,"completed rows stop their animation ticker");}
  using(var toast=new Toast()){toast.Show("Finished",false);Pump(3900);Check(toast.IsDisposed,"expired toasts release their control and ticker");}
  Pump(600);Check(Ticker.Count==0,"closing all windows releases every animation subscription");
  Lang.English=false;
  Console.WriteLine("UI QUALITY: "+checks+" checks, "+failures+" failures. Screenshots: "+pictures);return failures==0?0:1;
 }
 static void ReportDialogs(string pictures){
  foreach(bool english in new[]{true,false}){Lang.English=english;using(var dialog=new ReportDialog()){FdmTheme.Apply(dialog,dialog,true);dialog.Show();Pump(100);bool inside=true,normalHeight=true;foreach(var c in Tree(dialog))if(c is GlassButton){var bounds=dialog.RectangleToClient(c.RectangleToScreen(c.ClientRectangle));inside&=dialog.ClientRectangle.Contains(bounds);normalHeight&=c.Height>=40&&c.Height<=56;}Check(inside,"report send button stays inside the "+(english?"English":"Persian")+" dialog");Check(normalHeight,"report send button keeps a normal action height in "+(english?"English":"Persian"));Capture(dialog,Path.Combine(pictures,english?"report-en-dark.png":"report-fa-dark.png"));dialog.Close();}}
 }
}
