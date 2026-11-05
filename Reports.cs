using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Drawing;
using System.Windows.Forms;
namespace MehrDownload {
 class ProblemReport {public string id=Guid.NewGuid().ToString("N"),category,title,description,contact,version=ReleaseInfo.Version;}
 static class ProblemReports {
  static readonly object gate=new object();static bool sending;
  static string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MehrDownload","reports.json");
  static List<ProblemReport> Read(){return File.Exists(path)?new JavaScriptSerializer().Deserialize<List<ProblemReport>>(File.ReadAllText(path)):new List<ProblemReport>();}
  static void Write(List<ProblemReport> items){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",new JavaScriptSerializer().Serialize(items));if(File.Exists(path))File.Replace(path+".tmp",path,null);else File.Move(path+".tmp",path);}
  public static void Enqueue(ProblemReport report){lock(gate){var list=Read();if(list.Count>=100)throw new Exception(Lang.T("report.full"));list.Add(report);Write(list);}}
  public static async Task<bool> Flush(string id=null){
   lock(gate){if(sending)return false;sending=true;}
   try{return await Task.Run(()=>{
    List<ProblemReport> pending;lock(gate)pending=Read();
    foreach(var report in pending){bool delivered=false;foreach(string origin in new[]{AdPlacement.Origin,AdPlacement.LegacyOrigin})try{
     var request=(HttpWebRequest)WebRequest.Create(origin+"/api/public/reports");request.Method="POST";request.ContentType="application/json";request.Timeout=5000;request.ReadWriteTimeout=5000;request.AllowAutoRedirect=false;
     byte[] bytes=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(report));request.ContentLength=bytes.Length;using(var stream=request.GetRequestStream())stream.Write(bytes,0,bytes.Length);
     using(var response=(HttpWebResponse)request.GetResponse()){delivered=response.StatusCode==HttpStatusCode.Created||response.StatusCode==HttpStatusCode.Accepted;}
     if(delivered)break;
    }catch{}
    if(!delivered)break;lock(gate){var list=Read();list.RemoveAll(r=>r.id==report.id);Write(list);}
    }lock(gate)return id!=null&&!Read().Exists(r=>r.id==id);
   });}catch{return false;}finally{lock(gate)sending=false;}
  }
 }
  class ReportDialog:GlassForm {
  public ReportDialog(){
   Text=Lang.T("report.title");ClientSize=new Size(600,660);MinimumSize=new Size(600,660);StartPosition=FormStartPosition.CenterParent;Font=Brand.Font(11);RightToLeft=Lang.Rtl?RightToLeft.Yes:RightToLeft.No;
   var body=new SmoothTableLayout{Dock=DockStyle.Fill,Padding=new Padding(26,16,26,22),ColumnCount=1,RowCount=10};
   foreach(int h in new[]{40,24,42,24,42,24,120,24,42,64})body.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
   Controls.Add(body);
   body.Controls.Add(new Label{Text=Lang.T("report.heading"),Font=Brand.Font(16,FontStyle.Regular),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,AutoSize=false},0,0);
   body.Controls.Add(new Label{Text=Lang.T("report.category"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,Font=Brand.Font(9.5f),AutoSize=false},0,1);
   var category=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,Font=Brand.Font(10.5f)};category.Items.AddRange(new object[]{Lang.T("report.category.ui"),Lang.T("report.category.download"),Lang.T("report.category.update"),Lang.T("report.category.other")});category.SelectedIndex=0;body.Controls.Add(category,0,2);
   category.DrawMode=DrawMode.OwnerDrawFixed;category.ItemHeight=26;
   category.DrawItem+=(s,e)=>{if(e.Index<0)return;var palette=Fdm.Of(GlassDark);bool selected=(e.State&DrawItemState.Selected)!=0;using(var brush=new SolidBrush(selected?palette.AccentSoft:palette.Surface))e.Graphics.FillRectangle(brush,e.Bounds);var bounds=Rectangle.Inflate(e.Bounds,-6,0);TextRenderer.DrawText(e.Graphics,category.Items[e.Index].ToString(),category.Font,bounds,palette.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|(Lang.Rtl?TextFormatFlags.Right|TextFormatFlags.RightToLeft:TextFormatFlags.Left));e.DrawFocusRectangle();};
   body.Controls.Add(new Label{Text=Lang.T("report.titleField"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,Font=Brand.Font(9.5f),AutoSize=false},0,3);
   var title=new TextBox{Dock=DockStyle.Fill,MaxLength=120,Font=Brand.Font(10.5f),BorderStyle=BorderStyle.FixedSingle};body.Controls.Add(title,0,4);
   body.Controls.Add(new Label{Text=Lang.T("report.body"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,Font=Brand.Font(9.5f),AutoSize=false},0,5);
   var description=new TextBox{Dock=DockStyle.Fill,Multiline=true,MaxLength=4000,ScrollBars=ScrollBars.Vertical,Font=Brand.Font(10.5f),BorderStyle=BorderStyle.FixedSingle};body.Controls.Add(description,0,6);
   body.Controls.Add(new Label{Text=Lang.T("report.contact"),Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.Transparent,Font=Brand.Font(9.5f),AutoSize=false},0,7);
   var contact=new TextBox{Dock=DockStyle.Fill,MaxLength=160,RightToLeft=RightToLeft.No,Font=Brand.Font(10.5f),BorderStyle=BorderStyle.FixedSingle};body.Controls.Add(contact,0,8);
   var foot=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
   foot.RowStyles.Add(new RowStyle(SizeType.Absolute,64));
   foot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170));foot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   var send=new GlassButton{Text=Lang.T("report.send"),Primary=true,Dock=DockStyle.Fill,Height=44,MaximumSize=new Size(0,48),Margin=new Padding(0,8,8,8)};
   var note=new Label{Text=Lang.T("report.version")+ReleaseInfo.Version+" • "+Lang.T("report.private"),AutoSize=false,Dock=DockStyle.Top,Height=64,TextAlign=ContentAlignment.MiddleLeft,Font=Brand.Font(9),Margin=new Padding(10,0,0,0),BackColor=Color.Transparent};
   foot.Controls.Add(send,0,0);foot.Controls.Add(note,1,0);body.Controls.Add(foot,0,9);
    bool submitted=false;
    send.Click+=async(s,e)=>{if(submitted){Close();return;}if(String.IsNullOrWhiteSpace(title.Text)||String.IsNullOrWhiteSpace(description.Text)){note.Text=Lang.T("report.required");note.ForeColor=Fdm.Of(GlassDark).Danger;title.Focus();return;}
    var report=new ProblemReport{category=category.Text,title=title.Text.Trim(),description=description.Text.Trim(),contact=contact.Text.Trim()};
    try{ProblemReports.Enqueue(report);send.Enabled=false;note.Text=Lang.T("report.sending");bool sent=await ProblemReports.Flush(report.id);if(IsDisposed)return;note.Text=sent?Lang.T("report.sent"):Lang.T("report.saved");note.ForeColor=Fdm.Of(GlassDark).Success;submitted=true;send.Text=Lang.T("action.close");send.Enabled=true;}catch(Exception ex){if(IsDisposed)return;note.Text=ex.Message;note.ForeColor=Fdm.Of(GlassDark).Danger;send.Enabled=true;}
    };
   FdmTheme.Apply(this,this,false);
  }
 }
}
