(() => {
 const form=document.querySelector('[data-lead-form]'); if(!form) return;
 const feedback=form.querySelector('[data-feedback]'); const button=form.querySelector('button[type="submit"]');
 form.addEventListener('submit',async event=>{event.preventDefault(); if(!form.reportValidity()) return;
  button.disabled=true; button.textContent='Preparando diagnóstico…'; feedback.hidden=true;
  const data=new FormData(form); const body=Object.fromEntries(data.entries());
  body.consentAccepted=data.has('consentAccepted'); body.communicationConsent=data.has('communicationConsent'); body.source='public_free_diagnostic';
  try { const response=await fetch('/bff/public/diagnostic/start',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}); const payload=await response.json();
   if(!response.ok) throw new Error(payload.message||'Não foi possível iniciar agora.');
   sessionStorage.setItem('valoraPublicDiagnostic',JSON.stringify({leadId:payload.leadId,sessionId:payload.sessionId}));
   window.location.assign('/diagnostico-gratuito?sessionId='+encodeURIComponent(payload.sessionId));
  } catch(error){feedback.textContent=error.message||'Não foi possível iniciar agora. Tente novamente.';feedback.hidden=false;button.disabled=false;button.textContent='Iniciar diagnóstico oficial';}
 });
})();
