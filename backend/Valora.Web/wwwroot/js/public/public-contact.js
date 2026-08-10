(function(){
  'use strict';
  const form=document.querySelector('[data-contact-form]');
  if(!form)return;
  const phone=form.elements.phone;
  phone.addEventListener('input',()=>{const value=phone.value.replace(/\D/g,'').slice(0,11);phone.value=value.length>10?value.replace(/(\d{2})(\d{5})(\d{0,4})/,'($1) $2-$3'):value.replace(/(\d{2})(\d{4})(\d{0,4})/,'($1) $2-$3');});
  ValoraPublic.bindOnce(form,async data=>{
    const summary=form.querySelector('[data-form-summary]');
    summary.hidden=true;
    if(!form.checkValidity()){
      form.classList.add('was-validated');
      summary.textContent='Revise os campos obrigatórios, o e-mail e o aceite de privacidade.';
      summary.hidden=false;
      form.querySelector(':invalid')?.focus();
      return;
    }
    const message=`Olá, sou ${data.get('name')}. ${data.get('message')} Meu e-mail é ${data.get('email')}${data.get('phone')?` e meu telefone é ${data.get('phone')}`:''}.`;
    const link=`https://wa.me/5591992545353?text=${encodeURIComponent(message)}`;
    form.querySelector('[data-contact-success]').hidden=false;
    form.querySelector('[data-whatsapp-link]')?.setAttribute('href',link);
    ValoraPublic.toast('Tudo certo. Abra o WhatsApp para concluir o atendimento.','success');
    window.open(link,'_blank','noopener,noreferrer');
  });
})();
