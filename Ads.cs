using System;
using System.IO;
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Reflection;
using System.Collections.Generic;

namespace MehrDownload {
 class Advertisement {
  public bool active=true;
  public string title="",description="",ctaText="",link="https://hamtavar.ir",imageUrl="/assets/hamtavar.png";
  public string titleEn="",descriptionEn="",ctaTextEn="";
  public string productId="Hamtavar",logoResource="Products.Hamtavar.png";
  public int revision=1;
 }
 class AdPlacement : GlassPanel {
  public const string Origin="https://downloader.hamtavar.ir";
  public const string LegacyOrigin="https://asan-download.woolly-frog-8626.chatgpt.site";
  readonly Label tag=new Label(),heading=new Label(),description=new Label();
  readonly PictureBox picture=new PictureBox();readonly GlassButton action=new GlassButton();
  TableLayoutPanel adRow;
  internal const int RotationInterval=12000;
  readonly Timer refresh=new Timer(),rotation=new Timer();bool loading,disposed,campaignActive=true;
  readonly List<Advertisement> catalog=ProductAds.Create();Advertisement ad;
  int currentIndex;string displayedImage="";Image remoteHamtavarImage;
  internal int ProductCount{get{return catalog.Count;}}
  internal int CurrentIndex{get{return currentIndex;}}
  readonly string cache;
  public AdPlacement(bool online) {
   cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MehrDownload","ad.json");
   ad=catalog[0];
   Padding=new Padding(12);Radius=16;Dock=DockStyle.Fill;Elevated=true;
   var row=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};
   adRow=row;
   row.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,86));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,146));
   Controls.Add(row);
   var badge=new GlassPanel{Dock=DockStyle.Fill,Radius=12,Padding=new Padding(8),Margin=new Padding(0,0,12,0),Elevated=false};
   picture.Dock=DockStyle.Fill;picture.SizeMode=PictureBoxSizeMode.Zoom;picture.BackColor=Color.Transparent;
   badge.Controls.Add(picture);row.Controls.Add(badge,0,0);
   var texts=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=Padding.Empty};
   texts.RowStyles.Add(new RowStyle(SizeType.Absolute,26));
   texts.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   var titleRow=new SmoothTableLayout{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};
   titleRow.RowStyles.Add(new RowStyle(SizeType.Percent,100));
   titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));
   titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
   tag.Text=Lang.T("ad.tag");tag.Dock=DockStyle.Fill;tag.BackColor=Color.Transparent;tag.Font=Brand.Font(8.5f);tag.TextAlign=ContentAlignment.MiddleLeft;tag.AutoSize=false;
   heading.Dock=DockStyle.Fill;heading.BackColor=Color.Transparent;heading.Font=Brand.Font(10.5f,FontStyle.Regular);heading.AutoEllipsis=true;heading.Margin=Padding.Empty;heading.TextAlign=ContentAlignment.MiddleLeft;
   description.Dock=DockStyle.Fill;description.BackColor=Color.Transparent;description.Font=Brand.Font(9);description.AutoEllipsis=true;description.AutoSize=false;description.Margin=Padding.Empty;description.TextAlign=ContentAlignment.MiddleLeft;
   titleRow.Controls.Add(tag,0,0);titleRow.Controls.Add(heading,1,0);
   texts.Controls.Add(titleRow,0,0);texts.Controls.Add(description,0,1);row.Controls.Add(texts,1,0);
   action.Dock=DockStyle.Fill;action.Font=Brand.Font(10);action.Margin=new Padding(12,0,0,0);row.Controls.Add(action,2,0);
   action.Click+=(s,e)=>{Uri target;if(Uri.TryCreate(ad.link,UriKind.Absolute,out target)&&(target.Scheme=="https"||target.Scheme=="http"))try{System.Diagnostics.Process.Start(target.AbsoluteUri);}catch(Exception ex){description.Text=ex.Message;}};
   // Leave the built-in copy empty so language changes always resolve fresh defaults.
   if(online && File.Exists(cache))try{Advertisement cached=new JavaScriptSerializer().Deserialize<Advertisement>(File.ReadAllText(cache));if(cached!=null&&!String.IsNullOrEmpty(cached.title)){cached.productId="Hamtavar";cached.logoResource="Products.Hamtavar.png";catalog[0]=ad=cached;campaignActive=cached.active;}}catch{}
   Apply();refresh.Interval=5*60*1000;refresh.Tick+=async(s,e)=>await UpdateAd();
   if(online){refresh.Start();HandleCreated+=async(s,e)=>await UpdateAd();}
   rotation.Interval=RotationInterval;rotation.Tick+=(s,e)=>{if(CanRotate)AdvanceAdvertisement();};
   VisibleChanged+=(s,e)=>{if(Visible)rotation.Start();else rotation.Stop();};rotation.Start();
  }
  internal bool CanRotate{get{return ShouldAdvance(Cursor.Position,ContainsFocus);}}
  internal bool ShouldAdvance(Point cursor,bool focused){return !disposed&&Visible&&campaignActive&&!focused&&!ClientRectangle.Contains(PointToClient(cursor));}
  internal void AdvanceAdvertisement(){if(disposed)return;currentIndex=(currentIndex+1)%catalog.Count;ad=catalog[currentIndex];Apply();}
  public static Image Embedded(string name){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var i=Image.FromStream(s))return new Bitmap(i);}
  public void ApplyLanguage(){
   // Native labels mirror logical Left alignment when they inherit RTL.
   var align=ContentAlignment.MiddleLeft;
   tag.TextAlign=heading.TextAlign=description.TextAlign=align;
   Apply();
  }
  void Apply(){
   tag.Text=Lang.T("ad.tag")+" · "+CompletedFile.PersianDigits((currentIndex+1)+"/"+catalog.Count);
   if(Lang.English){
    heading.Text=!String.IsNullOrEmpty(ad.titleEn)?ad.titleEn:!String.IsNullOrEmpty(ad.title)?ad.title:Lang.T("ad.default.title");
    description.Text=!String.IsNullOrEmpty(ad.descriptionEn)?ad.descriptionEn:!String.IsNullOrEmpty(ad.description)?ad.description:Lang.T("ad.default.description");
    action.Text=!String.IsNullOrEmpty(ad.ctaTextEn)?ad.ctaTextEn:!String.IsNullOrEmpty(ad.ctaText)?ad.ctaText:Lang.T("ad.default.cta");
   }else{
    heading.Text=String.IsNullOrEmpty(ad.title)?Lang.T("ad.default.title"):ad.title;
    description.Text=String.IsNullOrEmpty(ad.description)?Lang.T("ad.default.description"):ad.description;
    action.Text=String.IsNullOrEmpty(ad.ctaText)?Lang.T("ad.default.cta"):ad.ctaText;
   }
   action.AccessibleName=action.Text+": "+heading.Text;
   picture.AccessibleName=heading.Text;
   Uri target;bool hasLink=Uri.TryCreate(ad.link,UriKind.Absolute,out target)&&(target.Scheme=="https"||target.Scheme=="http");
   action.Visible=hasLink;adRow.ColumnStyles[2].Width=hasLink?146:0;
   string imageId=ad.productId+"|"+ad.revision+"|"+ad.imageUrl;
   if(imageId!=displayedImage){
    Image image=null;
    try{image=ad.productId=="Hamtavar"&&remoteHamtavarImage!=null?new Bitmap(remoteHamtavarImage):Embedded(ad.logoResource);}catch{image=Embedded("Hamtavar.png");}
    var old=picture.Image;picture.Image=image;displayedImage=imageId;if(old!=null)old.Dispose();
   }
   foreach(Control c in Controls)c.Visible=campaignActive&&ad.active;
  }
  static byte[] Get(string url,int maximum) {
   try{return GetRaw(url,maximum);}catch{if(url.StartsWith(Origin+"/",StringComparison.Ordinal))return GetRaw(LegacyOrigin+url.Substring(Origin.Length),maximum);throw;}
  }
  static byte[] GetRaw(string url,int maximum) {
   var request=DownloadEngine.Request(url);request.Timeout=request.ReadWriteTimeout=8000;
   using(var response=(HttpWebResponse)request.GetResponse())using(var input=response.GetResponseStream())using(var output=new MemoryStream()) {
    if(response.ContentLength>maximum)throw new IOException("Ad too large");byte[] buffer=new byte[16384];int read;
    while((read=input.Read(buffer,0,buffer.Length))>0){if(output.Length+read>maximum)throw new IOException("Ad too large");output.Write(buffer,0,read);}return output.ToArray();
   }
  }
  async Task UpdateAd(){
   if(loading||disposed)return;loading=true;
   try {
    var result=await Task.Run(()=>{
     var next=new JavaScriptSerializer().Deserialize<Advertisement>(System.Text.Encoding.UTF8.GetString(Get(Origin+"/api/public/ad",32768)));
     if(next==null||next.title==null||next.title.Length>100||next.description==null||next.description.Length>240||next.ctaText==null||next.ctaText.Length>40)throw new IOException("Invalid advertisement");
     if(next.titleEn!=null&&next.titleEn.Length>100)throw new IOException("Invalid advertisement");
     if(next.descriptionEn!=null&&next.descriptionEn.Length>240)throw new IOException("Invalid advertisement");
     if(next.ctaTextEn!=null&&next.ctaTextEn.Length>40)throw new IOException("Invalid advertisement");
     Uri link;if(!Uri.TryCreate(next.link,UriKind.Absolute,out link)||(link.Scheme!="https"&&link.Scheme!="http"))throw new IOException("Invalid link");
     if(next.imageUrl!="/assets/hamtavar.png"&&!System.Text.RegularExpressions.Regex.IsMatch(next.imageUrl??"",@"^/media/[a-f0-9]{32}\.(png|jpg|webp)$"))throw new IOException("Invalid image path");
     return next;
    });
    Image image=null;
    if(result.active&&result.imageUrl!="/assets/hamtavar.png")try{byte[] bytes=await Task.Run(()=>Get(Origin+result.imageUrl,4*1024*1024));using(var s=new MemoryStream(bytes))using(var raw=Image.FromStream(s)){if(raw.Width>6000||raw.Height>6000)throw new IOException("Invalid image size");image=new Bitmap(raw);}}catch{}
    if(disposed){if(image!=null)image.Dispose();return;}
    result.productId="Hamtavar";result.logoResource="Products.Hamtavar.png";catalog[0]=result;campaignActive=result.active;
    if(remoteHamtavarImage!=null)remoteHamtavarImage.Dispose();remoteHamtavarImage=image;
    if(currentIndex==0){ad=result;displayedImage="";}Apply();
    Directory.CreateDirectory(Path.GetDirectoryName(cache));File.WriteAllText(cache,new JavaScriptSerializer().Serialize(result));
   }catch{}finally{loading=false;}
  }
  protected override void Dispose(bool disposing){disposed=true;if(disposing){rotation.Stop();rotation.Dispose();refresh.Stop();refresh.Dispose();if(picture.Image!=null)picture.Image.Dispose();if(remoteHamtavarImage!=null)remoteHamtavarImage.Dispose();}base.Dispose(disposing);}
 }
}
