using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
namespace MehrDownload {
 class DownloadPrompt:GlassForm {
  public DownloadPrompt(string url,string name,bool queued,bool dark){
   Text=Lang.T("prompt.title");Font=Brand.Font(11);ClientSize=new Size(560,380);MinimumSize=new Size(520,360);StartPosition=FormStartPosition.CenterParent;RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;
   var body=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(28,16,28,22),ColumnCount=1,RowCount=4};
   body.RowStyles.Add(new RowStyle(SizeType.Absolute,66));
   body.RowStyles.Add(new RowStyle(SizeType.Absolute,98));
   body.RowStyles.Add(new RowStyle(SizeType.Absolute,8));
   body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   Controls.Add(body);
   var head=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
   head.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,58));
   head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   head.Controls.Add(new LogoView{Dock=DockStyle.Fill,Margin=new Padding(0,0,12,0)},0,0);
   var titles=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
   titles.RowStyles.Add(new RowStyle(SizeType.Percent,58));
   titles.RowStyles.Add(new RowStyle(SizeType.Percent,42));
   var heading=new Label{Text=queued?Lang.T("prompt.queued"):Lang.T("prompt.question"),Font=Brand.Font(16,FontStyle.Regular),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,AutoSize=false};
   titles.Controls.Add(heading,0,0);
   var subtitle=new Label{Text=Lang.T("prompt.hint"),Font=Brand.Font(9.5f),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,AutoSize=false};
   titles.Controls.Add(subtitle,0,1);
   head.Controls.Add(titles,1,0);
   body.Controls.Add(head,0,0);
   var card=new GlassPanel{Dock=DockStyle.Fill,Radius=16,Padding=new Padding(18),Margin=Padding.Empty};
   var cardRows=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
   cardRows.RowStyles.Add(new RowStyle(SizeType.Percent,58));
   cardRows.RowStyles.Add(new RowStyle(SizeType.Percent,42));
   var fileName=new Label{Text=name,Dock=DockStyle.Fill,Font=Brand.Font(13,FontStyle.Regular),TextAlign=ContentAlignment.MiddleCenter,AutoEllipsis=true,BackColor=Color.Transparent,RightToLeft=RightToLeft.No,AutoSize=false};
   cardRows.Controls.Add(fileName,0,0);
   var host=new Label{Text=new Uri(url).Host,Dock=DockStyle.Fill,Font=Brand.Font(9.5f),TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.Transparent,RightToLeft=RightToLeft.No,AutoSize=false};
   cardRows.Controls.Add(host,0,1);
   card.Controls.Add(cardRows);
   body.Controls.Add(card,0,1);
   var actions=new CenteredActionRow();
   var yes=new GlassButton{Text=queued?Lang.T("prompt.enqueue"):Lang.T("prompt.download"),Primary=true,Size=new Size(160,44),DialogResult=DialogResult.Yes};
   var no=new GlassButton{Text=Lang.T("prompt.cancel"),Size=new Size(130,44),DialogResult=DialogResult.Cancel};
   actions.Controls.Add(yes);actions.Controls.Add(no);
   body.Controls.Add(actions,0,3);
   CancelButton=no;Shown+=(s,e)=>no.Focus();
   var timeout=new Timer{Interval=90000};timeout.Tick+=(s,e)=>{DialogResult=DialogResult.Cancel;Close();};Shown+=(s,e)=>timeout.Start();FormClosed+=(s,e)=>timeout.Dispose();
   FdmTheme.Apply(this,this,dark);
  }
 }
 class BatchDialog : GlassForm {
  public string Links="";
  public BatchDialog(bool dark=false){
   Text=Lang.T("batch.title");Font=Brand.Font(11);ClientSize=new Size(660,470);MinimumSize=new Size(540,420);StartPosition=FormStartPosition.CenterParent;RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;
   var body=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(26,16,26,22),ColumnCount=1,RowCount=4};
   body.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
   body.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
   body.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   body.RowStyles.Add(new RowStyle(SizeType.Absolute,66));
   Controls.Add(body);
   var heading=new Label{Text=Lang.T("batch.heading"),Font=Brand.Font(15,FontStyle.Regular),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,AutoSize=false};
   body.Controls.Add(heading,0,0);
   var hint=new Label{Text=Lang.T("batch.hint"),Font=Brand.Font(9.5f),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,AutoSize=false};
   body.Controls.Add(hint,0,1);
   var area=new TextBox{Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,Font=Brand.Font(11),RightToLeft=RightToLeft.No,AcceptsReturn=true,AccessibleName=Lang.T("batch.title")};
   var box=new GlassPanel{Dock=DockStyle.Fill,Radius=12,Padding=new Padding(12),Margin=Padding.Empty,Elevated=false};
   box.Controls.Add(area);
   body.Controls.Add(box,0,2);
   var actions=new CenteredActionRow();
   var start=new GlassButton{Text=Lang.T("batch.start"),Primary=true,Size=new Size(180,44),DialogResult=DialogResult.OK};
   var cancel=new GlassButton{Text=Lang.T("prompt.cancel"),Size=new Size(120,44),DialogResult=DialogResult.Cancel};
   actions.Controls.Add(start);actions.Controls.Add(cancel);
   body.Controls.Add(actions,0,3);
   AcceptButton=start;CancelButton=cancel;
   start.Click+=(s,e)=>Links=area.Text.Trim();
   Shown+=(s,e)=>area.Focus();
   FdmTheme.Apply(this,this,dark);
  }
 }
 class DownloadSpeed {
  class Sample {public long Bytes;public double Seconds;}
  readonly Queue<Sample> samples=new Queue<Sample>();readonly Stopwatch clock=new Stopwatch();
  public void Reset(){samples.Clear();clock.Restart();samples.Enqueue(new Sample{Seconds=0,Bytes=0});}
  public double Measure(long bytes){if(!clock.IsRunning)Reset();var now=new Sample{Seconds=clock.Elapsed.TotalSeconds,Bytes=bytes};samples.Enqueue(now);while(samples.Count>1&&now.Seconds-samples.Peek().Seconds>3)samples.Dequeue();var first=samples.Peek();double elapsed=now.Seconds-first.Seconds;return elapsed>=.2?Math.Max(0,now.Bytes-first.Bytes)/elapsed:0;}
 }
}
