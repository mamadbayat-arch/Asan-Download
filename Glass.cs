using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace MehrDownload {

 // ---------------------------- animation engine ----------------------------
 public static class Ease {
  public static double Clamp01(double v){ return v<0?0:(v>1?1:v); }
  public static double OutCubic(double t){ t=Clamp01(t); double u=1-t; return 1-u*u*u; }
  public static double OutQuint(double t){ t=Clamp01(t); double u=1-t; return 1-u*u*u*u*u; }
  public static double InOutCubic(double t){ t=Clamp01(t); return t<0.5?4.0*t*t*t:1.0-Math.Pow(-2.0*t+2.0,3.0)/2.0; }
  public static double OutBack(double t){ t=Clamp01(t); const double c1=1.70158,c3=2.70158; double u=t-1.0; return 1.0+c3*u*u*u+c1*u*u; }
  public static double Approach(double current,double target,double dt,double speed){ return current+(target-current)*(1.0-Math.Exp(-speed*dt)); }
 }

 public interface IAnim { void Step(double dt); bool Finished { get; } }

 // Single 60fps heartbeat shared by every animated control in the app.
 public static class Ticker {
  static readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
  static readonly List<IAnim> items=new List<IAnim>();
  static DateTime last=DateTime.UtcNow;
  static bool running;
  public static int Count { get { return items.Count; } }
  public static void Add(IAnim a){
   if(a==null)return;
   for(int i=0;i<items.Count;i++) if(object.ReferenceEquals(items[i],a))return;
   items.Add(a);
   if(!running){ running=true;last=DateTime.UtcNow;timer.Interval=16;timer.Tick+=OnTick;timer.Start(); }
  }
  public static void Remove(IAnim a){ for(int i=items.Count-1;i>=0;i--) if(object.ReferenceEquals(items[i],a))items.RemoveAt(i); if(items.Count==0&&running){timer.Stop();timer.Tick-=OnTick;running=false;} }
  static void OnTick(object sender,EventArgs e){
   DateTime now=DateTime.UtcNow; double dt=(now-last).TotalSeconds; last=now;
   if(dt>0.2)dt=0.2; if(dt<=0)return;
   foreach(IAnim a in items.ToArray()){
    if(!items.Contains(a))continue;
    try{ a.Step(dt);if(a.Finished)Remove(a); }catch{Remove(a);}
   }
   if(items.Count==0){ timer.Stop(); timer.Tick-=OnTick; running=false; }
  }
 }

 // Base class for controls that react to the pointer with eased motion.
 public abstract class AnimatedControl : Control, IAnim {
  protected double hover,press,enter=1,enterDelay;protected double pressTarget;
  protected double hoverTarget,enterTarget=1;
  bool registered;
  public AnimatedControl(){
   SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor|ControlStyles.ResizeRedraw,true);
   BackColor=Color.Transparent;
  }
  public double HoverAmount { get { return hover; } }
  public double PressAmount { get { return press; } }
  public double EnterAmount { get { return enter; } }
  public void AnimateHover(bool on){ hoverTarget=on?1:0; Ensure(); }
  public void AnimatePress(bool on){ pressTarget=on?1:0; Ensure(); }
  public void AnimateEnter(bool on){ enterTarget=on?1:0; Ensure(); }
  public void SetEnterDelay(double seconds){ enterDelay=seconds; }
  void Ensure(){ if(!registered){ registered=true;Ticker.Add(this);} }
  public virtual void Step(double dt){
   if(enterDelay>0){ enterDelay-=dt; return; }
   bool changed=false;
   double h=Ease.Approach(hover,hoverTarget,dt,14); if(Math.Abs(h-hover)>0.0005)changed=true; hover=h;
   double p=Ease.Approach(press,pressTarget,dt,20); if(Math.Abs(p-press)>0.0005)changed=true; press=p;
   double e=Ease.Approach(enter,enterTarget,dt,11); if(Math.Abs(e-enter)>0.0005)changed=true; enter=e;
   if(changed)Invalidate();
   if(Math.Abs(hoverTarget-hover)<0.002&&Math.Abs(pressTarget-press)<0.002&&Math.Abs(enterTarget-enter)<0.002){
    hover=hoverTarget;press=pressTarget;enter=enterTarget;registered=false;Invalidate();
   }
  }
  public virtual bool Finished { get { return IsDisposed||!registered; } }
  protected override void Dispose(bool disposing){if(disposing)Ticker.Remove(this);base.Dispose(disposing);}
  protected override void OnMouseCaptureChanged(EventArgs e){AnimatePress(false);base.OnMouseCaptureChanged(e);}
 }

 // ---------------------------- FDM palette ----------------------------
 public sealed class Palette {
  public bool IsDark;
  public Color Page,PageAlt,Surface,SurfaceAlt,SurfaceHi,Ink,InkSoft,Muted,Line,LineSoft;
  public Color Accent,Accent2,AccentInk,AccentSoft,Success,SuccessSoft,Warn,WarnSoft,Danger,DangerSoft,Track;
  public Color NavHover,NavActive,NavText,NavTextActive,Shadow;
  public Palette(bool dark){
   IsDark=dark;
   if(dark){
    Page=Color.FromArgb(0x0F,0x18,0x15); PageAlt=Color.FromArgb(0x0A,0x12,0x10);
    Surface=Color.FromArgb(0x16,0x21,0x1D); SurfaceAlt=Color.FromArgb(0x1A,0x26,0x22); SurfaceHi=Color.FromArgb(0x1C,0x2E,0x27);
    Ink=Color.FromArgb(0xE8,0xF0,0xEC); InkSoft=Color.FromArgb(0xA9,0xB8,0xB2); Muted=Color.FromArgb(0x9A,0xAE,0xA3);
    Line=Color.FromArgb(0x24,0x33,0x2E); LineSoft=Color.FromArgb(0x1B,0x28,0x23);
    Accent=Color.FromArgb(0x2F,0xB8,0x94); Accent2=Color.FromArgb(0x0E,0x8A,0x6C); AccentInk=Color.FromArgb(0x7C,0xD8,0xBC); AccentSoft=Color.FromArgb(0x10,0x29,0x1F);
    Success=Color.FromArgb(0x4A,0xDE,0x80); SuccessSoft=Color.FromArgb(0x11,0x2A,0x1C); Warn=Color.FromArgb(0xFB,0xBF,0x24); WarnSoft=Color.FromArgb(0x2C,0x21,0x08);
    Danger=Color.FromArgb(0xF8,0x71,0x71); DangerSoft=Color.FromArgb(0x2C,0x14,0x14); Track=Color.FromArgb(0x25,0x33,0x2E);
    NavHover=Color.FromArgb(0x1B,0x26,0x22); NavActive=Color.FromArgb(0x14,0x2B,0x24); NavText=Color.FromArgb(0x9E,0xAE,0xA6); NavTextActive=Color.FromArgb(0x7C,0xD8,0xBC);
    Shadow=Color.FromArgb(0,0,0,140);
   }else{
    Page=Color.FromArgb(0xFF,0xFF,0xFF); PageAlt=Color.FromArgb(0xE9,0xF2,0xEE);
    Surface=Color.White; SurfaceAlt=Color.FromArgb(0xF5,0xF9,0xF7); SurfaceHi=Color.FromArgb(0xE4,0xF4,0xEE);
    Ink=Color.FromArgb(0x17,0x22,0x36); InkSoft=Color.FromArgb(0x55,0x62,0x77); Muted=Color.FromArgb(0x62,0x73,0x82);
    Line=Color.FromArgb(0xDC,0xE7,0xE2); LineSoft=Color.FromArgb(0xEF,0xF4,0xF1);
    Accent=Color.FromArgb(0x10,0x7C,0x65); Accent2=Color.FromArgb(0x0A,0x66,0x52); AccentInk=Color.FromArgb(0x0B,0x5E,0x4C); AccentSoft=Color.FromArgb(0xE4,0xF4,0xEE);
    Success=Color.FromArgb(0x16,0xA3,0x4A); SuccessSoft=Color.FromArgb(0xE7,0xF7,0xEE); Warn=Color.FromArgb(0xD9,0x77,0x06); WarnSoft=Color.FromArgb(0xFD,0xF3,0xE2);
    Danger=Color.FromArgb(0xDC,0x26,0x26); DangerSoft=Color.FromArgb(0xFD,0xEC,0xEC); Track=Color.FromArgb(0xE2,0xEA,0xE7);
    NavHover=Color.FromArgb(0xEF,0xF6,0xF3); NavActive=Color.FromArgb(0xE0,0xF2,0xEB); NavText=Color.FromArgb(0x4C,0x5E,0x71); NavTextActive=Color.FromArgb(0x0B,0x5E,0x4C);
    Shadow=Color.FromArgb(19,42,62,26);
   }
  }
 }

 public static class Fdm {
  public static readonly Palette Light=new Palette(false);
  public static readonly Palette Dark=new Palette(true);
  public static Palette Of(bool dark){ return dark?Dark:Light; }
  public static Color Mix(Color a,Color b,double t){ t=Ease.Clamp01(t); return Color.FromArgb((int)(a.A+(b.A-a.A)*t),(int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t)); }
  public static Color Alpha(Color c,int a){ return Color.FromArgb(Math.Max(0,Math.Min(255,a)),c); }
  public static GraphicsPath Round(RectangleF r,float radius){
   var p=new GraphicsPath(); float d=Math.Min(radius*2,Math.Min(Math.Abs(r.Width),Math.Abs(r.Height)));
   if(d<=0){ p.AddRectangle(r); return p; }
   p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
  }
  public static void FillRound(Graphics g,RectangleF r,float radius,Color c){ using(var p=Round(r,radius)) using(var b=new SolidBrush(c)) g.FillPath(b,p); }
  public static void FillRound(Graphics g,RectangleF r,float radius,Color a,Color b,float angle){ using(var p=Round(r,radius)) using(var br=new LinearGradientBrush(r,a,b,angle)) g.FillPath(br,p); }
  public static void StrokeRound(Graphics g,RectangleF r,float radius,Color c,float width){ using(var p=Round(r,radius)) using(var pen=new Pen(c,width)) g.DrawPath(pen,p); }
  public static void Shadow(Graphics g,RectangleF r,float radius,Color color,int layers,float spread){
   for(int i=layers;i>=1;i--){
    float e=spread*i/(float)layers;
    var rr=RectangleF.Inflate(r,e,e); rr.Offset(0,e*0.28f);
    int a=(int)(color.A/(float)layers)+1;
    FillRound(g,rr,radius+e,Color.FromArgb(a,color));
   }
  }
  public static void Glow(Graphics g,RectangleF r,Color color){
   using(var p=new GraphicsPath()){ p.AddEllipse(r); using(var b=new PathGradientBrush(p)){ b.CenterColor=color; b.SurroundColors=new[]{Color.FromArgb(0,color)}; g.FillPath(b,p); } }
  }
  public static void Text(Graphics g,string text,Font font,Rectangle bounds,Color color,TextFormatFlags flags){ TextRenderer.DrawText(g,text??"",font,bounds,color,flags); }
 }

 // ---------------------------- brand ----------------------------
 static class Brand {
  static PrivateFontCollection fonts=new PrivateFontCollection();
  static bool loaded;
  static readonly Dictionary<string,Font> cache=new Dictionary<string,Font>();
  [DllImport("gdi32.dll")] static extern IntPtr AddFontMemResourceEx(IntPtr data,uint size,IntPtr reserved,ref uint count);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wparam,string text);
  public static void Cue(TextBox box,string text){ if(box!=null&&box.IsHandleCreated)SendMessage(box.Handle,0x1501,new IntPtr(1),text); }
  public static Font Font(float size,FontStyle style=FontStyle.Regular) {
   style=FontStyle.Regular;
   if(!loaded){
    foreach(string name in new[]{"Vazir-Regular.ttf"}){
     using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){
      if(stream==null)continue;
      byte[] bytes=new byte[stream.Length];int read=0;while(read<bytes.Length){int n=stream.Read(bytes,read,bytes.Length-read);if(n<=0)break;read+=n;}
      IntPtr ptr=Marshal.AllocCoTaskMem(bytes.Length);Marshal.Copy(bytes,0,ptr,bytes.Length);uint count=0;AddFontMemResourceEx(ptr,(uint)bytes.Length,IntPtr.Zero,ref count);fonts.AddMemoryFont(ptr,bytes.Length);Marshal.FreeCoTaskMem(ptr);
     }
    }
    loaded=true;
   }
   float actual=size*Lang.Scale;
   string key=actual.ToString("0.###")+"|"+style;
   Font cached;
   if(cache.TryGetValue(key,out cached))return cached;
   Font created;
   try{ created=new Font(fonts.Families[0],actual,style); }catch{ created=new Font("Segoe UI",actual,style); }
   cache[key]=created; bases[created]=size; return created;
  }
  static readonly Dictionary<Font,float> bases=new Dictionary<Font,float>();
  // Re-creates every font of a tree with the size of the currently selected language.
  public static void Rescale(Control root){
   float baseSize;
   if(root.Font!=null&&bases.TryGetValue(root.Font,out baseSize))root.Font=Font(baseSize,root.Font.Style);
   foreach(Control child in root.Controls)Rescale(child);
  }
  public static GraphicsPath Round(RectangleF r,float radius){ return Fdm.Round(r,radius); }
  public static void Logo(Graphics g,Rectangle r) {
   g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var p=Round(r,r.Width*.28f))using(var b=new LinearGradientBrush(r,Color.FromArgb(0x19,0x92,0x78),Color.FromArgb(0x17,0x22,0x36),60f)){g.FillPath(b,p);using(var pen=new Pen(Color.FromArgb(170,255,255,255),Math.Max(1,r.Width/80f)))g.DrawPath(pen,p);}
   var old=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(r.Width/100f,r.Height/100f);
   using(var p=new GraphicsPath()){
    p.AddLines(new[]{new PointF(43,18),new PointF(57,18),new PointF(57,46),new PointF(70,35),new PointF(80,47),new PointF(50,73),new PointF(20,47),new PointF(30,35),new PointF(43,46)});p.CloseFigure();
    using(var b=new SolidBrush(Color.White))g.FillPath(b,p);
   }
   using(var pen=new Pen(Color.FromArgb(220,255,255,255),6)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLine(pen,26,83,74,83);}
   g.Restore(old);
  }
 }

 class LogoView : Control {
  public LogoView(){SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;Size=new Size(48,48);AccessibleName=Lang.T("logo.name");}
  protected override void OnPaint(PaintEventArgs e){Brand.Logo(e.Graphics,new Rectangle(2,2,Width-4,Height-4));}
 }

 // ---------------------------- window chrome ----------------------------
 public class GlassForm : Form {
  public bool GlassDark;
  internal TitleBar titleBar;
  int visualUpdates;bool redrawSuspended;
  readonly List<Control> suspendedControls=new List<Control>();
  [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
  protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x02000000;return p;}}
  internal void BeginVisualUpdate(){
   if(visualUpdates++!=0)return;
   redrawSuspended=IsHandleCreated&&Visible;
   if(redrawSuspended)SendMessage(Handle,0x000B,IntPtr.Zero,IntPtr.Zero);
   SuspendTree(this);
  }
  void SuspendTree(Control root){root.SuspendLayout();suspendedControls.Add(root);foreach(Control child in root.Controls)SuspendTree(child);}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);if(visualUpdates>0&&redrawSuspended)SendMessage(Handle,0x000B,IntPtr.Zero,IntPtr.Zero);}
  internal void EndVisualUpdate(){
   if(visualUpdates==0||--visualUpdates!=0)return;
   try{for(int n=suspendedControls.Count-1;n>=0;n--)if(!suspendedControls[n].IsDisposed)suspendedControls[n].ResumeLayout(true);}
   finally{suspendedControls.Clear();if(redrawSuspended&&IsHandleCreated)SendMessage(Handle,0x000B,new IntPtr(1),IntPtr.Zero);redrawSuspended=false;Invalidate(true);}
  }
  VeilAnim fadeAnimation;
  public GlassForm(){
   DoubleBuffered=true;ResizeRedraw=true;FormBorderStyle=FormBorderStyle.None;
   Padding=new Padding(0,46,0,0);
   RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;
   titleBar=new TitleBar(this);Controls.Add(titleBar);
  }
  protected override void OnLayout(LayoutEventArgs e){
   base.OnLayout(e);
   if(titleBar!=null){ var bounds=new Rectangle(0,0,Math.Max(1,ClientSize.Width),46);if(titleBar.Bounds!=bounds)titleBar.Bounds=bounds; }
  }
  protected override void OnResize(EventArgs e){ base.OnResize(e); if(titleBar!=null)titleBar.UpdateRestore(); }
  internal void ToggleMaximized(){MaximizedBounds=Screen.FromControl(this).WorkingArea;WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;}
  protected override void WndProc(ref Message m){
   const int hitTest=0x84;
   if(m.Msg==hitTest && WindowState==FormWindowState.Normal){
    long position=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(position&0xffff),(short)((position>>16)&0xffff)));int edge=7;bool left=p.X<edge,right=p.X>=ClientSize.Width-edge,top=p.Y<edge,bottom=p.Y>=ClientSize.Height-edge;
    int hit=left?(top?13:bottom?16:10):right?(top?14:bottom?17:11):top?12:bottom?15:0;
    if(hit!=0){m.Result=new IntPtr(hit);return;}
   }
   base.WndProc(ref m);
  }
  protected override void OnPaintBackground(PaintEventArgs e){
   var palette=Fdm.Of(GlassDark);
   using(var brush=new SolidBrush(palette.Page))e.Graphics.FillRectangle(brush,e.ClipRectangle);
  }
  protected override void Dispose(bool disposing){ if(disposing){if(fadeAnimation!=null){Ticker.Remove(fadeAnimation);fadeAnimation.Cancel();fadeAnimation=null;}} base.Dispose(disposing); }
  void PaintBackdrop(Graphics g){
   var palette=Fdm.Of(GlassDark);
   g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var b=new LinearGradientBrush(new Rectangle(0,0,Math.Max(1,Width),Math.Max(1,Height)),palette.Page,palette.PageAlt,90f))g.FillRectangle(b,new Rectangle(0,0,Width,Height));
   Fdm.Glow(g,new RectangleF(-260,-320,760,760),Color.FromArgb(GlassDark?26:40,0x10,0x7C,0x65));
   Fdm.Glow(g,new RectangleF(Width-520,Height-460,720,720),Color.FromArgb(GlassDark?22:26,0x2F,0xB8,0x94));
   using(var b=new SolidBrush(Color.FromArgb(GlassDark?235:250,palette.Surface)))g.FillRectangle(b,new Rectangle(0,0,Width,46));
   using(var pen=new Pen(palette.Line,1f))g.DrawLine(pen,0,45.5f,Width,45.5f);
  }
  // Fades a translucent veil over the window, used for entrance and theme transitions.
  public void Fade(Color tint,int from,int to,double seconds,Action midway){
   if(fadeAnimation!=null){Ticker.Remove(fadeAnimation);fadeAnimation.Cancel();}
   if(!SystemInformation.UIEffectsEnabled){if(midway!=null)midway();return;}
   var veil=new Veil{Tint=tint,Alpha=(byte)Math.Max(0,Math.Min(255,from)),Dock=DockStyle.None,Enabled=false,TabStop=false};
   veil.Bounds=ClientRectangle;
   Controls.Add(veil);veil.BringToFront();
   fadeAnimation=new VeilAnim(veil,from,to,seconds,()=>{ if(midway!=null)midway(); });
   Ticker.Add(fadeAnimation);
  }
  class VeilAnim : IAnim {
   Veil veil;double from,to,elapsed,duration;Action midway;bool fired;
   public VeilAnim(Veil v,int f,int t,double seconds,Action m){veil=v;from=f;to=t;duration=seconds;midway=m;}
   public void Step(double dt){
    if(veil.IsDisposed){elapsed=duration;return;}
    elapsed+=dt;double p=duration<=0?1:Ease.Clamp01(elapsed/duration);double e=Ease.OutCubic(p);
    veil.Alpha=(byte)Math.Max(0,Math.Min(255,from+(to-from)*e));
    if(veil.Parent!=null)veil.Bounds=new Rectangle(0,0,veil.Parent.ClientSize.Width,veil.Parent.ClientSize.Height);
    veil.Invalidate();
    if(!fired&&p>=0.45){fired=true;if(midway!=null)midway();}
   }
   public void Cancel(){if(!veil.IsDisposed)veil.Dispose();}
   public bool Finished { get { if(elapsed<duration&&!veil.IsDisposed)return false;Cancel();return true; } }
  }
 }

 public class Veil : Control {
  public Color Tint=Color.White;public byte Alpha=255;
  public Veil(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;}
  protected override void OnPaint(PaintEventArgs e){ using(var b=new SolidBrush(Color.FromArgb(Alpha,Tint)))e.Graphics.FillRectangle(b,ClientRectangle); }
 }

 class SmoothTableLayout : TableLayoutPanel { public SmoothTableLayout(){DoubleBuffered=true;RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;} }
 class CenteredActionRow : FlowLayoutPanel {
  bool arranging;
  public CenteredActionRow(){Dock=DockStyle.Fill;WrapContents=false;RightToLeft=RightToLeft.No;BackColor=Color.Transparent;FlowDirection=Lang.Rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight;DoubleBuffered=true;}
  protected override void OnLayout(LayoutEventArgs e){
   if(arranging)return;
   arranging=true;
   try{
    int width=0,height=0;foreach(Control child in Controls)if(child.Visible){width+=child.Width+child.Margin.Horizontal;height=Math.Max(height,child.Height+child.Margin.Vertical);}
    int x=Math.Max(0,(ClientSize.Width-width)/2),y=Math.Max(0,(ClientSize.Height-height)/2);
    var padding=new Padding(x,y,x,y);if(Padding!=padding)Padding=padding;
    base.OnLayout(e);
   }finally{arranging=false;}
  }
 }

 class WindowActionButton : AnimatedControl {
  public int Glyph;public bool Restored;
  public WindowActionButton(int glyph){
   Glyph=glyph;RightToLeft=RightToLeft.No;TabStop=true;Size=new Size(46,34);
   SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
   BackColor=Color.Transparent;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.PushButton;
  }
  protected override void OnMouseEnter(EventArgs e){AnimateHover(true);base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){AnimateHover(false);base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){AnimatePress(true);base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){AnimatePress(false);base.OnMouseUp(e);}
  public void PerformClick(){ if(Enabled)OnClick(EventArgs.Empty); }
  protected override bool IsInputKey(Keys key){return key==Keys.Space||key==Keys.Enter||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){e.Handled=true;e.SuppressKeyPress=true;}base.OnKeyDown(e);}
  protected override void OnKeyUp(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){PerformClick();e.Handled=true;}base.OnKeyUp(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   GlassForm form=FindForm() as GlassForm; bool dark=form!=null&&form.GlassDark;
   var palette=Fdm.Of(dark);
   Color ink=dark?Color.FromArgb(0xC7,0xD4,0xE0):Color.FromArgb(0x3C,0x4B,0x5A);
   Color fill=Color.Transparent;
   if(Glyph==2) fill=Color.FromArgb((int)(hover*235),0xE5,0x4B,0x4B);
   else fill=Color.FromArgb((int)(hover*40),palette.Ink);
   if(Glyph==2&&hover>0)ink=Color.White;
   var r=new RectangleF(4,4,Width-8,Height-8);
   if(fill.A>0)Fdm.FillRound(g,r,9,fill);
   float x=Width/2f,y=Height/2f;
   using(var pen=new Pen(ink,1.6f)){pen.StartCap=pen.EndCap=LineCap.Round;
    if(Glyph==0)g.DrawLine(pen,x-6,y+3,x+6,y+3);
    else if(Glyph==2){g.DrawLine(pen,x-5,y-5,x+5,y+5);g.DrawLine(pen,x+5,y-5,x-5,y+5);}
    else if(Restored){g.DrawLines(pen,new[]{new PointF(x-3,y-6),new PointF(x+6,y-6),new PointF(x+6,y+3)});g.DrawRectangle(pen,x-6,y-3,9,9);}
    else g.DrawRectangle(pen,x-5,y-5,10,10);
   }
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-5,-5),ink,palette.Surface);
  }
 }

 class TitleBar : Panel {
  GlassForm owner;WindowActionButton minimize,maximize,close;
  [DllImport("user32.dll")]static extern bool ReleaseCapture();
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr handle,int message,IntPtr wparam,IntPtr lparam);
  public TitleBar(GlassForm form){
   owner=form;RightToLeft=RightToLeft.No;SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=Color.Transparent;
   minimize=new WindowActionButton(0){AccessibleName=Lang.T("win.minimize")};maximize=new WindowActionButton(1){AccessibleName=Lang.T("win.maximize")};close=new WindowActionButton(2){AccessibleName=Lang.T("win.close")};Controls.Add(minimize);Controls.Add(maximize);Controls.Add(close);
   minimize.Click+=(s,e)=>owner.WindowState=FormWindowState.Minimized;maximize.Click+=(s,e)=>Toggle();close.Click+=(s,e)=>owner.Close();
   MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(owner.Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};MouseDoubleClick+=(s,e)=>{if(e.Button==MouseButtons.Left)Toggle();};
  }
  void Toggle(){owner.ToggleMaximized();}
  public void UpdateRestore(){maximize.Restored=owner.WindowState==FormWindowState.Maximized;maximize.AccessibleName=maximize.Restored?Lang.T("win.restore"):Lang.T("win.maximize");maximize.Invalidate();}
  internal void ApplyLanguage(){close.AccessibleName=Lang.T("win.close");minimize.AccessibleName=Lang.T("win.minimize");UpdateRestore();PerformLayout();Invalidate();}
  protected override void OnLayout(LayoutEventArgs e){
   base.OnLayout(e);
   if(close==null)return;
   if(Lang.Rtl){ close.Location=new Point(10,6);maximize.Location=new Point(58,6);minimize.Location=new Point(106,6); }
   else{ close.Location=new Point(Width-56,6);maximize.Location=new Point(Width-104,6);minimize.Location=new Point(Width-152,6); }
  }
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(owner.GlassDark);
   if(Lang.Rtl)Brand.Logo(g,new Rectangle(Width-38,11,24,24));
   else Brand.Logo(g,new Rectangle(14,11,24,24));
   TextRenderer.DrawText(g,owner.Text,Brand.Font(12f,FontStyle.Regular),new Rectangle(160,0,Math.Max(1,Width-320),Height),palette.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|(Lang.Rtl?TextFormatFlags.RightToLeft:0));
  }
 }

 // ---------------------------- surfaces ----------------------------
 public class GlassPanel : Panel {
  public bool Dark;public int Radius=14;public Color Tint=Color.Empty;
  public bool Elevated=true;public bool Interactive=false;
  public GlassPanel(){
   SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
   BackColor=Color.Transparent;ResizeRedraw=true;Padding=new Padding(16);
  }
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var r=new RectangleF(1,1,Width-3,Height-3);if(r.Width<2||r.Height<2)return;
   var palette=Fdm.Of(Dark);
   if(Elevated)Fdm.Shadow(g,r,Radius,palette.Shadow,4,3.5f);
   Color fill=!Tint.IsEmpty?Tint:palette.Surface;
   Fdm.FillRound(g,r,Radius,fill);
   using(var pen=new Pen(palette.Line,1f))using(var p=Fdm.Round(r,Radius))g.DrawPath(pen,p);
  }
 }

 public class GlassButton : AnimatedControl, IButtonControl {
  public bool Dark,Primary,Ghost;
  public int Glyph;
  public bool GlyphTrailing;
  public float Radius=12;
  public ContextMenuStrip Menu;
  public DialogResult DialogResult {get;set;}
  public GlassButton(){
   SetStyle(ControlStyles.Selectable|ControlStyles.StandardClick,true);
   Cursor=Cursors.Hand;Font=Brand.Font(11);TabStop=true;RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;AccessibleRole=AccessibleRole.PushButton;
  }
  public void NotifyDefault(bool value){Invalidate();}
  public void PerformClick(){if(Enabled)OnClick(EventArgs.Empty);}
  protected override void OnClick(EventArgs e){
   if(Menu!=null){Menu.Show(this,new Point(Lang.Rtl?Width:0,Height),Lang.Rtl?ToolStripDropDownDirection.BelowLeft:ToolStripDropDownDirection.BelowRight);return;}
   base.OnClick(e);var form=FindForm();if(form!=null&&DialogResult!=DialogResult.None)form.DialogResult=DialogResult;
  }
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){press=0;pressTarget=0;base.OnLostFocus(e);Invalidate();}
  protected override bool IsInputKey(Keys key){return key==Keys.Space||key==Keys.Enter||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){AnimatePress(true);e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnKeyUp(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){AnimatePress(false);PerformClick();e.Handled=true;}base.OnKeyUp(e);}
  protected override void OnMouseEnter(EventArgs e){AnimateHover(true);base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){AnimateHover(false);AnimatePress(false);base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){AnimatePress(true);base.OnMouseDown(e);}
  protected override void OnMouseUp(MouseEventArgs e){AnimatePress(false);base.OnMouseUp(e);}
  protected override void OnEnabledChanged(EventArgs e){Invalidate();base.OnEnabledChanged(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   if(Width<2||Height<2)return;
   float lift=(float)(hover*2.0-press*1.0);
   var r=new RectangleF(2,2,Width-5,Height-6);
   var state=g.Save();
   g.TranslateTransform(0,-lift);
   Color ink=!Enabled?(Dark?Color.FromArgb(0x5E,0x6B,0x78):Color.FromArgb(0xA9,0xB4,0xBF)):palette.Ink;
   if(Primary){
    if(!Enabled)Fdm.FillRound(g,r,Radius,palette.Track);
    else{
     int glow=(int)(hover*70);
     if(glow>0)Fdm.Shadow(g,r,Radius,Color.FromArgb(glow,palette.Accent),4,4f);
     Color primary=Dark?Color.FromArgb(0x0A,0x78,0x60):palette.Accent;
     Fdm.FillRound(g,r,Radius,primary,Dark?Fdm.Mix(primary,Color.Black,0.12):palette.Accent2,55f);
     ink=Color.White;
    }
   }else if(Ghost){
    if(hover>0.01)Fdm.FillRound(g,r,Radius,Color.FromArgb((int)(hover*30),palette.Accent));
   }else{
    Color baseFill=Dark?palette.SurfaceAlt:palette.Surface;
    if(!Enabled)baseFill=Dark?Color.FromArgb(0x1A,0x22,0x2B):Color.FromArgb(0xF2,0xF5,0xF8);
    else if(hover>0.01)baseFill=Fdm.Mix(baseFill,palette.AccentSoft,(float)(hover*0.9));
    Fdm.FillRound(g,r,Radius,baseFill);
    Color border=!Enabled?palette.LineSoft:palette.Line;
    if(Enabled&&hover>0.01)border=Fdm.Mix(border,palette.Accent,(float)hover);
    using(var pen=new Pen(border,1f))using(var p=Fdm.Round(r,Radius))g.DrawPath(pen,p);
   }
   bool alone=Glyph!=0&&String.IsNullOrEmpty(Text);
   int glyphInset=alone?0:40;
   bool glyphOnLeft=GlyphTrailing?Lang.Rtl:!Lang.Rtl;
   int lead=Glyph!=0&&glyphOnLeft&&!alone?glyphInset:10;
   int trail=Glyph!=0&&!glyphOnLeft&&!alone?glyphInset:10;
   var textBounds=new Rectangle(lead,(int)r.Top,Math.Max(1,Width-lead-trail),(int)r.Height);
   TextRenderer.DrawText(g,Text,Font,textBounds,ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding|(Lang.Rtl?TextFormatFlags.RightToLeft:0));
   if(Glyph!=0){
    float x=alone?Width/2f:(GlyphTrailing?(Lang.Rtl?22:Width-22):(Lang.Rtl?Width-22:22)),y=r.Top+r.Height/2f;
    Color pen=Primary?Color.White:palette.AccentInk;
    if(!Enabled)pen=Dark?Color.FromArgb(0x5E,0x6B,0x78):Color.FromArgb(0xA9,0xB4,0xBF);
    DrawGlyph(g,pen,x,y);
   }
   g.Restore(state);
   if(Focused&&ShowFocusCues){using(var pen=new Pen(Color.FromArgb(120,palette.Accent),1.6f))g.DrawRectangle(pen,3,3,Width-7,Height-7);}
  }
  void DrawGlyph(Graphics g,Color ink,float x,float y){Icons.Draw(g,Icons.Button(Glyph),x,y,ink,20);}
 }

 public class GlassProgress : AnimatedControl {
  public bool Dark;
  public int Maximum=1000;public int Value;public ProgressBarStyle Style;
  double shown;double target;double shimmer;double sweep;
  public GlassProgress(){
   TabStop=false;
   SetStyle(ControlStyles.SupportsTransparentBackColor|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
   BackColor=Color.Transparent;
  }
  public void SetValue(int value){ Value=value; target=Maximum<=0?0:Ease.Clamp01(value/(double)Math.Max(1,Maximum)); Ticker.Add(this); }
  public void SetIndeterminate(bool on){ Style=on?ProgressBarStyle.Marquee:ProgressBarStyle.Continuous; Ticker.Add(this); }
  public override void Step(double dt){
   double before=shown;
   shown=Ease.Approach(shown,target,dt,9);
   if(Style==ProgressBarStyle.Marquee) sweep=(sweep+dt*0.55)%1.0;
   else shimmer=(shimmer+dt*0.42)%1.0;
   if(Math.Abs(shown-before)>0.0004||Style==ProgressBarStyle.Marquee)Invalidate();
   if(Math.Abs(target-shown)<0.0015&&Style!=ProgressBarStyle.Marquee) shown=target;
  }
  public override bool Finished { get { return IsDisposed||(Style!=ProgressBarStyle.Marquee && Math.Abs(target-shown)<0.0015); } }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   int h=Math.Max(6,Height-6);
   var track=new RectangleF(0,3,Width,h);
   Fdm.FillRound(g,track,h/2f,palette.Track);
   if(Style==ProgressBarStyle.Marquee){
    float band=Math.Max(28f,Width*0.28f);
    float x=(float)((sweep*(Width+band))-band);
    var fill=new RectangleF(x,3,band,h);
    Fdm.FillRound(g,fill,h/2f,Fdm.Mix(palette.Accent,palette.Accent2,0.4f),palette.Accent2,0f);
   }else if(shown>0.001){
    float w=(float)Math.Max(h,Width*shown);
    var fill=new RectangleF(Width-w,3,w,h);
    Fdm.FillRound(g,fill,h/2f,palette.Accent,palette.Accent2,0f);
    if(shown>0.06){
     float sw=(float)Math.Max(24,Width*0.18);
     float sx=(float)((shimmer*(Width+sw))-sw);
     var hl=new RectangleF(Math.Max(Width-w,sx),3,Math.Min(sw,w-(Width-w)),h);
     if(hl.Width>4){
      using(var path=Fdm.Round(hl,h/2f)){
       using(var b=new LinearGradientBrush(hl,Color.FromArgb(0,255,255,255),Color.FromArgb(120,255,255,255),0f))g.FillPath(b,path);
      }
     }
    }
   }
  }
 }
}
