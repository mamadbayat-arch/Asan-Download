using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Globalization;

[assembly: System.Reflection.AssemblyVersion("1.8.3.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.8.3.0")]
namespace MehrDownload {
 class CompletedFile {
  public string Id,Path;public long Size;public DateTime Finished;
  public double? DurationSeconds,AverageBytesPerSecond;
  public static string PersianDigits(string value){if(Lang.English)return value;for(int n=0;n<10;n++)value=value.Replace((char)('0'+n),(char)('۰'+n));return value;}
  public string PersianDate(){if(Finished==DateTime.MinValue)return "—";var date=Finished.Kind==DateTimeKind.Utc?Finished.ToLocalTime():Finished;
   if(Lang.English)return date.ToString("yyyy/MM/dd HH:mm",CultureInfo.InvariantCulture);
   var calendar=new PersianCalendar();return PersianDigits(String.Format(CultureInfo.InvariantCulture,"{0:0000}/{1:00}/{2:00} {3:00}:{4:00}",calendar.GetYear(date),calendar.GetMonth(date),calendar.GetDayOfMonth(date),date.Hour,date.Minute));}
  public string Duration(){if(!DurationSeconds.HasValue)return "—";long seconds=(long)Math.Ceiling(Math.Max(0,DurationSeconds.Value));return PersianDigits(String.Format(CultureInfo.InvariantCulture,"{0:00}:{1:00}:{2:00}",seconds/3600,(seconds/60)%60,seconds%60));}
  public string Average(bool bits){return AverageBytesPerSecond.HasValue?PersianDigits((AverageBytesPerSecond.Value*(bits?8:1)/1000000d).ToString("0.00",CultureInfo.InvariantCulture))+(bits?" Mb/s":" MB/s"):"—";}
 }
 class Saved {
  public string Url,Target,Folder;
  public int Connections=8;
  public bool SpeedInBits;
  public bool English;
  public bool Dark,SidebarCollapsed;
  public List<CompletedFile> History=new List<CompletedFile>();
  public List<string> Queue=new List<string>();
  public static Saved Load(string path) {
   Saved saved=HistoryStore.ReadSession(path) ?? HistoryStore.ReadSession(path+".bak") ?? new Saved();
   if(saved.History==null)saved.History=new List<CompletedFile>();
   if(saved.Queue==null)saved.Queue=new List<string>();
   if(String.IsNullOrEmpty(saved.Folder) && !String.IsNullOrEmpty(saved.Target)) saved.Folder=System.IO.Path.GetDirectoryName(saved.Target);
   HistoryStore.Merge(path,saved.History);return saved;
  }
  public void Save(string path) {
   HistoryStore.Save(path,this);
  }
  public string TargetFor(string url) {
   string name=DownloadEngine.FileName(url);
   if(String.IsNullOrEmpty(Folder)) return name;
   if(Url==url && !String.IsNullOrEmpty(Target) && System.IO.Path.GetDirectoryName(Target)==Folder && !File.Exists(Target) && Directory.Exists(Target+".mehrparts")) return Target;
   string candidate=System.IO.Path.Combine(Folder,name);
   for(int n=1;File.Exists(candidate) || Directory.Exists(candidate+".mehrparts");n++) candidate=System.IO.Path.Combine(Folder,System.IO.Path.GetFileNameWithoutExtension(name)+" ("+n+")"+System.IO.Path.GetExtension(name));
   return candidate;
  }
  public void Complete(string path,DownloadState metrics=null) {
   var file=new CompletedFile {Id=Guid.NewGuid().ToString("N"),Path=path,Size=new FileInfo(path).Length,Finished=DateTime.Now};if(metrics!=null&&metrics.HasTiming&&metrics.ActiveSeconds>0){file.DurationSeconds=metrics.ActiveSeconds;file.AverageBytesPerSecond=file.Size/metrics.ActiveSeconds;}History.Insert(0,file);
  }
 }

 class Window : GlassForm {
  TextBox link=new TextBox(), destination=new TextBox();
  GlassButton linkSummary;
  TableLayoutPanel contentLayout;
  TableLayoutPanel shellLayout;
  TableLayoutPanel bodyLayout;

  Label emptyHint;
  ToolTip hints=new ToolTip();
  ConnectionStepper connections=new ConnectionStepper();
  GlassButton start, pause, paste, addButton, batchButton, sortButton, filterButton, toggleSidebar;
  NavItem folder, theme;
  DownloadTable history=new DownloadTable();
  Saved saved=new Saved();
  Label status;
  StatTile speed, count, total;
  GlassProgress progress=new GlassProgress();
  DownloadEngine engine=new DownloadEngine();
  Timer timer=new Timer();
  long previous; DateTime previousTime=DateTime.UtcNow;
  readonly DownloadSpeed downloadSpeed=new DownloadSpeed();
  double displayedRate;
  internal bool LastDownloadDeclined;
  string currentUrl,currentTarget; int currentConnections; bool closing, dark;
  string settings=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MehrDownload","session.json");

  string filter="all", searchTerm="";
  readonly List<KeyValuePair<Control,string>> localized=new List<KeyValuePair<Control,string>>();
  readonly List<Label> alignedLabels=new List<Label>();
  NavItem languageItem;
  readonly List<NavItem> navItems=new List<NavItem>();
  List<DownloadItem> tableItems=new List<DownloadItem>();
  DownloadItem activeItem;
  DownloadTable tableHost;
  Panel sidebarHost;
  AdPlacement ad;
  StatusStrip statusStrip;
  SearchField searchField;
  DropZone dropZone;
  TableLayoutPanel composer;
  bool sidebarCollapsed;
  ColumnAnimator columnAnimator;
  TableLayoutPanel toolbarRow,composerRow;
  Panel sidebarScroll;
  Label sidebarBrand;
  bool compactLayout,updatingLayout;
  string settingsError,downloadError;

  public Window(bool restore) : this(restore,null) {}
  internal Window(bool restore,string settingsPath) {
   if(settingsPath!=null)settings=settingsPath;
   if(restore)try{saved=Saved.Load(settings);Lang.English=saved.English;}catch{settingsError=Lang.T("error.settings");}
   Text=Lang.T("app.name");var screen=Screen.PrimaryScreen.WorkingArea;
   ClientSize=new Size(Math.Min(1280,screen.Width-24),Math.Min(940,screen.Height-24));
   MinimumSize=new Size(Math.Min(960,screen.Width-24),Math.Min(640,screen.Height-24));
   StartPosition=FormStartPosition.CenterScreen;Font=Brand.Font(11);AutoScaleMode=AutoScaleMode.Dpi;
   RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;RightToLeftLayout=false;
   using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Download.ico"))Icon=new Icon(stream);
   BuildShell();
   BuildToolbar();
   BuildComposer();
   BuildStats();
   BuildTableArea();
   BuildStatusBar();
   BuildSponsor(restore && settingsPath==null);
   BuildSidebar();
   dark=saved.Dark;sidebarCollapsed=saved.SidebarCollapsed;
   shellLayout.ColumnStyles[0].Width=sidebarCollapsed?66:238;
   PaintTheme(this);ApplyLanguage();
   SetFilter("all");
   Resize+=(s,e)=>UpdateResponsiveLayout();
   contentLayout.SizeChanged+=(s,e)=>UpdateResponsiveLayout();
   UpdateResponsiveLayout();
   Shown+=(s,e)=>{
    UpdateTarget();
   };
   if(restore)try{link.Text=saved.Url;connections.SetValue(Math.Max(1,Math.Min(16,saved.Connections)));UpdateTarget();ReloadHistory();}catch{settingsError=Lang.T("error.settings");}
   if(settingsError!=null)status.Text=settingsError;
   UpdateQueueBadge();
   RenderSpeed();
   if(restore && settingsPath==null){var reportsTimer=new Timer{Interval=60000};reportsTimer.Tick+=async(s,e)=>await ProblemReports.Flush();Shown+=async(s,e)=>await ProblemReports.Flush();reportsTimer.Start();FormClosed+=(s,e)=>reportsTimer.Dispose();}
   timer.Interval=400;timer.Tick+=(s,e)=>RefreshDownload();timer.Start();
   FormClosing+=async(s,e)=>{if(engine.Running&&!closing){e.Cancel=true;closing=true;engine.Pause();Enabled=false;while(engine.Running)await Task.Delay(100);Close();}};
  }
  protected override void Dispose(bool disposing){if(disposing){timer.Stop();timer.Dispose();hints.Dispose();if(columnAnimator!=null)Ticker.Remove(columnAnimator);}base.Dispose(disposing);}

