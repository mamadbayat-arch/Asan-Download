using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MehrDownload {

 // Central language table. Every piece of user facing text is looked up here so the
 // application can be switched between Persian (right to left) and English (left to right).
 static class Lang {
  public static bool English=false;
  public static bool Rtl { get { return !English; } }
  // Keep the same regular Vazir metrics when switching languages.
  public const float PersianScale=1f;
  public static float Scale { get { return English?1f:PersianScale; } }

  static readonly Dictionary<string,string[]> table=new Dictionary<string,string[]>();
  static Lang(){
   Add("app.name","آسان دانلود","Asan Download");
   Add("app.tagline","دانلودر فارسی ویندوز","Windows download manager");
   Add("logo.name","لوگوی دانلود","Download logo");

   Add("win.minimize","کوچک کردن پنجره","Minimize window");
   Add("win.maximize","بزرگ کردن پنجره","Maximize window");
   Add("win.restore","بازگرداندن اندازهٔ پنجره","Restore window");
   Add("win.close","بستن برنامه","Close app");

   Add("action.add","افزودن دانلود","Add download");
   Add("action.paste","چسباندن","Paste");
   Add("action.start","شروع دانلود","Start download");
   Add("action.resume","شروع / ادامه","Start / resume");
   Add("action.new","دانلود تازه","New download");
   Add("action.stop","توقف","Pause");
   Add("action.folder","انتخاب پوشه","Choose folder");
   Add("action.close","بستن","Close");
   Add("action.extensionFolder","پوشهٔ افزونه","Extension folder");
   Add("action.toggleSidebar","جمع کردن یا باز کردن فهرست کناری","Collapse or expand the side bar");

   Add("link.view","مشاهدهٔ لینک","View link");
   Add("link.edit","ویرایش لینک کامل","Edit full link");
   Add("link.placeholder","افزودن لینک دانلود…","Add a download link…");
   Add("link.dialog.title","لینک کامل دانلود","Full download link");
   Add("link.dialog.hint","لینک کامل را ببینید یا ویرایش کنید","See or edit the full link");
   Add("link.dialog.confirm","تأیید لینک","Confirm link");
   Add("link.label","لینک دانلود","Download link");
   Add("link.path","مسیر فایل","File path");

   Add("sort.newest","جدیدترین","Newest");
   Add("sort.oldest","قدیمی‌ترین","Oldest");
   Add("sort.name","نام فایل","File name");
   Add("sort.size","حجم","Size");

   Add("filter.all","همهٔ فایل‌ها","All files");
   Add("filter.active","در حال دانلود","Downloading");
   Add("filter.completed","کامل‌شده","Completed");
   Add("filter.queued","در صف","Queued");
   Add("filter.allNav","همهٔ دانلودها","All downloads");
   Add("filter.queuedNav","در صف دانلود","Queued");

   Add("search.placeholder","جست‌وجو در دانلودها…","Search in downloads…");
   Add("search.label","جست‌وجو در دانلودها","Search in downloads");

   Add("stat.speed","سرعت دریافت","Download speed");
   Add("stat.completed","فایل‌های کامل‌شده","Completed files");
   Add("stat.size","حجم دریافت‌شده","Downloaded size");

   Add("nav.downloads","پوشهٔ دانلودها","Download folder");
   Add("nav.setFolder","تنظیم پوشه","Set download folder");
   Add("nav.report","گزارش مشکل","Report a problem");
   Add("nav.theme","تیره / روشن","Dark / light");
   Add("nav.integration","ویندوز و مرورگر","Windows and browser");
   Add("nav.language","زبان برنامه","Application language");
   Add("nav.languageValue","English","فارسی");

   Add("folder.required","ابتدا پوشهٔ دانلود را انتخاب کنید","Choose a download folder first");
   Add("folder.ready","پوشهٔ دانلود آماده است؛ لینک را بچسبانید.","Download folder is ready, paste a link.");
   Add("folder.target","فایل: ","File: ");
   Add("folder.choose","پوشهٔ دانلود را انتخاب کنید","Choose the download folder");
   Add("folder.incomplete","پوشهٔ دانلود را انتخاب کنید","Choose a download folder");

   Add("table.name","نام فایل","Name");
   Add("table.size","حجم","Size");
   Add("table.status","وضعیت","Status");
   Add("table.speed","سرعت","Speed");
   Add("table.date","تاریخ","Added");
   Add("table.noResults","فایلی با این نام پیدا نشد.","No downloads match your search.");
   Add("table.emptyFilter","در این بخش دانلودی وجود ندارد.","No downloads in this category.");
   Add("table.empty","هنوز دانلودی ندارید؛ لینک را بالا وارد کنید.","No downloads yet. Paste a link above to get started.");
   Add("table.emptyTitle","هنوز چیزی اینجا نیست","Nothing here yet");
   Add("drop.hint","لینک دانلود را اینجا رها کنید","Drop a download link here");

   Add("row.completed","کامل‌شده","Completed");
   Add("row.queued","در صف دانلود","Queued");
   Add("row.now","همین حالا","Just now");
   Add("row.duration","مدت ","Duration ");
   Add("row.received","دریافت‌شده ","Received ");
   Add("row.downloading","در حال دانلود • ","Downloading • ");

   Add("status.idle","برای شروع، لینک فایل را وارد کنید.","Paste a file link to get started.");
   Add("status.stopping","در حال توقف…","Pausing…");
   Add("status.queued","در صف دانلود: ","Queued: ");
   Add("status.connections","اتصال","connections");
   Add("status.downloading","در حال دانلود • ","Downloading • ");

   Add("toast.completed","دانلود کامل شد: ","Download finished: ");
   Add("toast.failed","دانلود ناموفق بود","Download failed");
   Add("toast.queued","به صف دانلود اضافه شد","Added to the download queue");

   Add("error.invalidUrl","لینک مستقیم معتبر وارد کنید.","Enter a valid direct link.");
   Add("error.exists","این فایل وجود دارد؛ نام تازه‌ای انتخاب کنید.","This file already exists, pick another name.");
   Add("error.clipboard","کلیپ‌بورد متنی ندارد.","The clipboard has no text.");
   Add("error.settings","تنظیمات قبلی خوانده نشد؛ پوشه را دوباره انتخاب کنید.","Previous settings could not be read, choose the folder again.");
   Add("error.generic","مشکل دانلود","Download problem");

   Add("connections.label","تعداد اتصال هم‌زمان","Simultaneous connections");
   Add("connections.hint","تعداد اتصال هم‌زمان؛ بین ۱ تا ۱۶","Simultaneous connections, from 1 to 16");

   Add("prompt.title","تأیید دانلود","Confirm download");
   Add("prompt.question","این فایل را دانلود می‌کنید؟","Download this file?");
   Add("prompt.queued","این فایل به صف دانلود اضافه شود؟","Add this file to the queue?");
   Add("prompt.hint","پیش از شروع، نام فایل و سایت را بررسی کنید.","Check the file name and website before downloading.");
   Add("prompt.download","دانلود کن","Download");
   Add("prompt.enqueue","افزودن به صف","Add to queue");
   Add("prompt.cancel","انصراف","Cancel");

   Add("report.title","گزارش مشکل","Report a problem");
   Add("report.heading","کمک کنید آسان دانلود بهتر شود","Help us make Asan Download better");
   Add("report.category","نوع مشکل","Category");
   Add("report.category.ui","ظاهر برنامه","App appearance");
   Add("report.category.download","دانلود","Downloading");
   Add("report.category.update","به‌روزرسانی","Updates");
   Add("report.category.other","سایر","Other");
   Add("report.titleField","عنوان کوتاه","Short title");
   Add("report.body","چه اتفاقی افتاد؟ انتظار داشتید چه شود؟","What happened and what did you expect?");
   Add("report.contact","ایمیل یا شماره تماس (اختیاری)","Email or phone (optional)");
   Add("report.send","ارسال گزارش","Send report");
   Add("report.version","نسخهٔ ","Version ");
   Add("report.sending","در حال ارسال…","Sending…");
   Add("report.required","عنوان و توضیح مشکل را وارد کنید.","Enter a title and a description.");
   Add("report.sent","گزارش شما ارسال شد. ممنون از همراهی شما.","Your report was sent, thank you.");
   Add("report.saved","گزارش ذخیره شد؛ با اتصال اینترنت خودکار ارسال می‌شود.","The report is stored and will be sent automatically.");

   Add("update.title","به‌روزرسانی آسان دانلود","Asan Download update");
   Add("update.heading","همیشه نسخهٔ تازه","Always the newest version");
   Add("update.checking","در حال بررسی نسخهٔ جدید…","Checking for a new version…");
   Add("update.downloading","نسخهٔ ","Version ");
   Add("update.installing","در حال نصب نسخهٔ تازه…","Installing the new version…");
   Add("update.receiving","در حال دریافت است…","is downloading…");
   Add("report.private","فقط متن این فرم ارسال می‌شود.","only the text of this form is sent.");
   Add("report.full","فهرست گزارش‌ها پر است. لطفاً کمی بعد دوباره تلاش کنید.","The report queue is full, please try again later.");
   Add("update.enter","ورود به برنامه","Enter the app");

   Add("integration.title","ویندوز و مرورگر","Windows and browser");
   Add("integration.startup","اجرای آسان دانلود با روشن‌شدن کامپیوتر","Start Asan Download when the computer turns on");
   Add("integration.help","برای دریافت دانلودهای کروم یا اج:\n۱. صفحهٔ افزونه‌ها را باز کنید و Developer mode را روشن کنید.\n۲. Load unpacked را بزنید و پوشهٔ افزونهٔ آسان دانلود را انتخاب کنید.\nدانلودهای نیازمند ورود به سایت، در مرورگر ادامه می‌یابند.",
    "To catch Chrome or Edge downloads:\n1. Open the extensions page and turn on Developer mode.\n2. Choose Load unpacked and select the Asan Download extension folder.\nDownloads that need a sign in keep running in the browser.");
   Add("integration.extNote","برای Chrome و Microsoft Edge · ابتدا برنامهٔ ویندوز را نصب کنید.","For Chrome and Microsoft Edge, install the Windows app first.");

   Add("action.addNew","افزودن دانلود تازه","Add a new download");
   Add("action.batch","افزودن گروهی","Add batch");
   Add("batch.title","دانلود گروهی","Batch download");
   Add("batch.heading","چند لینک را یک‌جا دانلود کنید","Download several links at once");
   Add("batch.hint","هر لینک را در یک خط بنویسید یا بچسبانید؛ دانلودها به ترتیب در صف قرار می‌گیرند.","Put every link on its own line, they are queued and downloaded one after another.");
   Add("batch.start","افزودن به صف و شروع","Queue and start");
   Add("batch.empty","هیچ لینکی پیدا نشد.","No link was found.");
   Add("batch.invalid","همهٔ لینک‌ها نامعتبر یا تکراری بودند.","All links were invalid or already queued.");
   Add("batch.added","{0} لینک به صف دانلود اضافه شد","{0} links added to the queue");
   Add("batch.limit","فقط ۱۰۰ لینک در صف نگه داشته می‌شود.","The queue holds up to 100 links.");
   Add("stat.speedTip","مگابایت بر ثانیه؛ کلیک برای نمایش مگابیت","Megabytes per second, click for megabits");
   Add("menu.open","باز کردن فایل","Open file");
   Add("menu.reveal","نمایش در پوشه","Show in folder");
   Add("table.missing","فایل جابه‌جا یا حذف شده است:\n","The file was moved or deleted:\n");
   Add("folder.dialog","پوشهٔ ذخیرهٔ همهٔ دانلودها را انتخاب کنید","Choose where every download is stored");
   Add("status.downloading","در حال دانلود…","Downloading…");
   Add("speed.bits","سرعت دانلود به مگابیت بر ثانیه","Download speed in megabits per second");
   Add("speed.bytes","سرعت دانلود به مگابایت بر ثانیه","Download speed in megabytes per second");
   Add("speed.bitsTip","مگابیت بر ثانیه؛ کلیک برای نمایش مگابایت","Megabits per second, click for megabytes");
   Add("speed.bytesTip","مگابایت بر ثانیه؛ کلیک برای نمایش مگابیت","Megabytes per second, click for megabits");
   Add("ad.tag","تبلیغات","Sponsored");
   Add("ad.default.title","همتوار؛ همراه روزهای منظم‌تر","Hamtavar, a calmer way to plan your days");
   Add("ad.default.description","مدیریت کارها، دخل‌وخرج و هدف‌ها در یک برنامهٔ فارسی.","Tasks, money and goals inside one Persian app.");
   Add("ad.default.cta","آشنایی با همتوار","Meet Hamtavar");
  }
  static void Add(string key,string fa,string en){ table[key]=new[]{fa,en}; }
  public static string T(string key){
   string[] value;
   if(!table.TryGetValue(key,out value))return key;
   return English?value[1]:value[0];
  }
  public static bool Has(string key){ return table.ContainsKey(key); }
  public static IEnumerable<string> Keys { get { return table.Keys; } }

  // Applies the reading direction to a whole control tree.
  public static void Apply(Control root){
   if(root is Form)((Form)root).RightToLeft=Rtl?RightToLeft.Yes:RightToLeft.No;
   if(root is TableLayoutPanel)((TableLayoutPanel)root).RightToLeft=Rtl?RightToLeft.Yes:RightToLeft.No;
   foreach(Control child in root.Controls)Apply(child);
  }
 }
}
