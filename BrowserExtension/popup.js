const strings={
 fa:{app:'آسان دانلود',toggle:'دریافت خودکار دانلودها',hint:'فقط دانلودهایی که با کلیک یا کشیدن شما آغاز می‌شوند به برنامه فرستاده می‌شوند.',connect:'بررسی اتصال به برنامه',idle:'برای شروع، دکمهٔ بررسی اتصال را بزنید.',checking:'در حال بررسی…',ok:'اتصال برقرار است؛ دانلودها به آسان دانلود می‌روند.',missing:'برنامه پاسخ نداد؛ آسان دانلود را باز کنید.',notFound:'برنامه پیدا نشد؛ آسان دانلود را باز و افزونه را Reload کنید.',version:'نسخهٔ افزونه: ۱.۷.۲',switch:'EN'},
 en:{app:'Asan Download',toggle:'Catch downloads automatically',hint:'Only downloads that you start with a click or a drag are sent to the app.',connect:'Check the app connection',idle:'Press the check button to start.',checking:'Checking…',ok:'Connected, downloads now go to Asan Download.',missing:'The app did not answer, please open Asan Download.',notFound:'The app was not found, open Asan Download and reload the extension.',version:'Extension version: 1.7.2',switch:'فا'}
};
const checkbox=document.getElementById('enabled');
const status=document.getElementById('status');
const connect=document.getElementById('connect');
let english=false;

function apply(){
 const text=strings[english?'en':'fa'];
 document.documentElement.lang=english?'en':'fa';
 document.documentElement.dir=english?'ltr':'rtl';
 document.getElementById('app-name').textContent=text.app;
 document.getElementById('toggle-label').textContent=text.toggle;
 document.getElementById('hint').textContent=text.hint;
 document.getElementById('version').textContent=text.version;
 document.getElementById('lang').textContent=text.switch;
 connect.textContent=english?text.connect:document.getElementById('connect').dataset.value||text.connect;
 status.classList.remove('ok','bad');
 if(!status.dataset.value)status.textContent=text.idle;
}

chrome.storage.local.get({enabled:true,english:false}).then(data=>{
 english=!!data.english;
 connect.dataset.value=strings[english?'en':'fa'].connect;
 apply();
 checkbox.checked=data.enabled;
});
document.getElementById('lang').addEventListener('click',()=>{
 english=!english;
 chrome.storage.local.set({english:english});
 connect.dataset.value=strings[english?'en':'fa'].connect;
 apply();
 status.textContent=strings[english?'en':'fa'].idle;
 status.dataset.value='';
});
checkbox.addEventListener('change',()=>chrome.storage.local.set({enabled:checkbox.checked}));
connect.addEventListener('click',async()=>{
 const text=strings[english?'en':'fa'];
 connect.disabled=true;status.textContent=text.checking;status.dataset.value='';
 try{
  const result=await chrome.runtime.sendNativeMessage('ir.hamtavar.asandownload',{action:'ping'});
  const ok=result&&result.accepted;
  status.textContent=ok?text.ok:text.missing;
  status.dataset.value=status.textContent;
  status.classList.add(ok?'ok':'bad');
 }catch{
  status.textContent=text.notFound;
  status.dataset.value=status.textContent;
  status.classList.add('bad');
 }finally{connect.disabled=false;}
});
