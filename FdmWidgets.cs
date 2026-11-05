using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MehrDownload {

 // Shared column geometry for the download table (RTL: first column sits at the right edge).
 // Walks a control tree and applies the FDM palette to every custom widget.
 public static class FdmTheme {
  public static void Apply(GlassForm form,Control root,bool isDark){
   if(form!=null)form.GlassDark=isDark;
   var palette=Fdm.Of(isDark);
   root.ForeColor=palette.Ink;
   if(root is TextBox||root is NumericUpDown||root is ComboBox)root.BackColor=palette.Surface;
   else if(root is Panel||root is Label||root is LogoView)root.BackColor=Color.Transparent;
   if(root is GlassPanel)((GlassPanel)root).Dark=isDark;
   if(root is GlassButton)((GlassButton)root).Dark=isDark;
   if(root is GlassProgress)((GlassProgress)root).Dark=isDark;
   if(root is NavItem)((NavItem)root).Dark=isDark;
   if(root is StatTile)((StatTile)root).Dark=isDark;
   if(root is SearchField)((SearchField)root).Dark=isDark;
   if(root is Segmented)((Segmented)root).Dark=isDark;
   if(root is DownloadTable)((DownloadTable)root).Dark=isDark;
   if(root is DownloadRowView)((DownloadRowView)root).Dark=isDark;
   if(root is DropZone)((DropZone)root).Dark=isDark;
   if(root is Toast)((Toast)root).Dark=isDark;
   if(root is StatusStrip)((StatusStrip)root).Dark=isDark;
   if(root is ConnectionStepper)((ConnectionStepper)root).Dark=isDark;
   var button=root as GlassButton;if(button!=null&&button.Menu!=null)ApplyMenu(button.Menu,isDark);
   var table=root as DownloadTable;if(table!=null){if(table.Menu!=null)ApplyMenu(table.Menu,isDark);table.RefreshRows();}
   foreach(Control child in root.Controls)Apply(form,child,isDark);
  }
  public static void ApplyMenu(ContextMenuStrip menu,bool dark){menu.BackColor=Fdm.Of(dark).Surface;menu.ForeColor=Fdm.Of(dark).Ink;menu.Renderer=new ToolStripProfessionalRenderer(new MenuColors(dark));}
  class MenuColors : ProfessionalColorTable {
   Palette palette;public MenuColors(bool dark){palette=Fdm.Of(dark);UseSystemColors=false;}
   public override Color ToolStripDropDownBackground {get{return palette.Surface;}}
   public override Color ImageMarginGradientBegin {get{return palette.Surface;}}
   public override Color ImageMarginGradientMiddle {get{return palette.Surface;}}
   public override Color ImageMarginGradientEnd {get{return palette.Surface;}}
   public override Color MenuItemSelected {get{return palette.AccentSoft;}}
   public override Color MenuItemBorder {get{return palette.Accent;}}
   public override Color MenuBorder {get{return palette.Line;}}
  }
 }

 public static class FdmGrid {
  public const int Icon=44, Size=92, Status=210, Speed=104, Date=120;
  public static Rectangle[] Columns(int width){
   int available=Math.Max(1,width-32);
   int size=width>=760?Size:0;
   int speed=width>=640?Speed:0;
   int date=width>=900?Date:0;
   int status=width>=520?Status:160;
   int fixedWidth=Icon+size+status+speed+date;
   int name=Math.Max(1,available-fixedWidth);
   var cells=new Rectangle[6];
   bool rtl=Lang.Rtl;
   int edge=rtl?width:0;
   int step=rtl?-1:1;
   cells[0]=new Rectangle(rtl?edge-Icon:edge,0,Icon,1); edge+=step*Icon;
   cells[1]=new Rectangle(rtl?edge-name:edge,0,name,1); edge+=step*name;
   cells[2]=new Rectangle(rtl?edge-size:edge,0,size,1); edge+=step*size;
   cells[3]=new Rectangle(rtl?edge-status:edge,0,status,1); edge+=step*status;
   cells[4]=new Rectangle(rtl?edge-speed:edge,0,speed,1); edge+=step*speed;
   cells[5]=new Rectangle(rtl?edge-date:edge,0,date,1);
   return cells;
  }
 }

 public sealed class DownloadItem {
  public string Title="",SizeText="",SpeedText="",DateText="",StatusText="",Kind="completed",Path="";
  public double Progress;
  public bool Indeterminate;
  public long Order,Bytes;
  public object Tag;
 }

 // ---------------------------- sidebar navigation ----------------------------
 public class NavItem : AnimatedControl {
  string caption="";public string Caption {get{return caption;}set{caption=value;AccessibleName=value;Invalidate();}}public int Glyph;public bool Active;public int Badge=-1;
  bool dark;
  public NavItem(){Height=40;TabStop=true;Cursor=Cursors.Hand;RightToLeft=RightToLeft.No;AccessibleRole=AccessibleRole.PushButton;}
  public void PerformClick(){ if(Enabled)OnClick(EventArgs.Empty); }
  protected override bool IsInputKey(Keys key){return key==Keys.Space||key==Keys.Enter||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){e.Handled=true;e.SuppressKeyPress=true;}base.OnKeyDown(e);}
  protected override void OnKeyUp(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){PerformClick();e.Handled=true;}base.OnKeyUp(e);}
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);AnimatePress(false);Invalidate();}
  public bool Dark { get { return dark; } set { dark=value; Invalidate(); } }
  protected override void OnMouseEnter(EventArgs e){AnimateHover(true);base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){AnimateHover(false);base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){AnimatePress(true);base.OnMouseDown(e);}
  bool DotsHit(Rectangle[] cells,int x){
   if(cells[5].Width<=0)return false;
   return Lang.Rtl?(x>=cells[5].X-6&&x<cells[5].X+24):(x>=cells[5].X+cells[5].Width-26&&x<cells[5].X+cells[5].Width+4);
  }
  protected override void OnMouseUp(MouseEventArgs e){AnimatePress(false);base.OnMouseUp(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(dark);
   float shift=(float)(1-enter)*26f;
   g.TranslateTransform(shift,0);
   var r=new RectangleF(6,2,Width-12,Height-4);
   if(Active){
    Fdm.FillRound(g,r,10,palette.NavActive);
    using(var pen=new Pen(Color.FromArgb(70,palette.Accent),1f))using(var p=Fdm.Round(r,10))g.DrawPath(pen,p);
    using(var b=new SolidBrush(palette.Accent))g.FillRectangle(b,Lang.Rtl?Width-4:1,10,3,Height-20);
   }else if(hover>0.01){
    Fdm.FillRound(g,r,10,Color.FromArgb((int)(hover*255),palette.NavHover));
   }
   Color ink=Active?palette.NavTextActive:palette.NavText;
   bool narrow=Width<130;
   float gx=narrow?Width/2f:(Lang.Rtl?Width-26:26),gy=Height/2f;
   using(var pen=new Pen(ink,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
    switch(Glyph){
     case 0:g.DrawEllipse(pen,gx-6,gy-6,12,12);g.DrawLine(pen,gx-1,gy,gx+6,gy);break;
     case 1:g.DrawLine(pen,gx-6,gy,gx+6,gy);g.DrawLine(pen,gx,gy-6,gx,gy+6);break;
     case 2:g.DrawEllipse(pen,gx-6,gy-6,12,12);g.DrawLines(pen,new[]{new PointF(gx-3,gy),new PointF(gx-1,gy+3),new PointF(gx+4,gy-3)});break;
     case 3:g.DrawRectangle(pen,gx-6,gy-5,12,10);g.DrawLine(pen,gx-3,gy-1,gx+3,gy-1);g.DrawLine(pen,gx-3,gy+2,gx+2,gy+2);break;
     case 4:g.DrawEllipse(pen,gx-6,gy-6,12,12);g.DrawArc(pen,gx-6,gy-6,12,12,-40,260);break;
     case 5:g.DrawRectangle(pen,gx-6,gy-6,12,12);g.DrawLine(pen,gx-4,gy-1,gx+4,gy-1);g.DrawLine(pen,gx-4,gy+2,gx+2,gy+2);break;
     case 6:g.DrawEllipse(pen,gx-6,gy-6,12,12);g.DrawLine(pen,gx,gy-4,gx,gy);g.DrawLine(pen,gx,gy,gx+3,gy+1);break;
     case 7:g.DrawRectangle(pen,gx-7,gy-5,14,11);g.DrawLine(pen,gx,gy+6,gx,gy+8);g.DrawLine(pen,gx-4,gy+8,gx+4,gy+8);break;
     case 8:g.DrawEllipse(pen,gx-4,gy-4,8,8);g.DrawLine(pen,gx+3,gy+3,gx+8,gy+8);break;
     case 9:g.DrawArc(pen,gx-6,gy-6,12,12,-90,180);g.DrawLine(pen,gx,gy-6,gx,gy+6);break;
     case 10:g.DrawEllipse(pen,gx-6,gy-6,12,12);g.DrawLine(pen,gx-2,gy-4,gx-2,gy+4);g.DrawLine(pen,gx+2,gy-4,gx+2,gy+4);break;
     case 11:g.DrawRectangle(pen,gx-6,gy-6,12,12);g.DrawLine(pen,gx-4,gy,gx+4,gy);break;
     case 12:g.DrawEllipse(pen,gx-7,gy-7,14,14);g.DrawEllipse(pen,gx-3,gy-7,6,14);g.DrawLine(pen,gx-7,gy,gx+7,gy);break;
    }
   }
   Color textInk=Active?palette.NavTextActive:palette.NavText;
   if(!narrow){
    int reserve=Badge>=0?40:0;
    var textRect=Lang.Rtl?new Rectangle(16+reserve,0,Math.Max(10,Width-56-reserve),Height):new Rectangle(46,0,Math.Max(10,Width-62-reserve),Height);
    TextRenderer.DrawText(g,Caption,Brand.Font(10.5f,Active?FontStyle.Bold:FontStyle.Regular),textRect,textInk,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
   }
   if(Badge>=0&&!narrow){
    string badge=CompletedFile.PersianDigits(Badge.ToString());
    int w=Math.Max(24,badge.Length*9+14);
    var badgeRect=Lang.Rtl?new RectangleF(14,Height/2f-10,w,20):new RectangleF(Width-14-w,Height/2f-10,w,20);
    Fdm.FillRound(g,badgeRect,10,Active?palette.Accent:palette.Track);
    TextRenderer.DrawText(g,badge,Brand.Font(9,FontStyle.Bold),Rectangle.Round(badgeRect),Active?Color.White:palette.InkSoft,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
   }
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-6,-4),palette.Accent,palette.Surface);
  }
 }

 // ---------------------------- stat tile ----------------------------
 public class StatTile : AnimatedControl {
  public string Caption="",IconGlyph="";
  public double Value,Shown,Target;
  public string Format="0.##";
  public string Suffix="";
  public bool Dark;
  public Color Accent=Color.Empty;
  public StatTile(){Height=72;RightToLeft=RightToLeft.No;}
  public void SetValue(double value){ Value=value; Target=value; Ticker.Add(this); }
  public void AnimateTo(double value){ Target=value; Ticker.Add(this); }
  protected override void OnMouseEnter(EventArgs e){AnimateHover(true);base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){AnimateHover(false);base.OnMouseLeave(e);}
  public override void Step(double dt){
   base.Step(dt);
   double before=Shown;
   Shown=Ease.Approach(Shown,Target,dt,7);
   if(Math.Abs(Shown-Target)<0.005)Shown=Target;
   if(Math.Abs(Shown-before)>0.0004)Invalidate();
  }
  public override bool Finished { get { return IsDisposed||(Math.Abs(Shown-Target)<0.005&&base.Finished); } }
  protected override bool IsInputKey(Keys key){return (Cursor==Cursors.Hand&&(key==Keys.Space||key==Keys.Enter))||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(Cursor==Cursors.Hand&&(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter)){OnClick(EventArgs.Empty);e.Handled=true;e.SuppressKeyPress=true;}base.OnKeyDown(e);}
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   var r=new RectangleF(1,1,Width-3,Height-3);if(r.Width<4||r.Height<4)return;
   Color accent=Accent.IsEmpty?palette.Accent:Accent;
   Fdm.Shadow(g,r,14,palette.Shadow,4,3f);
   Fdm.FillRound(g,r,14,palette.Surface);
   using(var pen=new Pen(palette.Line,1f))using(var p=Fdm.Round(r,14))g.DrawPath(pen,p);
   var iconBox=Lang.Rtl?new RectangleF(r.Right-44,r.Top+12,28,28):new RectangleF(r.Left+16,r.Top+12,28,28);
   Fdm.FillRound(g,iconBox,10,Color.FromArgb(38,accent));
   using(var pen=new Pen(accent,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;
    float cx=iconBox.X+iconBox.Width/2f,cy=iconBox.Y+iconBox.Height/2f;
    if(IconGlyph=="speed"){g.DrawArc(pen,cx-7,cy-7,14,14,-40,260);g.DrawLine(pen,cx,cy-7,cx,cy);g.DrawLine(pen,cx,cy,cx+4,cy+3);}
    else if(IconGlyph=="count"){g.DrawRectangle(pen,cx-7,cy-6,14,12);g.DrawLine(pen,cx-4,cy-2,cx+4,cy-2);}
    else if(IconGlyph=="size"){g.DrawEllipse(pen,cx-7,cy-7,14,14);g.DrawLines(pen,new[]{new PointF(cx-3,cy),new PointF(cx-1,cy+3),new PointF(cx+4,cy-3)});}
   }
   string text=Shown.ToString(Format)+Suffix;
   var valueRect=Lang.Rtl?new RectangleF(r.Left+14,r.Top+10,Math.Max(20,r.Width-84),26):new RectangleF(r.Left+52,r.Top+10,Math.Max(20,r.Width-84),26);
   TextRenderer.DrawText(g,CompletedFile.PersianDigits(text),Brand.Font(16f,FontStyle.Bold),Rectangle.Round(valueRect),palette.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
   var captionRect=Lang.Rtl?new RectangleF(r.Left+14,r.Top+37,Math.Max(20,r.Width-84),18):new RectangleF(r.Left+52,r.Top+37,Math.Max(20,r.Width-84),18);
   TextRenderer.DrawText(g,Caption,Brand.Font(9),Rectangle.Round(captionRect),palette.Muted,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.NoPadding);
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-5,-5),palette.Accent,palette.Surface);
  }
 }

 // ---------------------------- search field ----------------------------
 public class SearchField : Panel {
  public TextBox Input=new TextBox();
  public event EventHandler QueryChanged;
  bool dark,focused;
  public SearchField(){
   Height=42;Padding=new Padding(Lang.Rtl?14:44,9,Lang.Rtl?44:14,8);BackColor=Color.Transparent;DoubleBuffered=true;
   Input.BorderStyle=BorderStyle.None;Input.Dock=DockStyle.Fill;Input.Font=Brand.Font(11);Input.RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;
   Input.TextChanged+=(s,e)=>{if(QueryChanged!=null)QueryChanged(this,EventArgs.Empty);Invalidate();};
   Input.HandleCreated+=(s,e)=>{Brand.Cue(Input,Input.Tag as string??"");};
   Input.GotFocus+=(s,e)=>{focused=true;Invalidate();};
   Input.LostFocus+=(s,e)=>{focused=false;Invalidate();};
   Controls.Add(Input);
  }
  public bool Dark { get { return dark; } set { dark=value;Invalidate(); } }
  public void ApplyLanguage(){Padding=new Padding(Lang.Rtl?14:44,9,Lang.Rtl?44:14,8);Input.RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;Input.AccessibleName=Lang.T("search.label");Invalidate();}
  public string Placeholder { get { return Input.Tag as string; } set { Input.Tag=value;Brand.Cue(Input,value??"");Invalidate(); } }
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(dark);
   var r=new RectangleF(1,1,Width-3,Height-3);
   Color border=focused?palette.Accent:palette.Line;
   Fdm.FillRound(g,r,12,palette.Surface);
   if(focused)Fdm.Shadow(g,r,12,Color.FromArgb(46,palette.Accent),4,3.5f);
   using(var pen=new Pen(border,focused?1.6f:1f))using(var p=Fdm.Round(r,12))g.DrawPath(pen,p);
   using(var pen=new Pen(focused?palette.Accent:palette.Muted,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;
    float cx=Lang.Rtl?Width-26:26,cy=Height/2f;
    g.DrawEllipse(pen,cx-8,cy-8,10,10);g.DrawLine(pen,cx+1,cy+1,cx+6,cy+6);
   }
  }
 }

 // ---------------------------- segmented filter ----------------------------
 public class Segmented : AnimatedControl {
  public string[] Items=new string[0];
  public int Selected;
  public event EventHandler SelectionChanged;
  bool dark;
  public Segmented(){Height=40;Cursor=Cursors.Hand;RightToLeft=RightToLeft.No;}
  public bool Dark { get { return dark; } set { dark=value;Invalidate(); } }
  public int IndexAt(int x){
   if(Items.Length==0)return -1;
   int total=Items.Length;int gap=6;
   int w=(Width-gap*(total-1))/total;
   int index=(x-8)/Math.Max(1,w);
   if(index<0)index=0;if(index>=total)index=total-1;return index;
  }
  protected override void OnMouseDown(MouseEventArgs e){
   base.OnMouseDown(e);
   int index=IndexAt(e.X);
   if(index>=0&&index<Items.Length&&index!=Selected){Selected=index;Invalidate();if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);}
  }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(dark);
   if(Items.Length==0)return;
   int gap=6;
   int w=(Width-gap*(Items.Length-1))/Items.Length;
   for(int i=0;i<Items.Length;i++){
    int x=Lang.Rtl?Width-8-(i+1)*w-i*gap:8+i*(w+gap);
    if(x+w<0||x>Width)continue;
    var r=new RectangleF(x,4,w,Height-8);
    bool active=i==Selected;
    if(active){ Fdm.FillRound(g,r,10,palette.Accent); Fdm.Shadow(g,r,10,Color.FromArgb(60,palette.Accent),3,3f); }
    else if(hover>0.01)Fdm.FillRound(g,r,10,Color.FromArgb((int)(hover*200),palette.NavHover));
    TextRenderer.DrawText(g,Items[i],Brand.Font(10,active?FontStyle.Bold:FontStyle.Regular),Rectangle.Round(r),active?Color.White:palette.InkSoft,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.RightToLeft|TextFormatFlags.NoPadding);
   }
  }
 }

 // ---------------------------- connection stepper ----------------------------
 public class ConnectionStepper : AnimatedControl {
  public int Value=8, Minimum=1, Maximum=16;
  public bool Dark;
  public event EventHandler ValueChanged;
  public ConnectionStepper(){Height=44;TabStop=true;RightToLeft=RightToLeft.No;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.SpinButton;}
  protected override bool IsInputKey(Keys key){return key==Keys.Up||key==Keys.Down||key==Keys.Left||key==Keys.Right||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Up||e.KeyCode==Keys.Right){Step(1);e.Handled=true;}if(e.KeyCode==Keys.Down||e.KeyCode==Keys.Left){Step(-1);e.Handled=true;}if(e.KeyCode==Keys.Home){Step(Minimum-Value);e.Handled=true;}if(e.KeyCode==Keys.End){Step(Maximum-Value);e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
  public void SetValue(int value){Value=Math.Max(Minimum,Math.Min(Maximum,value));Invalidate();}
  void Step(int delta){
   int next=Math.Max(Minimum,Math.Min(Maximum,Value+delta));
   if(next==Value)return;
   Value=next;Invalidate();
   if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);
  }
  Rectangle MinusBox { get { return Lang.Rtl?new Rectangle(Width-32,5,28,Height-10):new Rectangle(4,5,28,Height-10); } }
  Rectangle PlusBox { get { return Lang.Rtl?new Rectangle(4,5,28,Height-10):new Rectangle(Width-32,5,28,Height-10); } }
  protected override void OnMouseDown(MouseEventArgs e){
   base.OnMouseDown(e);
   if(e.Button!=MouseButtons.Left)return;Focus();
   if(MinusBox.Contains(e.Location))Step(-1);
   else if(PlusBox.Contains(e.Location))Step(1);
  }
  protected override void OnMouseMove(MouseEventArgs e){ base.OnMouseMove(e); }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   var r=new RectangleF(1,1,Width-3,Height-3);if(r.Width<10||r.Height<10)return;
   Fdm.FillRound(g,r,12,palette.Surface);
   using(var pen=new Pen(palette.Line,1f))using(var p=Fdm.Round(r,12))g.DrawPath(pen,p);
   var minus=MinusBox;var plus=PlusBox;
   using(var pen=new Pen(palette.LineSoft,1f)){
    g.DrawLine(pen,Lang.Rtl?minus.Left-1:minus.Right+0.5f,8,Lang.Rtl?minus.Left-1:minus.Right+0.5f,Height-8);
    g.DrawLine(pen,Lang.Rtl?plus.Right+0.5f:plus.Left-0.5f,8,Lang.Rtl?plus.Right+0.5f:plus.Left-0.5f,Height-8);
   }
   Color ink=palette.Ink;
   using(var pen=new Pen(ink,1.7f)){pen.StartCap=pen.EndCap=LineCap.Round;
    g.DrawLine(pen,minus.Left+9,minus.Y+minus.Height/2f,minus.Right-9,minus.Y+minus.Height/2f);
    g.DrawLine(pen,plus.Left+9,plus.Y+plus.Height/2f,plus.Right-9,plus.Y+plus.Height/2f);
    g.DrawLine(pen,plus.Left+plus.Width/2f,plus.Y+9,plus.Left+plus.Width/2f,plus.Bottom-9);
   }
   TextRenderer.DrawText(g,CompletedFile.PersianDigits(Value.ToString()),Brand.Font(12f,FontStyle.Bold),new Rectangle(32,0,Math.Max(10,Width-64),Height),palette.Ink,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-3,-3),palette.Accent,palette.Surface);
  }
 }

 // ---------------------------- download table ----------------------------
 public class DownloadTable : Panel {
  public List<DownloadItem> Items=new List<DownloadItem>();
  public event Action<DownloadItem> Activated;
  public event Action<DownloadItem> MenuRequested;
  public ContextMenuStrip Menu;
  public bool Dark;
  public int RowHeight=58;
  public readonly Label EmptyHint=new Label();
  public string EmptyMessage="";
  Panel host;Control header;
  readonly Dictionary<int,DownloadRowView> pool=new Dictionary<int,DownloadRowView>();
  public DownloadTable(){
   Dock=DockStyle.Fill;DoubleBuffered=true;BackColor=Color.White;Padding=new Padding(0);
   header=new TableHeader{Dock=DockStyle.Top,Height=36};
   host=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.Transparent};
   host.Scroll+=(s,e)=>LayoutRows();
   Controls.Add(host);Controls.Add(header);
   EmptyHint.TextAlign=ContentAlignment.MiddleCenter;EmptyHint.Font=Brand.Font(11);EmptyHint.Visible=false;EmptyHint.BackColor=Color.Transparent;EmptyHint.AutoSize=false;EmptyHint.RightToLeft=RightToLeft.Yes;
   Controls.Add(EmptyHint);EmptyHint.BringToFront();
   header.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left)return;int index=IndexAt(e.X);string key=index==1?"title":index==2?"size":index==5?"date":null;if(key==null)return;SortDescending=SortKey==key?!SortDescending:key!="title";SortKey=key;ApplySort();if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);};
  }
  public int Selected=-1;
  public event EventHandler SelectionChanged;
  public string SortKey="date";public bool SortDescending=true;
  void Sort(){
   if(SortKey=="date")SortDescending=!SortDescending;
   else{SortKey="date";SortDescending=true;}
   ApplySort();
  }
  public void ApplySort(){
   Items.Sort(delegate(DownloadItem a,DownloadItem b){
    int c=SortKey=="title"?string.Compare(a.Title,b.Title,StringComparison.OrdinalIgnoreCase):SortKey=="size"?a.Bytes.CompareTo(b.Bytes):a.Order.CompareTo(b.Order);
    if(c==0)c=string.Compare(a.Title,b.Title,StringComparison.OrdinalIgnoreCase);
    return SortDescending?-c:c;
   });
   LayoutRows();
  }
  public void RefreshRows(){ LayoutRows(); foreach(var kv in pool) kv.Value.Invalidate(); }
  public void SetItems(List<DownloadItem> items){
   Items=items??new List<DownloadItem>();
   LayoutRows();
  }
  int IndexAt(int x){
   var cells=FdmGrid.Columns(Width);
   for(int i=0;i<cells.Length;i++)if(cells[i].Width>0&&x>=cells[i].X&&x<cells[i].X+cells[i].Width)return i;
   return -1;
  }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   using(var b=new SolidBrush(palette.Surface))g.FillRectangle(b,ClientRectangle);
   if(Items.Count>0||EmptyMessage.Length==0)return;
   float cx=Width/2f,cy=header!=null?header.Height+(Height-header.Height)/2f-30:Height/2f;
   if(Height-header.Height<140){TextRenderer.DrawText(g,EmptyMessage,Brand.Font(10.5f),new Rectangle(12,header.Height,Math.Max(1,Width-24),Math.Max(1,Height-header.Height)),palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|(Lang.Rtl?TextFormatFlags.RightToLeft:0));return;}
   using(var pen=new Pen(Color.FromArgb(70,palette.Muted),2f)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
    g.DrawLine(pen,cx,cy-26,cx,cy-2);
    g.DrawLines(pen,new[]{new PointF(cx-8,cy-10),new PointF(cx,cy-1),new PointF(cx+8,cy-10)});
    g.DrawLines(pen,new[]{new PointF(cx-13,cy+8),new PointF(cx-13,cy+15),new PointF(cx+13,cy+15),new PointF(cx+13,cy+8)});
   }
   var text=new RectangleF(0,cy+34,Width,44);
   TextRenderer.DrawText(g,EmptyMessage,Brand.Font(10.5f),Rectangle.Round(text),palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.RightToLeft|TextFormatFlags.NoPadding);
  }
  protected override void OnResize(EventArgs e){ base.OnResize(e); LayoutRows(); }
  internal void LayoutRows(){
   if(host==null)return;
   var palette=Fdm.Of(Dark);
   header.BackColor=palette.Surface;
   ((TableHeader)header).Dark=Dark;
   int total=Items.Count;
   host.AutoScrollMinSize=new Size(0,Math.Max(0,total*RowHeight));
   int visible=host.ClientSize.Height/RowHeight+3;
   int offset=host.AutoScrollPosition.Y;
   int first=Math.Max(0,-offset/RowHeight-1);
   int last=Math.Min(total,first+visible);
   var wanted=new HashSet<int>();
   for(int i=first;i<last;i++)wanted.Add(i);
   var stale=new List<int>(pool.Keys);
   foreach(int key in stale)if(!wanted.Contains(key)){pool[key].Dispose();pool.Remove(key);}
   for(int i=first;i<last;i++){
    DownloadRowView view;
    if(!pool.TryGetValue(i,out view)){
     view=new DownloadRowView{Dock=DockStyle.None};
     view.SetEnterDelay(0.025*(i-first));
     view.ItemActivated+=(item)=>{if(Activated!=null)Activated(item);};
     view.MenuRequested+=(item)=>{if(MenuRequested!=null)MenuRequested(item);};
     pool[i]=view;host.Controls.Add(view);
    }
    view.Dark=Dark;
    view.Item=Items[i];
    view.Bounds=new Rectangle(0,i*RowHeight+offset,host.ClientSize.Width,RowHeight);
   }
   EmptyHint.Visible=Items.Count==0;
   EmptyHint.Text="";
   EmptyHint.Bounds=new Rectangle(0,header.Height+4,Math.Max(10,Width),Math.Max(10,host.ClientSize.Height-8));
   EmptyHint.ForeColor=palette.Muted;
   EmptyHint.BringToFront();
   Invalidate();
  }
  protected override void OnVisibleChanged(EventArgs e){ base.OnVisibleChanged(e); LayoutRows(); }
  protected override void Dispose(bool disposing){if(disposing&&Menu!=null)Menu.Dispose();base.Dispose(disposing);}

  class TableHeader : Control {
   public bool Dark;
   public TableHeader(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);DoubleBuffered=true;}
   protected override void OnPaint(PaintEventArgs e){
    var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
    var palette=Fdm.Of(Dark);
    using(var b=new SolidBrush(palette.SurfaceAlt))g.FillRectangle(b,ClientRectangle);
    var table=Parent as DownloadTable;var cells=FdmGrid.Columns(table==null?Width:table.host.ClientSize.Width);
    string[] labels={"",Lang.T("table.name"),Lang.T("table.size"),Lang.T("table.status"),Lang.T("table.speed"),Lang.T("table.date")};
    var font=Brand.Font(9.5f,FontStyle.Bold);
    for(int i=0;i<cells.Length;i++){
     if(cells[i].Width<=0||labels[i].Length==0)continue;
     var r=cells[i];r.X+=8;r.Width=Math.Max(1,r.Width-16);r.Height=Height-10;
     TextRenderer.DrawText(g,labels[i],font,r,palette.Muted,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
    }
    using(var pen=new Pen(palette.Line,1f))g.DrawLine(pen,0,Height-0.5f,Width,Height-0.5f);
   }
  }
 }

 public class DownloadRowView : AnimatedControl {
  DownloadItem item=new DownloadItem();
  public bool Dark;
  public event Action<DownloadItem> ItemActivated;
  public event Action<DownloadItem> MenuRequested;
  bool dotsHover;
  double shown,target,sweep;
  public DownloadItem Item {
   get { return item; }
   set { item=value;target=value.Progress;AccessibleName=value.Title+", "+value.StatusText;Ticker.Add(this); }
  }
  public DownloadRowView(){Height=58;TabStop=true;AccessibleRole=AccessibleRole.ListItem;RightToLeft=RightToLeft.No;DoubleBuffered=true;}
  protected override void OnMouseEnter(EventArgs e){AnimateHover(true);base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){AnimateHover(false);dotsHover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){
   base.OnMouseDown(e);
   Focus();if(e.Button==MouseButtons.Right){if(MenuRequested!=null)MenuRequested(Item);return;}if(e.Button!=MouseButtons.Left)return;
   var cells=FdmGrid.Columns(Width);
   if(DotsHit(cells,e.X)&&Item.Kind=="completed"){ if(MenuRequested!=null)MenuRequested(Item); }
  }
  protected override void OnMouseMove(MouseEventArgs e){
   var cells=FdmGrid.Columns(Width);
   bool inside=DotsHit(cells,e.X);
   if(inside!=dotsHover){dotsHover=inside;Invalidate();}
   base.OnMouseMove(e);
  }
  bool DotsHit(Rectangle[] cells,int x){
   return Lang.Rtl?x>=0&&x<32:x>=Width-32&&x<Width;
  }
  protected override void OnMouseDoubleClick(MouseEventArgs e){base.OnMouseDoubleClick(e);if(e.Button==MouseButtons.Left&&!DotsHit(FdmGrid.Columns(Width),e.X)&&ItemActivated!=null)ItemActivated(Item);}
  protected override bool IsInputKey(Keys key){return key==Keys.Enter||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter){if(ItemActivated!=null)ItemActivated(Item);e.Handled=true;}if(e.KeyCode==Keys.Apps||(e.Shift&&e.KeyCode==Keys.F10)){if(MenuRequested!=null)MenuRequested(Item);e.Handled=true;}base.OnKeyDown(e);}
  protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
  protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
  public override void Step(double dt){
   base.Step(dt);
   target=Item.Progress;
   double before=shown;
   shown=Ease.Approach(shown,target,dt,10);
   sweep=(sweep+dt*0.6)%1.0;
   if(Math.Abs(shown-before)>0.0004||Item.Indeterminate)Invalidate();
  }
  public override bool Finished { get { return IsDisposed||(!Item.Indeterminate&&Math.Abs(shown-target)<0.002&&base.Finished); } }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   float shift=(float)(1-enter)*30f;
   g.TranslateTransform(shift,0);
   var cells=FdmGrid.Columns(Width);
   if(hover>0.01){
    using(var b=new SolidBrush(Color.FromArgb((int)(hover*(Dark?26:46)),palette.Ink)))g.FillRectangle(b,new Rectangle(0,0,Width,Height-1));
   }
   Color accent=palette.Accent;
   if(Item.Kind=="active")accent=palette.Accent;
   else if(Item.Kind=="completed")accent=palette.Success;
   else if(Item.Kind=="queued")accent=palette.Warn;
   else if(Item.Kind=="failed")accent=palette.Danger;
   else accent=palette.Muted;
   var iconBox=new RectangleF(cells[0].X+cells[0].Width-38,Height/2f-14,28,28);
   Fdm.FillRound(g,iconBox,9,Color.FromArgb(30,accent));
   using(var pen=new Pen(accent,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
    float cx=iconBox.X+14,cy=iconBox.Y+14;
    if(Item.Kind=="active"){g.DrawLine(pen,cx-3,cy-6,cx-3,cy+6);g.DrawLine(pen,cx+3,cy-6,cx+3,cy+6);}
    else if(Item.Kind=="completed"){g.DrawLines(pen,new[]{new PointF(cx-6,cy),new PointF(cx-2,cy+4),new PointF(cx+6,cy-5)});}
    else if(Item.Kind=="queued"){g.DrawEllipse(pen,cx-6,cy-6,12,12);g.DrawLine(pen,cx,cy-4,cx,cy);g.DrawLine(pen,cx,cy,cx+3,cy+1);}
    else if(Item.Kind=="failed"){g.DrawLine(pen,cx-5,cy-5,cx+5,cy+5);g.DrawLine(pen,cx+5,cy-5,cx-5,cy+5);}
    else{g.DrawLine(pen,cx-5,cy-5,cx+5,cy+5);g.DrawLine(pen,cx+5,cy-5,cx-5,cy+5);}
   }
   var nameRect=cells[1];nameRect.Offset(8,8);nameRect.Width=Math.Max(1,nameRect.Width-16);nameRect.Height=22;
   TextRenderer.DrawText(g,Item.Title,Brand.Font(10.5f,FontStyle.Bold),nameRect,palette.Ink,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
   var metaRect=cells[1];metaRect.Offset(8,32);metaRect.Width=Math.Max(1,metaRect.Width-16);metaRect.Height=18;
   TextRenderer.DrawText(g,Item.StatusText??"",Brand.Font(9),metaRect,palette.Muted,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
   if(cells[2].Width>0){
    var r=cells[2];r.Offset(8,10);r.Width=Math.Max(1,r.Width-16);r.Height=22;
    TextRenderer.DrawText(g,Item.SizeText??"",Brand.Font(10),r,palette.InkSoft,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
   }
   if(cells[3].Width>0){
    var r=cells[3];r.Offset(8,16);r.Width=Math.Max(1,r.Width-16);r.Height=20;
    if(Item.Kind=="active"||Item.Kind=="paused"||Item.Kind=="queued"){
     int barWidth=r.Width-52;
     var bar=new RectangleF(Lang.Rtl?r.Left+52:r.Left,r.Top+5,(float)barWidth,10);
     Fdm.FillRound(g,bar,5,palette.Track);
     if(Item.Indeterminate){
      float band=Math.Max(24f,barWidth*0.3f);
      float slide=(float)(sweep*(barWidth-band));
      var fill=new RectangleF(Lang.Rtl?bar.Right-band-slide:bar.Left+slide,bar.Top,band,10);
      Fdm.FillRound(g,fill,5,palette.Accent);
     }else if(shown>0.001){
      float w=(float)Math.Max(10,barWidth*shown);
      var fill=new RectangleF(Lang.Rtl?bar.Right-w:bar.Left,bar.Top,w,10);
      Fdm.FillRound(g,fill,5,palette.Accent,palette.Accent2,0f);
     }
     if(Item.Kind!="queued"){
      var pct=new Rectangle(Lang.Rtl?r.Left+3:r.Right-46,r.Top-4,44,26);
      TextRenderer.DrawText(g,CompletedFile.PersianDigits((shown*100).ToString("0")+"%"),Brand.Font(9.5f,FontStyle.Bold),pct,palette.InkSoft,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.NoPadding);
     }
    }else{
     string text=Item.Kind=="completed"?Lang.T("row.completed"):(Item.StatusText??"");
     Color ink=Item.Kind=="completed"?palette.Success:palette.Muted;
     TextRenderer.DrawText(g,text,Brand.Font(9.5f,FontStyle.Bold),r,ink,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
    }
   }
   if(cells[4].Width>0){
    var r=cells[4];r.Offset(8,10);r.Height=22;r.Width=Math.Max(1,r.Width-16);
    TextRenderer.DrawText(g,Item.SpeedText??"",Brand.Font(10),r,palette.InkSoft,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
   }
   if(cells[5].Width>0){
    var r=cells[5];r.Offset(8,10);r.Width=Math.Max(1,r.Width-16);r.Height=22;
    TextRenderer.DrawText(g,Item.DateText??"",Brand.Font(9.5f),r,palette.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
   }
   if(Item.Kind=="completed"){
    float dotX=Lang.Rtl?2:Width-30;
    var dots=new RectangleF(dotX,10,26,30);
    if(dotsHover) Fdm.FillRound(g,dots,8,Color.FromArgb(38,palette.Ink));
    using(var pen=new Pen(dotsHover?palette.Ink:palette.Muted,1.8f)){
     for(int n=-1;n<=1;n++)using(var dot=new SolidBrush(pen.Color))g.FillEllipse(dot,dotX+11,24+n*5,2.6f,2.6f);
    }
   }
   using(var pen=new Pen(palette.LineSoft,1f))g.DrawLine(pen,0,Height-1.5f,Width,Height-1.5f);
   if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-2,-2),palette.Accent,palette.Surface);
  }
 }

 // ---------------------------- drop zone ----------------------------
 public class DropZone : AnimatedControl {
  public string Message="";
  public event Action<string[]> Dropped;
  bool dark;
  public DropZone(){Height=96;RightToLeft=RightToLeft.No;AllowDrop=true;Message=Lang.T("drop.hint");}
  public bool Dark { get { return dark; } set { dark=value;Invalidate(); } }
  protected override void OnDragEnter(DragEventArgs e){Uri uri;string text=e.Data!=null?e.Data.GetData(DataFormats.Text) as string:null;bool valid=Uri.TryCreate((text??"").Trim(),UriKind.Absolute,out uri)&&(uri.Scheme=="https"||uri.Scheme=="http");e.Effect=valid?DragDropEffects.Copy:DragDropEffects.None;AnimateHover(valid);base.OnDragEnter(e);}
  protected override void OnDragLeave(EventArgs e){AnimateHover(false);base.OnDragLeave(e);}
  protected override void OnDragDrop(DragEventArgs e){
   if(e.Data!=null&&e.Data.GetDataPresent(DataFormats.Text)){
    string text=e.Data.GetData(DataFormats.Text) as string;
    if(!String.IsNullOrEmpty(text)&&Dropped!=null)Dropped(new[]{text.Trim()});
   }
   AnimateHover(false);Invalidate();base.OnDragDrop(e);
  }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(dark);
   var r=new RectangleF(1,1,Width-3,Height-3);
   Color stroke=hover>0.01?palette.Accent:Color.FromArgb(150,palette.Muted);
   Fdm.FillRound(g,r,14,hover>0.01?Color.FromArgb((int)(hover*24),palette.Accent):palette.SurfaceAlt);
   using(var pen=new Pen(stroke,1.6f)){pen.DashStyle=DashStyle.Dash;using(var p=Fdm.Round(r,14))g.DrawPath(pen,p);}
   using(var pen=new Pen(hover>0.01?palette.Accent:palette.Muted,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;
    float cx=Width/2f,cy=26;
    g.DrawLines(pen,new[]{new PointF(cx-6,cy+4),new PointF(cx,cy+9),new PointF(cx+8,cy+1)});
    g.DrawArc(pen,cx-9,cy-9,16,16,-90,180);g.DrawLine(pen,cx,cy-9,cx,cy+9);
   }
   var text=new RectangleF(0,42,Width,22);
   TextRenderer.DrawText(g,Message,Brand.Font(10.5f),Rectangle.Round(text),hover>0.01?palette.AccentInk:palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.NoPadding);
  }
 }

 // ---------------------------- toast ----------------------------
 public class Toast : AnimatedControl {
  public new string Text="";public bool Danger;public bool Dark;
  double life,offset;bool dead;
  public Toast(){Height=46;Width=320;RightToLeft=RightToLeft.No;}
  public void Show(string message,bool danger){Text=message;Danger=danger;offset=26;Ticker.Add(this);}
  public override void Step(double dt){
   life+=dt;offset=Ease.Approach(offset,0,dt,14);
   if(offset>0.2||life<3.2)Invalidate();
   if(life>3.6){dead=true;Dispose();}
  }
  public override bool Finished { get { return dead||IsDisposed; } }
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   float y=(float)offset;
   var r=new RectangleF(1,1,Width-3,Height-3);
   g.TranslateTransform(0,y);
   Color accent=Danger?palette.Danger:palette.Success;
   Fdm.Shadow(g,r,12,Color.FromArgb(60,palette.Ink),4,4f);
   Fdm.FillRound(g,r,12,palette.Surface);
   using(var pen=new Pen(palette.Line,1f))using(var p=Fdm.Round(r,12))g.DrawPath(pen,p);
   var dot=Lang.Rtl?new RectangleF(r.Right-40,r.Top+13,20,20):new RectangleF(r.Left+20,r.Top+13,20,20);
   Fdm.FillRound(g,dot,10,Color.FromArgb(40,accent));
   using(var pen=new Pen(accent,2f)){pen.StartCap=pen.EndCap=LineCap.Round;float cx=dot.X+10,cy=dot.Y+10;
    if(Danger){g.DrawLine(pen,cx-4,cy-4,cx+4,cy+4);g.DrawLine(pen,cx+4,cy-4,cx-4,cy+4);}
    else g.DrawLines(pen,new[]{new PointF(cx-4,cy),new PointF(cx-1,cy+3),new PointF(cx+5,cy-3)});
   }
   var text=new RectangleF(Lang.Rtl?r.Left+14:r.Left+54,r.Top,Math.Max(20,r.Width-70),r.Height);
   TextRenderer.DrawText(g,Text,Brand.Font(10),Rectangle.Round(text),palette.Ink,(Lang.Rtl?TextFormatFlags.Right:TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.NoPadding);
  }
 }

 // ---------------------------- status strip ----------------------------
 public class StatusStrip : Panel {
  public string SpeedText="",BadgeText="",Hint="";
  public bool Dark;
  public StatusStrip(){Height=40;DoubleBuffered=true;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
   var palette=Fdm.Of(Dark);
   using(var b=new SolidBrush(palette.Surface))g.FillRectangle(b,ClientRectangle);
   using(var pen=new Pen(palette.Line,1f))g.DrawLine(pen,0,0.5f,Width,0.5f);
   float chipWidth=Math.Max(140,Measure(g,SpeedText)+54);
   var chip=Lang.Rtl?new RectangleF(Width-chipWidth-12,7,chipWidth,26):new RectangleF(12,7,chipWidth,26);
   Fdm.FillRound(g,chip,13,palette.SurfaceAlt);
   using(var pen=new Pen(palette.Line,1f))using(var p=Fdm.Round(chip,13))g.DrawPath(pen,p);
   float arrowX=Lang.Rtl?chip.Right-20:chip.Left+20;
   using(var pen=new Pen(palette.Success,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;
    if(Lang.Rtl){
     g.DrawLines(pen,new[]{new PointF(arrowX,chip.Y+8),new PointF(arrowX-6,chip.Y+14),new PointF(arrowX,chip.Y+20)});
     g.DrawLines(pen,new[]{new PointF(arrowX-5,chip.Y+8),new PointF(arrowX-11,chip.Y+14),new PointF(arrowX-5,chip.Y+20)});
    }else{
     g.DrawLines(pen,new[]{new PointF(arrowX,chip.Y+8),new PointF(arrowX+6,chip.Y+14),new PointF(arrowX,chip.Y+20)});
     g.DrawLines(pen,new[]{new PointF(arrowX+5,chip.Y+8),new PointF(arrowX+11,chip.Y+14),new PointF(arrowX+5,chip.Y+20)});
    }
   }
   var speedRect=new RectangleF(chip.X+(Lang.Rtl?10:38),chip.Y,chip.Width-46,chip.Height);
   TextRenderer.DrawText(g,SpeedText,Brand.Font(9.5f,FontStyle.Bold),Rectangle.Round(speedRect),palette.InkSoft,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
   float used=chipWidth+28;
   if(BadgeText.Length>0){
    float badgeWidth=Math.Max(64,Measure(g,BadgeText)+28);
    var badge=Lang.Rtl?new RectangleF(chip.X-badgeWidth-8,7,badgeWidth,26):new RectangleF(chip.X+chipWidth+8,7,badgeWidth,26);
    Fdm.FillRound(g,badge,13,Color.FromArgb(38,palette.Success));
    used+=badgeWidth+8;
    TextRenderer.DrawText(g,BadgeText,Brand.Font(9.5f,FontStyle.Bold),Rectangle.Round(badge),palette.Success,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|(Lang.Rtl?TextFormatFlags.RightToLeft:0)|TextFormatFlags.NoPadding);
   }
   if(Hint.Length>0){
    int hintWidth=Math.Max(0,Width-(int)used-24);
    var hint=new Rectangle(Lang.Rtl?12:(int)used,0,hintWidth,Height);
    if(hintWidth>20)TextRenderer.DrawText(g,Hint,Brand.Font(9.5f),hint,palette.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
   }
  }
  float Measure(Graphics g,string text){ if(String.IsNullOrEmpty(text))return 0; SizeF b=g.MeasureString(text,Brand.Font(9.5f)); return b.Width; }
 }
}
