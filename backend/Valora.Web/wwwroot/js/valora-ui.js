(function(){
  'use strict';
  window.ValoraUI=Object.freeze({
    setBusy(button,busy,label){if(!button)return;button.disabled=busy;button.setAttribute('aria-busy',String(busy));button.classList.toggle('btn-loading',busy);if(label){button.dataset.defaultLabel||=button.textContent;button.textContent=busy?label:button.dataset.defaultLabel;}},
    announce(message,kind='info'){const host=document.querySelector('[data-toast-host],#toastContainer');if(!host)return;const item=document.createElement('div');item.className='valora-toast is-'+kind;item.setAttribute('role',kind==='error'?'alert':'status');item.textContent=message;host.append(item);setTimeout(()=>item.remove(),5000);}
  });
}());
