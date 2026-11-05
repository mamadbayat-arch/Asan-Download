using System.Drawing;
using System.Drawing.Drawing2D;

namespace MehrDownload {
 enum IconKind { Plus,Download,Clipboard,Folder,FolderPlus,Check,Clock,Report,Monitor,Search,Moon,Pause,Stop,Globe,Chevron,Play,More,Refresh,Sort,Filter,Sidebar,ListPlus,Chart,Link,Archive,Gauge }
 // A single 24-unit outline system keeps weights, caps and alignment consistent.
 static class Icons {
  internal static void Draw(Graphics g,IconKind kind,float x,float y,Color ink,float size){
   var state=g.Save();g.TranslateTransform(x-size/2,y-size/2);g.ScaleTransform(size/24,size/24);g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var p=new Pen(ink,1.8f)){p.StartCap=p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
    switch(kind){
     case IconKind.Plus:g.DrawLine(p,5,12,19,12);g.DrawLine(p,12,5,12,19);break;
     case IconKind.Download:g.DrawLine(p,12,3,12,15);Lines(g,p,7,10,12,15,17,10);Lines(g,p,4,16,4,20,20,20,20,16);break;
     case IconKind.Clipboard:Round(g,p,5,5,14,16,2);Round(g,p,9,3,6,4,1);g.DrawLine(p,9,12,15,12);g.DrawLine(p,9,16,13,16);break;
     case IconKind.Folder:case IconKind.FolderPlus:Lines(g,p,3,8,3,5,9,5,12,8,21,8,21,19,3,19,3,8);if(kind==IconKind.FolderPlus){g.DrawLine(p,9,13,15,13);g.DrawLine(p,12,10,12,16);}break;
     case IconKind.Check:g.DrawEllipse(p,3,3,18,18);Lines(g,p,7,12,10.5f,15.5f,17,9);break;
     case IconKind.Clock:g.DrawEllipse(p,3,3,18,18);Lines(g,p,12,7,12,12,16,14);break;
     case IconKind.Report:Lines(g,p,4,4,20,4,21,5,21,17,20,18,8,18,3,21,3,5,4,4);g.DrawLine(p,12,8,12,12);g.DrawEllipse(p,11.7f,15,0.6f,0.6f);break;
     case IconKind.Monitor:Round(g,p,3,4,18,13,2);g.DrawLine(p,12,17,12,21);g.DrawLine(p,8,21,16,21);break;
     case IconKind.Search:g.DrawEllipse(p,3,3,13,13);g.DrawLine(p,14,14,21,21);break;
     case IconKind.Moon:using(var path=new GraphicsPath()){path.AddBezier(19,15,11,18,6,11,10,4);path.AddBezier(10,4,-1,8,3,23,15,21);path.AddBezier(15,21,18,20,20,17,19,15);g.DrawPath(p,path);}break;
     case IconKind.Pause:g.DrawLine(p,8,5,8,19);g.DrawLine(p,16,5,16,19);break;
     case IconKind.Stop:Round(g,p,5,5,14,14,2);break;
     case IconKind.Globe:g.DrawEllipse(p,3,3,18,18);g.DrawEllipse(p,8,3,8,18);g.DrawLine(p,3,12,21,12);break;
     case IconKind.Chevron:Lines(g,p,6,9,12,15,18,9);break;
     case IconKind.Play:Lines(g,p,7,4,20,12,7,20,7,4);break;
     case IconKind.More:for(int n=5;n<=19;n+=7)g.DrawEllipse(p,11,n-1,2,2);break;
     case IconKind.Refresh:g.DrawArc(p,4,4,16,16,40,295);Lines(g,p,20,3,20,9,14,9);break;
     case IconKind.Sort:g.DrawLine(p,3,6,12,6);g.DrawLine(p,3,12,10,12);g.DrawLine(p,3,18,7,18);g.DrawLine(p,18,5,18,19);Lines(g,p,14,15,18,19,22,15);break;
     case IconKind.Filter:Lines(g,p,3,4,21,4,14,12,14,19,10,21,10,12,3,4);break;
     case IconKind.Sidebar:Round(g,p,3,4,18,16,2);g.DrawLine(p,9,4,9,20);g.DrawLine(p,5.5f,8,6.5f,8);g.DrawLine(p,5.5f,12,6.5f,12);break;
     case IconKind.ListPlus:for(int n=5;n<=19;n+=7){g.DrawEllipse(p,3,n-0.5f,1,1);g.DrawLine(p,8,n,n==19?12:21,n);}g.DrawLine(p,19,15,19,23);g.DrawLine(p,15,19,23,19);break;
     case IconKind.Chart:g.DrawLine(p,5,20,5,12);g.DrawLine(p,12,20,12,4);g.DrawLine(p,19,20,19,8);break;
     case IconKind.Link:g.DrawArc(p,3,10,11,10,45,270);g.DrawArc(p,10,3,11,10,225,270);g.DrawLine(p,8,16,16,8);break;
     case IconKind.Archive:Round(g,p,3,4,18,5,1);Lines(g,p,5,9,5,20,19,20,19,9);g.DrawLine(p,10,13,14,13);break;
     case IconKind.Gauge:g.DrawArc(p,3,3,18,18,135,270);g.DrawLine(p,12,12,17,7);g.DrawEllipse(p,11,11,2,2);g.DrawLine(p,5,18,19,18);break;
    }
   }g.Restore(state);
  }
  static void Lines(Graphics g,Pen p,params float[] points){var path=new PointF[points.Length/2];for(int i=0;i<path.Length;i++)path[i]=new PointF(points[i*2],points[i*2+1]);g.DrawLines(p,path);}
  static void Round(Graphics g,Pen p,float x,float y,float width,float height,float radius){using(var path=new GraphicsPath()){float d=radius*2;path.AddArc(x,y,d,d,180,90);path.AddArc(x+width-d,y,d,d,270,90);path.AddArc(x+width-d,y+height-d,d,d,0,90);path.AddArc(x,y+height-d,d,d,90,90);path.CloseFigure();g.DrawPath(p,path);}}
  internal static IconKind Button(int glyph){switch(glyph){case 1:return IconKind.Plus;case 2:return IconKind.Clipboard;case 3:return IconKind.Download;case 4:return IconKind.FolderPlus;case 5:return IconKind.Refresh;case 6:return IconKind.Report;case 7:return IconKind.Monitor;case 8:return IconKind.Search;case 9:return IconKind.Chevron;case 10:return IconKind.Pause;case 11:return IconKind.Play;case 12:return IconKind.More;case 13:return IconKind.Refresh;case 14:return IconKind.Check;case 15:return IconKind.Clock;case 16:return IconKind.Sort;case 17:return IconKind.Filter;case 18:return IconKind.Sidebar;case 19:return IconKind.Chart;case 20:return IconKind.Link;case 21:return IconKind.Moon;default:return IconKind.ListPlus;}}
  internal static IconKind Navigation(int glyph){switch(glyph){case 0:return IconKind.Archive;case 1:return IconKind.Download;case 2:return IconKind.Check;case 3:return IconKind.Clock;case 4:return IconKind.FolderPlus;case 5:return IconKind.Folder;case 6:return IconKind.Report;case 7:return IconKind.Monitor;case 8:return IconKind.Search;case 9:return IconKind.Moon;case 10:return IconKind.Pause;case 11:return IconKind.Stop;default:return IconKind.Globe;}}
 }
}
