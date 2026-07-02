(function(){
  function ensureOverlay(){
    let el=document.getElementById('loadingOverlay');
    if(!el){el=document.createElement('div');el.id='loadingOverlay';el.className='d-none position-fixed top-0 start-0 end-0 p-3 text-center';el.style.zIndex='2000';el.innerHTML='<span class="badge rounded-pill text-bg-primary shadow"><span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span><span id="loadingMessage">Processando...</span></span>';document.body.appendChild(el);} return el;
  }
  window.setGlobalLoading=function(active,message){const el=ensureOverlay();const msg=document.getElementById('loadingMessage'); if(msg)msg.textContent=message||'Processando...'; el.classList.toggle('d-none',!active); document.body.classList.toggle('is-loading',!!active);};
  window.withLoading=async function(message,asyncFunction){setGlobalLoading(true,message); try{return await asyncFunction();} finally{setGlobalLoading(false);}};
  window.setButtonLoading=function(button,active,text){if(!button)return; if(active){button.dataset.originalHtml=button.innerHTML; button.disabled=true; button.innerHTML='<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>'+(text||'Processando...');} else {button.disabled=false; if(button.dataset.originalHtml)button.innerHTML=button.dataset.originalHtml;}};
  window.Loading={show:m=>setGlobalLoading(true,m||'Carregando...'),hide:()=>setGlobalLoading(false)};
})();
