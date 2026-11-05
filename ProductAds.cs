using System.Collections.Generic;

namespace MehrDownload {
 static class ProductAds {
  internal static List<Advertisement> Create(){return new List<Advertisement>{
   Item("Hamtavar","همتوار","Hamtavar","مدیریت کارها، مشتریان و حسابداری کسب‌وکار در یک برنامه.","Tasks, customers and business accounting in one workspace.","https://hamtavar.ir"),
   Item("Diman","دیمان","Diman","سفارش‌گیری و حسابداری کافه و رستوران.","Ordering and accounting for cafes and restaurants.","https://dimanapp.ir"),
   Item("Timan","تیمان","Timan","دستیار مدیریت کلینیک و کارهای روزمرهٔ درمان.","A daily management assistant for clinics.","https://timanapp.ir"),
   Item("kalan","کالان","Kalan","حسابداری و انبارداری برای فروشگاه‌ها و اصناف.","Accounting and inventory for shops and retailers.","https://kalanapp.ir"),
   Item("youzan","یوزان","Youzan","فروش، پذیرش و کنترل ورود باشگاه و استخر.","Sales, reception and access control for gyms and pools.","https://youzanapp.ir"),
   Item("bandan","بندان","Bandan","مدیریت مشاور املاک و نمایشگاه خودرو.","Digital management for real estate and car dealerships.","https://bandanapp.ir"),
   Item("zaran","زران","Zaran","دستیار مدیریت فروشگاه‌ها و کسب‌وکارهای خدماتی.","A management assistant for shops and service businesses.","https://zaranapp.ir"),
   Item("Chaman","چمان","Chaman","نوبت‌دهی، مشتریان و حسابداری سالن زیبایی.","Appointments, customers and accounting for beauty salons.","https://chamanapp.ir"),
   Item("taran","تاران","Taran","مدیریت فروشگاه پوشاک، کیف، کفش و عینک.","Management for fashion, footwear and eyewear stores.","https://taranapp.ir"),
   Item("Hamtavar-Personal","همتوار شخصی","Hamtavar Personal","برنامه‌ریزی کارها، هزینه‌ها، عادت‌ها و هدف‌های شخصی.","Plan personal tasks, expenses, habits and goals.",""),
   Item("Jaryan","جریان","Jaryan","پیاده‌سازی جلسات و استخراج تصمیم‌ها و وظایف.","Transcribe meetings and extract decisions and tasks.","" )
  };}
  static Advertisement Item(string id,string fa,string en,string description,string descriptionEn,string link){
   return new Advertisement{productId=id,logoResource="Products."+id+".png",title=fa,titleEn=en,description=description,descriptionEn=descriptionEn,ctaText="آشنایی با محصول",ctaTextEn="Learn more",link=link};
  }
 }
}
