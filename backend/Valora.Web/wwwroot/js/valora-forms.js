(function(){
  'use strict';
  document.addEventListener('submit',event=>{const form=event.target;if(!(form instanceof HTMLFormElement))return;if(!form.checkValidity()){event.preventDefault();event.stopImmediatePropagation();form.classList.add('was-validated');const first=form.querySelector(':invalid');first?.focus();window.ValoraUI?.announce('Revise os campos destacados antes de continuar.','error');return;}const button=event.submitter;if(button instanceof HTMLButtonElement){window.ValoraUI?.setBusy(button,true,button.dataset.loadingText||'Salvando…');window.setTimeout(()=>window.ValoraUI?.setBusy(button,false),15000);}},true);
  document.addEventListener('input',event=>{const field=event.target;if(field instanceof HTMLInputElement||field instanceof HTMLSelectElement||field instanceof HTMLTextAreaElement){field.classList.toggle('is-invalid',!field.validity.valid);field.setAttribute('aria-invalid',String(!field.validity.valid));}});
}());