  // ───────────────────────────── layout ─────────────────────────────
  void BuildShell(){
   bodyLayout=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty,Padding=new Padding(0)};
   bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,96));
   Controls.Add(bodyLayout);
   shellLayout=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,Margin=Padding.Empty,Padding=new Padding(0)};
   shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,238));
   shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   bodyLayout.Controls.Add(shellLayout,0,0);
   sidebarHost=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent,Margin=Padding.Empty,Padding=new Padding(10,10,10,10)};
   shellLayout.Controls.Add(sidebarHost,0,0);
   contentLayout=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Margin=Padding.Empty,Padding=new Padding(14,10,14,6)};
   foreach(int h in new[]{64,78,78})contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
   contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));
   shellLayout.Controls.Add(contentLayout,1,0);
  }

  void BuildToolbar(){
   var bar=new Panel{Dock=DockStyle.Fill,BackColor=Color.Transparent,Margin=Padding.Empty};
   var row=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1,Margin=Padding.Empty,Padding=new Padding(0)};
   toolbarRow=row;
   row.RowStyles.Add(new RowStyle(SizeType.Absolute,52));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,156));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,128));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,164));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,214));
   // left: primary action
   addButton=new GlassButton{Text=Lang.T("action.add"),Primary=true,Glyph=1,Font=Brand.Font(11,FontStyle.Bold),Dock=DockStyle.Fill,Margin=new Padding(0,6,10,6),AccessibleName=Lang.T("action.addNew")};
   localized.Add(new KeyValuePair<Control,string>(addButton,"action.add"));
   addButton.Click+=(s,e)=>{link.Focus();link.SelectAll();};
   row.Controls.Add(addButton,4,0);
   batchButton=new GlassButton{Text=Lang.T("action.batch"),Glyph=22,Font=Brand.Font(10),Dock=DockStyle.Fill,Margin=new Padding(0,6,10,6),AccessibleName=Lang.T("action.batch")};
   batchButton.Click+=(s,e)=>OpenBatch();
   localized.Add(new KeyValuePair<Control,string>(batchButton,"action.batch"));
   row.Controls.Add(batchButton,3,0);
   // search
   searchField=new SearchField{Placeholder=Lang.T("search.placeholder"),AccessibleName=Lang.T("search.label"),Dock=DockStyle.Fill};
   searchField.QueryChanged+=(s,e)=>{searchTerm=searchField.Input.Text.Trim();RefreshTable();};
   var searchHost=new Panel{Dock=DockStyle.Fill,Margin=new Padding(0,6,10,6),BackColor=Color.Transparent};
   searchHost.Controls.Add(searchField);
   row.Controls.Add(searchHost,2,0);
   // right: sort + filter
   sortButton=MenuButton(Lang.T("sort.newest"),16);
   filterButton=MenuButton(Lang.T("filter.all"),17);
   localized.Add(new KeyValuePair<Control,string>(sortButton,"sort.newest"));
   localized.Add(new KeyValuePair<Control,string>(filterButton,"filter.all"));
   row.Controls.Add(sortButton,1,0);
   row.Controls.Add(filterButton,0,0);
   bar.Controls.Add(row);
   contentLayout.Controls.Add(bar,0,0);
   var sortMenu=new ContextMenuStrip{Font=Brand.Font(10),RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No};
   sortMenu.Items.Add(Lang.T("sort.newest"),null,(s,e)=>SetSort("date",true));
   sortMenu.Items.Add(Lang.T("sort.oldest"),null,(s,e)=>SetSort("date",false));
   sortMenu.Items.Add(Lang.T("sort.name"),null,(s,e)=>SetSort("title",false));
   sortMenu.Items.Add(Lang.T("sort.size"),null,(s,e)=>SetSort("size",true));
   sortButton.Menu=sortMenu;
   var filterMenu=new ContextMenuStrip{Font=Brand.Font(10),RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No};
   filterMenu.Items.Add(Lang.T("filter.all"),null,(s,e)=>SetFilter("all"));
   filterMenu.Items.Add(Lang.T("filter.active"),null,(s,e)=>SetFilter("active"));
   filterMenu.Items.Add(Lang.T("filter.completed"),null,(s,e)=>SetFilter("completed"));
   filterMenu.Items.Add(Lang.T("filter.queued"),null,(s,e)=>SetFilter("queued"));
   filterButton.Menu=filterMenu;
  }

  GlassButton MenuButton(string text,int glyph){
   var b=new GlassButton{Text=text,Glyph=glyph,GlyphTrailing=true,Font=Brand.Font(10),Dock=DockStyle.Fill,Margin=new Padding(0,6,8,6),AccessibleName=text};
   return b;
  }

  void BuildComposer(){
   var card=new GlassPanel{Dock=DockStyle.Fill,Radius=16,Padding=new Padding(14),Margin=new Padding(0,0,0,6),Elevated=true};
   composer=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=1,Margin=Padding.Empty};
   composer.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
   card.Controls.Add(composer);
   var row=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1,Margin=Padding.Empty};
   composerRow=row;
   row.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,180));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,108));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,92));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,182));
   // actions on the left, link field on the right (RTL reading order)
   var actionHost=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty,BackColor=Color.Transparent};
   start=Button(Lang.T("action.start"),1);((GlassButton)start).Primary=true;start.Dock=DockStyle.Fill;start.Font=Brand.Font(11,FontStyle.Bold);localized.Add(new KeyValuePair<Control,string>(start,"action.start"));
   pause=Button(Lang.T("action.stop"),10);pause.Dock=DockStyle.Fill;pause.Visible=false;pause.Font=Brand.Font(11,FontStyle.Bold);localized.Add(new KeyValuePair<Control,string>(pause,"action.stop"));
   actionHost.Controls.Add(start);actionHost.Controls.Add(pause);
   row.Controls.Add(actionHost,4,0);
   connections.Dock=DockStyle.Fill;connections.Margin=new Padding(0,0,10,0);connections.AccessibleName=Lang.T("connections.label");
   hints.SetToolTip(connections,Lang.T("connections.hint"));
   row.Controls.Add(connections,3,0);
   paste=Button(Lang.T("action.paste"),0);paste.Dock=DockStyle.Fill;paste.Margin=new Padding(0,0,10,0);localized.Add(new KeyValuePair<Control,string>(paste,"action.paste"));
   row.Controls.Add(paste,2,0);
   linkSummary=new GlassButton{Text=Lang.T("link.view"),RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No,Font=Brand.Font(10),Dock=DockStyle.Fill,Margin=new Padding(0,0,10,0),AccessibleName=Lang.T("link.edit")};
   linkSummary.Click+=(s,e)=>EditLink();
   row.Controls.Add(linkSummary,1,0);
   link.RightToLeft=RightToLeft.No;link.Font=Brand.Font(11);link.AccessibleName=Lang.T("link.label");link.BorderStyle=BorderStyle.None;link.TextAlign=Lang.Rtl?HorizontalAlignment.Right:HorizontalAlignment.Left;
   var inputShell=new GlassPanel{Dock=DockStyle.Fill,Radius=12,Padding=new Padding(12,9,12,7),Margin=Padding.Empty,Elevated=false};
   link.Dock=DockStyle.Fill;inputShell.Controls.Add(link);
   row.Controls.Add(inputShell,0,0);
   composer.Controls.Add(row,0,0);
   status=new Label{Text=Lang.T("status.idle"),BackColor=Color.Transparent,Font=Brand.Font(9),AutoSize=false};
   contentLayout.Controls.Add(card,0,1);
   paste.Click+=(s,e)=>{try{if(Clipboard.ContainsText()){link.Text=Clipboard.GetText().Trim();link.Focus();}else MessageBox.Show(Lang.T("error.clipboard"),Text);}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};
   link.TextChanged+=(s,e)=>UpdateTarget();
   link.HandleCreated+=(s,e)=>Brand.Cue(link,Lang.T("link.placeholder"));
   start.Click+=async(s,e)=>await Start();
   pause.Click+=(s,e)=>{engine.Pause();pause.Enabled=false;status.Text=Lang.T("status.stopping");};
  }

  void BuildStats(){
   var row=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=new Padding(0,4,0,8)};
   row.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   for(int n=0;n<3;n++)row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));
   speed=new StatTile{Caption=Lang.T("stat.speed"),IconGlyph="speed",Suffix=" MB/s",Format="0.00",Dock=DockStyle.Fill};
   count=new StatTile{Caption=Lang.T("stat.completed"),IconGlyph="count",Format="0",Dock=DockStyle.Fill};
   total=new StatTile{Caption=Lang.T("stat.size"),IconGlyph="size",Suffix=" MB",Format="0.0",Dock=DockStyle.Fill};
   row.Controls.Add(speed,0,0);row.Controls.Add(count,1,0);row.Controls.Add(total,2,0);
   speed.Click+=(s,e)=>{saved.SpeedInBits=!saved.SpeedInBits;RenderSpeed();ReloadHistory();try{saved.Save(settings);}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};
   speed.Cursor=Cursors.Hand;
   speed.TabStop=true;count.TabStop=false;total.TabStop=false;
   hints.SetToolTip(speed,Lang.T("stat.speedTip"));
   contentLayout.Controls.Add(row,0,2);
  }

  void BuildTableArea(){
   var host=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
   host.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   host.RowStyles.Add(new RowStyle(SizeType.Absolute,86));
   history=new DownloadTable{Dock=DockStyle.Fill,Margin=Padding.Empty};
   emptyHint=history.EmptyHint;
   history.Activated+=(item)=>OpenItem(item);
   history.MenuRequested+=(item)=>ShowItemMenu(item);
   var menu=new ContextMenuStrip{Font=Brand.Font(11),RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No};
   menu.Items.Add(Lang.T("menu.open"),null,(s,e)=>OpenCompleted(false));
   menu.Items.Add(Lang.T("menu.reveal"),null,(s,e)=>OpenCompleted(true));
   history.Menu=menu;
   history.SelectionChanged+=(s,e)=>RefreshFilterLabels();
   host.Controls.Add(history,0,0);
   dropZone=new DropZone{Message=Lang.T("drop.hint"),Dock=DockStyle.Fill,Margin=new Padding(0,10,0,0)};
   dropZone.Dropped+=(files)=>{
    foreach(string file in files){
     if(file.StartsWith("http://",StringComparison.OrdinalIgnoreCase)||file.StartsWith("https://",StringComparison.OrdinalIgnoreCase))ReceiveDownload(file);
    }
   };
   host.Controls.Add(dropZone,0,1);
   contentLayout.Controls.Add(host,0,3);
   tableHost=history;
  }

  void BuildStatusBar(){
   statusStrip=new StatusStrip{Dock=DockStyle.Fill,Margin=new Padding(0),SpeedText="▼ 0.00 MB/s",BadgeText=CompletedFile.PersianDigits("0")+" "+Lang.T("status.connections")};
   contentLayout.Controls.Add(statusStrip,0,4);
  }

  void BuildSponsor(bool online){
   ad=new AdPlacement(online){Dock=DockStyle.Fill,Margin=new Padding(14,4,14,10)};
   var host=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=1,Margin=Padding.Empty};
   host.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   host.Controls.Add(ad,0,0);
   if(bodyLayout!=null)bodyLayout.Controls.Add(host,0,1);
  }

  void BuildSidebar(){
   var layout=new SmoothTableLayout{Dock=DockStyle.None,Height=510,ColumnCount=1,RowCount=13,Margin=Padding.Empty,RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No};
   layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   var heights=new[]{34,44,44,44,44,18,44,44,44,18,44,44,44};
   for(int n=0;n<heights.Length;n++)layout.RowStyles.Add(new RowStyle(SizeType.Absolute,heights[n]));
   var brandLabel=new Label{Text=Lang.T("app.name"),Dock=DockStyle.Fill,TextAlign=Lang.Rtl?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft,Font=Brand.Font(12.5f,FontStyle.Bold),BackColor=Color.Transparent,ForeColor=Fdm.Light.Ink,AutoSize=false};
   sidebarBrand=brandLabel;localized.Add(new KeyValuePair<Control,string>(brandLabel,"app.name"));alignedLabels.Add(brandLabel);
   layout.Controls.Add(brandLabel,0,0);
   navItems.Add(Nav(Lang.T("filter.allNav"),0,()=>SetFilter("all")));
   navItems.Add(Nav(Lang.T("filter.active"),1,()=>SetFilter("active")));
   navItems.Add(Nav(Lang.T("filter.completed"),2,()=>SetFilter("completed")));
   navItems.Add(Nav(Lang.T("filter.queuedNav"),3,()=>SetFilter("queued")));
   layout.Controls.Add(navItems[0],0,1);
   layout.Controls.Add(navItems[1],0,2);
   layout.Controls.Add(navItems[2],0,3);
   layout.Controls.Add(navItems[3],0,4);
   layout.Controls.Add(new Panel{Dock=DockStyle.Fill,Margin=new Padding(10,6,10,6),BackColor=Color.Transparent},0,5);
   folder=NavButton(Lang.T("nav.downloads"),5);
   folder.Tag="nav.downloads";
   folder.Click+=(s,e)=>{try{if(!String.IsNullOrEmpty(saved.Folder)&&Directory.Exists(saved.Folder))Process.Start("explorer.exe","\""+saved.Folder+"\"");else ChooseFolder();}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};
   var setFolder=NavButton(Lang.T("nav.setFolder"),4);
   setFolder.Tag="nav.setFolder";
   setFolder.Click+=(s,e)=>{if(!engine.Running)ChooseFolder();};
   var report=NavButton(Lang.T("nav.report"),6);
   report.Tag="nav.report";
   report.Click+=(s,e)=>{using(var dialog=new ReportDialog()){FdmTheme.Apply(dialog,dialog,dark);dialog.ShowDialog(this);}};
   layout.Controls.Add(folder,0,6);
   layout.Controls.Add(setFolder,0,7);
   layout.Controls.Add(report,0,8);
   layout.Controls.Add(new Panel{Dock=DockStyle.Fill,Margin=new Padding(10,6,10,6),BackColor=Color.Transparent},0,9);
   theme=NavButton(Lang.T("nav.theme"),9);
   theme.Tag="nav.theme";
   theme.Click+=(s,e)=>ToggleTheme();
   var integration=NavButton(Lang.T("nav.integration"),7);
   integration.Tag="nav.integration";
   integration.Click+=(s,e)=>IntegrationSettings();
   languageItem=NavButton(Lang.English?"فارسی":"English",12);
   languageItem.Click+=(s,e)=>ToggleLanguage();
   layout.Controls.Add(theme,0,10);
   layout.Controls.Add(languageItem,0,11);
   layout.Controls.Add(integration,0,12);
   toggleSidebar=new GlassButton{Glyph=18,Font=Brand.Font(10),Dock=DockStyle.Bottom,Height=44,Margin=Padding.Empty,AccessibleName=Lang.T("action.toggleSidebar")};
   toggleSidebar.Click+=(s,e)=>ToggleSidebar();
   sidebarScroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.Transparent};
   sidebarScroll.Controls.Add(layout);
   sidebarScroll.SizeChanged+=(s,e)=>FitSidebar(layout);
   sidebarScroll.Layout+=(s,e)=>FitSidebar(layout);
   sidebarHost.Controls.Add(sidebarScroll);sidebarHost.Controls.Add(toggleSidebar);
  }
  void FitSidebar(Control layout){
   int scrollbar=sidebarScroll.Height<layout.Height?SystemInformation.VerticalScrollBarWidth:0;
   var bounds=new Rectangle(0,sidebarScroll.AutoScrollPosition.Y,Math.Max(1,sidebarScroll.Width-scrollbar),510);
   if(layout.Bounds!=bounds)layout.Bounds=bounds;
  }

  NavItem Nav(string text,int glyph,Action action){
   var item=new NavItem{Caption=text,Glyph=glyph,Dock=DockStyle.Fill,Margin=new Padding(0,1,0,1)};
   item.Click+=(s,e)=>{if(action!=null)action();};
   return item;
  }
  NavItem NavButton(string text,int glyph){
   var item=new NavItem{Caption=text,Glyph=glyph,Dock=DockStyle.Fill,Margin=new Padding(0,1,0,1)};
   return item;
  }

  void ToggleLanguage(){
   Lang.English=!Lang.English;
   saved.English=Lang.English;
   try{saved.Save(settings);}catch{}
   ApplyLanguage();
  }
  void ApplyLanguage(){
   BeginVisualUpdate();
   try{
   Text=Lang.T("app.name");
   foreach(var entry in localized)entry.Key.Text=Lang.T(entry.Value);
   foreach(Label label in alignedLabels)label.TextAlign=Lang.Rtl?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft;
   Lang.Apply(this);
   Brand.Rescale(this);
   titleBar.ApplyLanguage();
   LocalizeMenu(sortButton.Menu,new[]{"sort.newest","sort.oldest","sort.name","sort.size"});
   LocalizeMenu(filterButton.Menu,new[]{"filter.all","filter.active","filter.completed","filter.queued"});
   LocalizeMenu(history.Menu,new[]{"menu.open","menu.reveal"});
   navItems[0].Caption=Lang.T("filter.allNav");
   navItems[1].Caption=Lang.T("filter.active");
   navItems[2].Caption=Lang.T("filter.completed");
   navItems[3].Caption=Lang.T("filter.queuedNav");
   languageItem.Caption=Lang.English?"فارسی":"English";
   LocalizeSidebar(sidebarHost);
   hints.SetToolTip(languageItem,Lang.T("nav.language")+": "+languageItem.Caption);
   if(dropZone!=null){dropZone.Message=Lang.T("drop.hint");dropZone.Invalidate();}
   if(ad!=null)ad.ApplyLanguage();
   searchField.Placeholder=Lang.T("search.placeholder");
   searchField.Input.Tag=Lang.T("search.placeholder");
   searchField.Input.RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;
   searchField.ApplyLanguage();
   Brand.Cue(searchField.Input,Lang.T("search.placeholder"));
   link.TextAlign=Lang.Rtl?HorizontalAlignment.Right:HorizontalAlignment.Left;
   speed.Caption=Lang.T("stat.speed");count.Caption=Lang.T("stat.completed");total.Caption=Lang.T("stat.size");
   linkSummary.AccessibleName=Lang.T("link.edit");
   linksAccessibleName();
   toggleSidebar.AccessibleName=Lang.T("action.toggleSidebar");hints.SetToolTip(toggleSidebar,toggleSidebar.AccessibleName);
   foreach(var button in new[]{start,pause,paste,addButton,batchButton,sortButton,filterButton})button.AccessibleName=button.Text;
   RenderSpeed();
   RefreshFilterLabels();
   UpdateTarget();
   RefreshTable();
   RefreshStatusStrip();
   }finally{EndVisualUpdate();}
  }
  void LocalizeSidebar(Control root){foreach(Control c in root.Controls){var item=c as NavItem;if(item!=null){if(item.Tag is string)item.Caption=Lang.T((string)item.Tag);hints.SetToolTip(item,item.Caption);}LocalizeSidebar(c);}}
  void LocalizeMenu(ContextMenuStrip menu,string[] keys){menu.RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;menu.Font=Brand.Font(10);for(int n=0;n<keys.Length;n++)menu.Items[n].Text=Lang.T(keys[n]);}
  void linksAccessibleName(){
   link.AccessibleName=Lang.T("link.label");
   destination.AccessibleName=Lang.T("link.path");
   connections.AccessibleName=Lang.T("connections.label");
   hints.SetToolTip(connections,Lang.T("connections.hint"));
  }
  void ToggleSidebar(){
   sidebarCollapsed=!sidebarCollapsed;
   saved.SidebarCollapsed=sidebarCollapsed;try{saved.Save(settings);}catch{}
   sidebarBrand.Visible=!sidebarCollapsed;
   sidebarHost.Padding=sidebarCollapsed?new Padding(2,10,2,8):new Padding(10);
   if(columnAnimator!=null)Ticker.Remove(columnAnimator);
   int from=(int)shellLayout.ColumnStyles[0].Width;
   int to=sidebarCollapsed?66:238;
   columnAnimator=new ColumnAnimator(shellLayout,0,from,to,SystemInformation.UIEffectsEnabled?0.22:0);
   Ticker.Add(columnAnimator);
  }
  void ToggleTheme(){
   dark=!dark;saved.Dark=dark;try{saved.Save(settings);}catch{}
   BeginVisualUpdate();
   try{PaintTheme(this);}finally{EndVisualUpdate();}
  }
  void UpdateResponsiveLayout(){
   if(updatingLayout||composerRow==null||toolbarRow==null||sidebarBrand==null)return;
   updatingLayout=true;
   try{
    bool compact=contentLayout.Width<980;
    if(compact!=compactLayout){
     compactLayout=compact;
     var search=searchField.Parent;var actions=start.Parent;var input=link.Parent;
     toolbarRow.SuspendLayout();composerRow.SuspendLayout();
     toolbarRow.ColumnStyles.Clear();toolbarRow.RowStyles.Clear();
     composerRow.ColumnStyles.Clear();composerRow.RowStyles.Clear();
     toolbarRow.ColumnCount=5;toolbarRow.RowCount=2;
     composerRow.ColumnCount=5;composerRow.RowCount=2;
     foreach(int width in compact?new[]{150,130,0,180}:new[]{156,128,0,164,214})toolbarRow.ColumnStyles.Add(new ColumnStyle(width==0?SizeType.Percent:SizeType.Absolute,width==0?100:width));
     foreach(int width in compact?new[]{0,108,100,182}:new[]{0,180,108,100,182})composerRow.ColumnStyles.Add(new ColumnStyle(width==0?SizeType.Percent:SizeType.Absolute,width==0?100:width));
     for(int n=0;n<(compact?2:1);n++){toolbarRow.RowStyles.Add(new RowStyle(SizeType.Percent,compact?50:100));composerRow.RowStyles.Add(new RowStyle(SizeType.Percent,compact?50:100));}
     toolbarRow.SetColumnSpan(search,compact?4:1);
     toolbarRow.SetCellPosition(search,new TableLayoutPanelCellPosition(compact?0:2,0));
     toolbarRow.SetCellPosition(filterButton,new TableLayoutPanelCellPosition(0,compact?1:0));toolbarRow.SetCellPosition(sortButton,new TableLayoutPanelCellPosition(1,compact?1:0));
     toolbarRow.SetCellPosition(batchButton,new TableLayoutPanelCellPosition(compact?2:3,compact?1:0));toolbarRow.SetCellPosition(addButton,new TableLayoutPanelCellPosition(compact?3:4,compact?1:0));
     composerRow.SetColumnSpan(input,compact?3:1);composerRow.SetCellPosition(input,new TableLayoutPanelCellPosition(0,0));
     composerRow.SetCellPosition(linkSummary,new TableLayoutPanelCellPosition(compact?3:1,0));composerRow.SetCellPosition(paste,new TableLayoutPanelCellPosition(compact?1:2,compact?1:0));
     composerRow.SetCellPosition(connections,new TableLayoutPanelCellPosition(compact?2:3,compact?1:0));composerRow.SetCellPosition(actions,new TableLayoutPanelCellPosition(compact?3:4,compact?1:0));
     toolbarRow.ColumnCount=compact?4:5;toolbarRow.RowCount=compact?2:1;
     composerRow.ColumnCount=compact?4:5;composerRow.RowCount=compact?2:1;
     input.Margin=compact?new Padding(0,0,0,6):Padding.Empty;
     linkSummary.Margin=new Padding(0,0,10,compact?6:0);
     composer.RowStyles[0].SizeType=SizeType.Percent;composer.RowStyles[0].Height=100;
     toolbarRow.ResumeLayout(true);composerRow.ResumeLayout(true);
    }
    contentLayout.RowStyles[0].Height=compact?106:64;contentLayout.RowStyles[1].Height=compact?126:78;
    bodyLayout.RowStyles[1].Height=ClientSize.Height<720?80:96;
    var tableLayout=(TableLayoutPanel)history.Parent;
    bool showDropZone=ClientSize.Height>=740;
    dropZone.Visible=showDropZone;
    tableLayout.RowStyles[1].Height=showDropZone?86:0;
    sidebarBrand.Visible=!sidebarCollapsed;
    sidebarHost.Padding=sidebarCollapsed?new Padding(2,10,2,8):new Padding(10);
   }finally{updatingLayout=false;}
  }

  // ───────────────────────────── helpers ─────────────────────────────
  Control Input(TextBox box){
   var panel=new GlassPanel{Dock=DockStyle.Fill,Radius=12,Padding=new Padding(12,6,12,4),Margin=Padding.Empty,Elevated=false};
   panel.BackColor=Color.Transparent;
   box.BorderStyle=BorderStyle.None;box.Dock=DockStyle.Fill;panel.Controls.Add(box);return panel;
  }
  Label Label(string text){
   var label=new Label{Text=text,Dock=DockStyle.Fill,TextAlign=Lang.Rtl?ContentAlignment.MiddleRight:ContentAlignment.MiddleLeft,AutoEllipsis=true,AutoSize=false,BackColor=Color.Transparent,Margin=Padding.Empty,Font=Brand.Font(10)};
   alignedLabels.Add(label);return label;
  }
  GlassButton Button(string text,int glyph){return new GlassButton{Text=text,Glyph=glyph,Font=Brand.Font(10),Height=40,Width=110,Margin=new Padding(0)};}

  void RefreshFilterLabels(){
   filterButton.Text=Lang.T(filterNames[filter]);
   sortButton.Text=history.SortKey=="title"?Lang.T("sort.name"):(history.SortKey=="size"?Lang.T("sort.size"):(history.SortDescending?Lang.T("sort.newest"):Lang.T("sort.oldest")));
  }
  void SetFilter(string value){
   filter=value;
   for(int n=0;n<navItems.Count;n++)navItems[n].Active=(n<4&&navNames[n]==value);
   RefreshFilterLabels();
   RefreshTable();
  }
  void SetSort(string key,bool descending){
   history.SortKey=key;history.SortDescending=descending;
   history.ApplySort();
   RefreshFilterLabels();
  }
  readonly string[] navNames={"all","active","completed","queued"};
  readonly Dictionary<string,string> filterNames=new Dictionary<string,string>{{"all","filter.all"},{"active","filter.active"},{"completed","filter.completed"},{"queued","filter.queued"}};

  void PaintTheme(Control root) {
   FdmTheme.Apply(this,root,dark);
  }

  void UpdateTarget() {
   if(engine.Running)return;
   Uri uri;string url=link.Text.Trim();
   bool valid=Uri.TryCreate(url,UriKind.Absolute,out uri) && (uri.Scheme=="http" || uri.Scheme=="https");
   try{destination.Text=valid?saved.TargetFor(url):(saved.Folder ?? "");}catch{destination.Text=saved.Folder ?? "";}
   linkSummary.Text=valid?uri.Host+" / "+DownloadEngine.FileName(url):(String.IsNullOrEmpty(url)?Lang.T("link.view"):Lang.T("link.edit"));
   bool configured=!String.IsNullOrEmpty(saved.Folder);
   statusStrip.Hint=!configured?Lang.T("folder.incomplete"):(valid?destination.Text:(saved.Folder??""));
   statusStrip.Invalidate();
  }

  void EditLink() {
   using(var dialog=new GlassForm{Text=Lang.T("link.dialog.title"),ClientSize=new Size(720,280),MinimumSize=new Size(520,260),StartPosition=FormStartPosition.CenterParent,Font=Brand.Font(11),RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No}){
    var full=new TextBox{Text=link.Text,Multiline=true,ScrollBars=ScrollBars.Vertical,RightToLeft=RightToLeft.No,Dock=DockStyle.Fill,Font=Brand.Font(11),BorderStyle=BorderStyle.None,AccessibleName=Lang.T("link.dialog.title")};
    var body=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(22),RowCount=3,ColumnCount=1};
    body.RowStyles.Add(new RowStyle(SizeType.Absolute,28));
    body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
    body.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
    body.Controls.Add(Label(Lang.T("link.dialog.hint")),0,0);
    var shell=new GlassPanel{Dock=DockStyle.Fill,Radius=12,Padding=new Padding(12),Elevated=false};shell.BackColor=Color.Transparent;shell.Controls.Add(full);
    body.Controls.Add(shell,0,1);
    var save=new GlassButton{Text=Lang.T("link.dialog.confirm"),Dock=DockStyle.Right,Width=140,DialogResult=DialogResult.OK,Primary=true};
    body.Controls.Add(save,0,2);dialog.Controls.Add(body);dialog.AcceptButton=save;
    FdmTheme.Apply(dialog,dialog,dark);
    dialog.KeyPreview=true;dialog.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape)dialog.DialogResult=DialogResult.Cancel;};
    if(dialog.ShowDialog(this)==DialogResult.OK)link.Text=full.Text.Trim();
   }
  }

  void ReloadHistory() {
   RefreshTable();
   count.AnimateTo(saved.History.Count);
   long bytes=0;foreach(var file in saved.History)bytes+=file.Size;
   total.AnimateTo(bytes/1048576.0);
  }

  void RefreshTable(){
   tableItems.Clear();
   activeItem=BuildActiveItem();
   if(activeItem!=null&&(filter=="all"||filter=="active"))tableItems.Add(activeItem);
   if(filter=="all"||filter=="queued"){
    int index=0;
    foreach(string queued in saved.Queue){
     string title="";try{title=DownloadEngine.FileName(queued);}catch{}
     tableItems.Add(new DownloadItem{Title=title,Kind="queued",StatusText=Lang.T("row.queued"),SizeText="—",SpeedText="—",DateText="—",Order=long.MaxValue-1000-index,Tag=queued});
     index++;
    }
   }
   if(filter=="all"||filter=="completed"){
    foreach(var file in saved.History){
     tableItems.Add(new DownloadItem{
      Title=Path.GetFileName(file.Path),
      Kind="completed",
      StatusText=file.DurationSeconds.HasValue?(Lang.T("row.duration")+"\u200E"+file.Duration()+"\u200E • \u200E"+file.Average(saved.SpeedInBits)+"\u200E"):Lang.T("row.completed"),
      SizeText=CompletedFile.PersianDigits((file.Size/1048576.0).ToString("0.00",CultureInfo.InvariantCulture))+" MB",
      SpeedText=file.Average(saved.SpeedInBits),
      DateText=file.PersianDate(),
      Order=file.Finished.Ticks,
      Bytes=file.Size,
      Tag=file});
    }
   }
   if(searchTerm.Length>0){
    var filtered=new List<DownloadItem>();
    foreach(var item in tableItems)if(item.Title!=null&&item.Title.IndexOf(searchTerm,StringComparison.OrdinalIgnoreCase)>=0)filtered.Add(item);
    tableItems=filtered;
   }
   history.EmptyMessage=tableItems.Count==0?Lang.T(searchTerm.Length>0?"table.noResults":filter=="all"?"table.empty":"table.emptyFilter"):"";
   history.SetItems(tableItems);
   history.ApplySort();
   history.Invalidate();
   UpdateQueueBadge();
  }

  DownloadItem BuildActiveItem(){
   if(engine.State==null&&!engine.Running&&String.IsNullOrEmpty(currentTarget))return null;
   // A finished download already lives in the completed list, so it is not shown twice.
   if(!engine.Running&&!String.IsNullOrEmpty(currentTarget)&&File.Exists(currentTarget))return null;
   string name=String.IsNullOrEmpty(currentTarget)?Lang.T("action.add"):Path.GetFileName(currentTarget);
   long received=System.Threading.Interlocked.Read(ref engine.Received);
   long size=engine.State!=null?engine.State.Size:0;
   double progress=size>0?Math.Min(1,received/(double)size):0;
   var item=new DownloadItem{Title=name,Kind=engine.Running?"active":"paused",Order=long.MaxValue};
   item.Progress=progress;item.Indeterminate=size<=0&&engine.Running;
   item.Bytes=size;
   item.SizeText=size>0?(size/1048576.0).ToString("0.0")+" MB":(received/1048576.0).ToString("0.0")+" MB";
   item.SpeedText=engine.Running?(CompletedFile.PersianDigits((displayedRate*(saved.SpeedInBits?8:1)/1000000d).ToString("0.00"))+(saved.SpeedInBits?" Mb/s":" MB/s")):"—";
   string done=(received/1048576.0).ToString("0.0")+" MB";
   item.StatusText=size>0?(Lang.T("row.received")+done+" • "+(engine.Status??"")):(engine.Status??"");
   item.DateText=engine.Running?Lang.T("row.now"):"—";
   return item;
  }

  void ShowItemMenu(DownloadItem item){
   if(item==null||item.Kind!="completed")return;
   var target=(CompletedFile)item.Tag;
   if(target==null)return;
   selectedFile=target;
   var menu=new ContextMenuStrip{Font=Brand.Font(11),RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No};
   menu.Items.Add(Lang.T("menu.open"),null,(s,e)=>OpenCompleted(false));
   menu.Items.Add(Lang.T("menu.reveal"),null,(s,e)=>OpenCompleted(true));
   FdmTheme.ApplyMenu(menu,dark);menu.Closed+=(s,e)=>menu.Dispose();
   menu.Show(Cursor.Position);
  }
  CompletedFile selectedFile;

  void OpenItem(DownloadItem item){
   if(item==null)return;
   if(item.Kind=="completed"){
    var file=(CompletedFile)item.Tag;
    if(file!=null){selectedFile=file;OpenCompleted(false);}
   }
  }

  void OpenCompleted(bool reveal) {
   if(selectedFile==null)return;
   var file=selectedFile;
   try{if(!File.Exists(file.Path)){MessageBox.Show(Lang.T("table.missing")+file.Path,Text);return;}if(reveal)Process.Start("explorer.exe","/select,\""+file.Path+"\"");else Process.Start(new ProcessStartInfo(file.Path){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message,Text);}
  }

  bool ChooseFolder() {
   using(var dlg=new FolderBrowserDialog {Description=Lang.T("folder.dialog"),SelectedPath=saved.Folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads")}) {
    if(dlg.ShowDialog()!=DialogResult.OK)return false;
    try{saved.Folder=dlg.SelectedPath;saved.Save(settings);UpdateTarget();return true;}catch(Exception ex){MessageBox.Show(ex.Message,Text);return false;}
   }
  }
  // Runs an action on the user interface thread and completes when it has been applied.
  Task RunUi(Action action){
   if(IsDisposed||!IsHandleCreated){action();return Task.FromResult(0);}
   if(!InvokeRequired){action();return Task.FromResult(0);}
   var completion=new TaskCompletionSource<int>();
   try{BeginInvoke(new Action(delegate(){
    try{action();completion.SetResult(0);}catch(Exception ex){completion.SetException(ex);}
   }));}catch(Exception ex){completion.SetException(ex);}
   return completion.Task;
  }
  async Task Start(bool confirmed=false) {
   if(engine.Running) return;
   Uri uri; string url=link.Text.Trim();
   if(!Uri.TryCreate(url,UriKind.Absolute,out uri) || (uri.Scheme!="http" && uri.Scheme!="https")) {MessageBox.Show(Lang.T("error.invalidUrl"),Text);return;}
   if(String.IsNullOrEmpty(saved.Folder) && !ChooseFolder()) return;
   UpdateTarget();
   if(!confirmed&&!ConfirmDownload(url,false))return;
   Exception failure=null;
   try {
    string target=Path.GetFullPath(destination.Text);
    if(File.Exists(target)) {MessageBox.Show(Lang.T("error.exists"),Text);return;}
    Directory.CreateDirectory(Path.GetDirectoryName(target));
    currentUrl=url;currentTarget=target;currentConnections=connections.Value;
    downloadError=null;
    saved.Url=url;saved.Target=target;saved.Connections=currentConnections;saved.Save(settings);
    previous=0;previousTime=DateTime.UtcNow;downloadSpeed.Reset();
    SetRunning(true);
    await engine.Run(url,target,currentConnections);
    await RunUi(delegate{
     if(engine.Error==null && File.Exists(target)){saved.Complete(target,engine.State);saved.Save(settings);ReloadHistory();ShowToast(Lang.T("toast.completed")+Path.GetFileName(target),false);}
    });
   } catch(Exception ex) {failure=ex;}
   await RunUi(delegate{
    SetRunning(false);RefreshDownload();
    if(!closing&&(failure!=null||engine.Error!=null)){downloadError=failure!=null?failure.Message:engine.Error;status.Text=downloadError;RefreshStatusStrip();ShowToast(Lang.T("toast.failed"),true);}
    if(!closing&&failure==null&&engine.Error==null&&!engine.Paused)StartNextQueued();
   });
  }

  void SetRunning(bool running){
   link.Enabled=linkSummary.Enabled=paste.Enabled=connections.Enabled=addButton.Enabled=!running;
   pause.Visible=running;pause.Enabled=running;
   start.Visible=!running;if(running)pause.BringToFront();
   start.Text=running?Lang.T("status.downloading"):Lang.T("action.start");
   start.Enabled=!running;
  }

  void OpenBatch(){
   using(var dialog=new BatchDialog(dark)){
    if(dialog.ShowDialog(this)!=DialogResult.OK)return;
    AddBatch(dialog.Links);
   }
  }
  int AddBatch(string text){
   var links=ParseLinks(text);
   if(links.Count==0){MessageBox.Show(Lang.T("batch.empty"),Text);return 0;}
   int added=0;bool full=false;
   foreach(string url in links){
    if(url==currentUrl||saved.Queue.Contains(url))continue;
    if(saved.Queue.Count>=100){full=true;break;}
    saved.Queue.Add(url);added++;
   }
   if(added==0){MessageBox.Show(Lang.T("batch.invalid"),Text);return 0;}
   SaveSettings();RefreshTable();
   ShowToast(Lang.T("batch.added").Replace("{0}",CompletedFile.PersianDigits(added.ToString())),false);
   if(full)ShowToast(Lang.T("batch.limit"),true);
   if(!engine.Running)StartNextQueued();
   return added;
  }
  void StartNextQueued(){
   if(engine.Running||closing)return;
   if(saved.Queue.Count==0){UpdateQueueBadge();return;}
   string next=saved.Queue[0];saved.Queue.RemoveAt(0);SaveSettings();RefreshTable();
   ReceiveDownload(next,true);
  }
  void UpdateQueueBadge(){
   if(navItems.Count>3)navItems[3].Badge=saved.Queue.Count;
  }
  List<string> ParseLinks(string text){
   var list=new List<string>();
   if(String.IsNullOrEmpty(text))return list;
   foreach(string line in text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries)){
    string trimmed=line.Trim();
    if(trimmed.Length==0)continue;
    if(trimmed.IndexOf(' ')>=0||trimmed.IndexOf('\t')>=0){
     foreach(string part in trimmed.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries))AddLink(list,part);
    }else AddLink(list,trimmed);
   }
   return list;
  }
  void AddLink(List<string> list,string value){
   string url=value.Trim();
   Uri uri;
   if(!Uri.TryCreate(url,UriKind.Absolute,out uri))return;
   if(uri.Scheme!="http"&&uri.Scheme!="https")return;
   if(!list.Contains(url))list.Add(url);
  }
  void SaveSettings(){saved.Url=link.Text;saved.Target=destination.Text;saved.Connections=connections.Value;saved.Save(settings);}
  void RenderSpeed(){double value=displayedRate*(saved.SpeedInBits?8:1)/1000000d;speed.SetValue(value);speed.Suffix=saved.SpeedInBits?" Mb/s":" MB/s";speed.AccessibleName=saved.SpeedInBits?Lang.T("speed.bits"):Lang.T("speed.bytes");hints.SetToolTip(speed,saved.SpeedInBits?Lang.T("speed.bitsTip"):Lang.T("speed.bytesTip"));RefreshStatusStrip();}

  internal bool ReceiveDownload(string url,bool confirmed=false){
   LastDownloadDeclined=false;
   if(!Integration.ValidUrl(url)||closing)return false;
   if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Show();Activate();
   if(!confirmed&&!ConfirmDownload(url,engine.Running)){LastDownloadDeclined=true;return false;}
   if(engine.Running){if(saved.Queue.Count>=100)return false;if(!saved.Queue.Contains(url)&&url!=currentUrl){saved.Queue.Add(url);SaveSettings();RefreshTable();}ShowToast(Lang.T("toast.queued"),false);return true;}
   if(String.IsNullOrEmpty(saved.Folder)){saved.Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");}
   link.Text=url;SaveSettings();BeginInvoke(new Action(async()=>await Start(true)));return true;
  }
  bool ConfirmDownload(string url,bool queued){using(var dialog=new DownloadPrompt(url,DownloadEngine.FileName(url),queued,dark))return dialog.ShowDialog(this)==DialogResult.Yes;}

  void ShowToast(string message,bool danger){
   var toast=new Toast{Text=message,Danger=danger,Dark=dark,Width=340};
   toast.Show(message,danger);
   Controls.Add(toast);toast.BringToFront();
   toast.Location=new Point(Math.Max(10,(ClientSize.Width-340)/2),ClientSize.Height-150);
   toast.Anchor=AnchorStyles.Bottom;
  }

  void IntegrationSettings(){using(var dialog=new GlassForm{Text=Lang.T("integration.title"),ClientSize=new Size(600,420),MinimumSize=new Size(600,420),StartPosition=FormStartPosition.CenterParent,Font=Brand.Font(11),RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No}){
   var layout=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(24),RowCount=4};foreach(int height in new[]{48,140,55,55})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
   var startup=new CheckBox{Text=Lang.T("integration.startup"),Checked=Integration.StartupEnabled,Dock=DockStyle.Fill};startup.CheckedChanged+=(s,e)=>{try{Integration.StartupEnabled=startup.Checked;}catch(Exception ex){MessageBox.Show(ex.Message,Text);}};layout.Controls.Add(startup,0,0);
   layout.Controls.Add(Label(Lang.T("integration.help")),0,1);
   var folderButton=Button(Lang.T("action.extensionFolder"),0);folderButton.Width=200;folderButton.Dock=DockStyle.Right;folderButton.Click+=(s,e)=>Process.Start("explorer.exe","\""+Path.Combine(Path.GetDirectoryName(ReleaseInfo.InstalledPath),"BrowserExtension")+"\"");layout.Controls.Add(folderButton,0,2);
   var close=Button(Lang.T("action.close"),0);close.DialogResult=DialogResult.OK;close.Dock=DockStyle.Right;layout.Controls.Add(close,0,3);dialog.Controls.Add(layout);FdmTheme.Apply(dialog,dialog,dark);dialog.ShowDialog(this);
  }}

  void RefreshDownload() {
   if(engine.State==null) {if(engine.Running)status.Text=engine.Status;displayedRate=0;RenderSpeed();UpdateActiveRow();return;}
   long received=System.Threading.Interlocked.Read(ref engine.Received);
   double rate=downloadSpeed.Measure(System.Threading.Interlocked.Read(ref engine.SessionReceived));
   displayedRate=engine.Running?rate:0;RenderSpeed();
   previous=received;previousTime=DateTime.UtcNow;
   long size=engine.State.Size;
   progress.SetValue(size>0?(int)Math.Min(1000,received*1000.0/size):0);
   progress.SetIndeterminate(size<=0&&engine.Running);
   status.Text=engine.Status;
   RefreshStatusStrip();
   UpdateActiveRow();
  }

  void UpdateActiveRow(){
   DownloadItem item=BuildActiveItem();
   int index=-1;
   for(int n=0;n<tableItems.Count;n++)if(tableItems[n].Kind=="active"||tableItems[n].Kind=="paused"){index=n;break;}
   if(item==null){
    if(index>=0){tableItems.RemoveAt(index);history.SetItems(tableItems);history.ApplySort();}
    return;
   }
   if(searchTerm.Length>0&&item.Title.IndexOf(searchTerm,StringComparison.OrdinalIgnoreCase)<0){if(index>=0){tableItems.RemoveAt(index);history.SetItems(tableItems);}return;}
   if(index>=0){tableItems[index]=item;history.RefreshRows();return;}
   if(filter!="all"&&filter!="active")return;
   tableItems.Add(item);history.SetItems(tableItems);history.ApplySort();
  }

  void RefreshStatusStrip(){
   statusStrip.SpeedText=CompletedFile.PersianDigits((displayedRate*(saved.SpeedInBits?8:1)/1000000d).ToString("0.00",CultureInfo.InvariantCulture))+(saved.SpeedInBits?" Mb/s":" MB/s");
   statusStrip.BadgeText=CompletedFile.PersianDigits(engine.ActiveConnections.ToString())+" "+Lang.T("status.connections");
   statusStrip.Hint=engine.Running?engine.Status:!String.IsNullOrEmpty(downloadError)?downloadError:(String.IsNullOrEmpty(saved.Folder)?Lang.T("folder.incomplete"):saved.Folder??"");hints.SetToolTip(statusStrip,statusStrip.Hint);statusStrip.Invalidate();
  }

  class ColumnAnimator : IAnim {
   TableLayoutPanel host;int column;double from,to,elapsed,duration;
   public ColumnAnimator(TableLayoutPanel host,int column,int from,int to,double duration){this.host=host;this.column=column;this.from=from;this.to=to;this.duration=duration;}
   public void Step(double dt){
    elapsed+=dt;double p=Math.Min(1,duration<=0?1:elapsed/duration);double e=Ease.InOutCubic(p);
    host.ColumnStyles[column].Width=(int)Math.Round(from+(to-from)*e);
    host.PerformLayout();
   }
   public bool Finished {
    get { if(elapsed<duration)return false; host.ColumnStyles[column].Width=(int)to; host.PerformLayout(); return true; }
   }
  }
 }

 static class Program {
  [STAThread] static void Main(string[] args) {
   ServicePointManager.DefaultConnectionLimit=32;
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   try {
    if(args.Length>0 && args[0]=="--install") {Install();return;}
    if(args.Length>1 && args[0]=="--screenshot") {
     if(args.Length>2&&args[2]=="en")Lang.English=true;
     using(var form=new Window(false)) {
      if(args.Length>2&&args[2]=="dark"){
       typeof(Window).GetField("dark",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(form,true);
       typeof(Window).GetMethod("PaintTheme",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(form,new object[]{form});
      }
      form.Show();DateTime until=DateTime.UtcNow.AddMilliseconds(1800);while(DateTime.UtcNow<until){Application.DoEvents();System.Threading.Thread.Sleep(16);}
      using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(args[1]);}form.Close();}return;
    }
    if(args.Length>0 && args[0]=="--apply-update"){UpdateClient.Apply(args);return;}
    using(var instance=new System.Threading.Mutex(false,"Local\\AsanDownload.Desktop.v1")){
     bool owned=false;try{try{owned=instance.WaitOne(0);}catch(System.Threading.AbandonedMutexException){owned=true;}
      if(!owned){Integration.Deliver("activate");return;}
      if(!(args.Length>0&&args[0]=="--skip-update"))using(var update=new StartupUpdate()){update.ShowDialog();if(update.Relaunching||update.ExitRequested)return;}
      var window=new Window(true);window.Shown+=(s,e)=>Integration.Listen(window);Application.Run(window);
     }finally{if(owned)instance.ReleaseMutex();}
    }
   } catch(Exception ex) {
    if(args.Length>0&&args[0]=="--apply-update"){try{if(File.Exists(ReleaseInfo.InstalledPath))Process.Start(new ProcessStartInfo(ReleaseInfo.InstalledPath,"--skip-update"){UseShellExecute=true});}catch{}Environment.ExitCode=1;return;}
    if(Environment.GetEnvironmentVariable("MEHR_INSTALL_SILENT")=="1")File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),"InstallError.txt"),ex.ToString());
    else MessageBox.Show(ex.Message,Lang.T("app.name"));Environment.ExitCode=1;
   }
  }
  internal static void Install(bool copy=true) {
   string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","MehrDownload");
   Directory.CreateDirectory(dir);
   string exe=Path.Combine(dir,"MehrDownload.exe");
   if(copy&&!String.Equals(Application.ExecutablePath,exe,StringComparison.OrdinalIgnoreCase)) File.Copy(Application.ExecutablePath,exe,true);
   Integration.RegisterBrowser();
   using(var preferences=Registry.CurrentUser.CreateSubKey(@"Software\MehrDownload")){Integration.StartupEnabled=true;preferences.SetValue("StartupConfigured",1);}
   Type t=Type.GetTypeFromProgID("WScript.Shell");dynamic shell=Activator.CreateInstance(t);
   foreach(string path in new[]{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.Programs)}) {
    string old=Path.Combine(path,"Mehr Download.lnk");if(File.Exists(old))File.Delete(old);
    string previous=Path.Combine(path,"دانلود.lnk");if(File.Exists(previous))File.Delete(previous);
    string temporary=Path.Combine(path,"Download.lnk"),final=Path.Combine(path,Lang.English?"Asan Download.lnk":"آسان دانلود.lnk");
    dynamic shortcut=shell.CreateShortcut(temporary);shortcut.TargetPath=exe;shortcut.IconLocation=exe+",0";shortcut.WorkingDirectory=dir;shortcut.Description="Asan Download";shortcut.Save();
    if(File.Exists(final))File.Delete(final);File.Move(temporary,final);
   }
   string uninstall=Path.Combine(dir,"Uninstall.ps1");
   File.WriteAllText(uninstall,"$ErrorActionPreference = 'Stop'\r\nStop-Process -Name MehrDownload -ErrorAction SilentlyContinue\r\n$target = Join-Path $env:LOCALAPPDATA 'Programs\\MehrDownload'\r\nif ([IO.Path]::GetFullPath($target) -ne [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs\\MehrDownload'))) { throw 'Invalid path' }\r\nRemove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'آسان دانلود.lnk') -ErrorAction SilentlyContinue\r\nRemove-Item -LiteralPath (Join-Path ([Environment]::GetFolderPath('Programs')) 'آسان دانلود.lnk') -ErrorAction SilentlyContinue\r\nRemove-Item -LiteralPath 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\MehrDownload' -ErrorAction SilentlyContinue\r\nRemove-Item -LiteralPath $target -Recurse -Force\r\n",System.Text.Encoding.UTF8);
   using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MehrDownload")) {
    key.SetValue("DisplayName",Lang.English?"Asan Download":"آسان دانلود");key.SetValue("DisplayVersion",ReleaseInfo.Version);key.SetValue("Publisher","MehrNoor");key.SetValue("InstallLocation",dir);key.SetValue("DisplayIcon",exe);
    key.SetValue("UninstallString","powershell.exe -NoProfile -ExecutionPolicy Bypass -File \""+uninstall+"\"");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);
   }
   if(copy&&Environment.GetEnvironmentVariable("MEHR_INSTALL_SILENT")!="1") Process.Start(exe);
  }
 }
}
