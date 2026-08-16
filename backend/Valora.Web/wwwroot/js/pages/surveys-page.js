(() => {
  'use strict';
  const host = document.querySelector('[data-page="surveys-page"]');
  if (!host) return;
  const dialog = document.querySelector('[data-survey-dialog]');
  const form = dialog.querySelector('[data-survey-form]');
  const error = host.querySelector('[data-error]');
  let surveys = [];
  let forms = [];
  const unwrap = value => value?.data ?? value ?? [];
  const text = value => { const span=document.createElement('span'); span.textContent=value==null?'':String(value); return span.innerHTML; };
  const statusLabel = {draft:'Rascunho',published:'Publicado',active:'Ativo',closed:'Encerrado',archived:'Arquivado'};
  const statusTone = {draft:'warning',published:'info',active:'success',closed:'danger',archived:'info'};
  const date = value => value ? new Intl.DateTimeFormat('pt-BR').format(new Date(value)) : 'Sem data';
  const showError = problem => { error.textContent=problem?.message||'Não foi possível concluir a operação.'; error.classList.remove('d-none'); };
  const notify = (kind,message) => window.Toast?.[kind]?.(message);

  function render() {
    const query=host.querySelector('[data-search]').value.trim().toLowerCase();
    const status=host.querySelector('[data-status]').value;
    const filtered=surveys.filter(item=>(!status||item.status===status)&&(!query||`${item.title} ${item.formName||item.form_name||''}`.toLowerCase().includes(query)));
    host.querySelector('[data-metric="total"]').textContent=surveys.length;
    ['active','draft','closed'].forEach(key=>host.querySelector(`[data-metric="${key}"]`).textContent=surveys.filter(x=>x.status===key).length);
    host.querySelector('[data-loading]').classList.add('d-none');
    host.querySelector('[data-table]').classList.toggle('d-none',!filtered.length);
    host.querySelector('[data-empty]').classList.toggle('d-none',!!filtered.length);
    host.querySelector('[data-items]').innerHTML=filtered.map(item=>`<tr><td><strong>${text(item.title)}</strong><small class="d-block text-muted">${text(item.description||'Sem descrição')}</small></td><td>${text(item.formName||item.form_name||'Formulário não identificado')}</td><td><small>${date(item.startsAt||item.starts_at)} — ${date(item.expiresAt||item.expires_at)}</small></td><td><span class="valora-status valora-status--${statusTone[item.status]||'info'}">${statusLabel[item.status]||text(item.status)}</span></td><td><div class="survey-actions"><button class="btn btn-sm btn-outline-secondary" type="button" data-action="edit" data-id="${item.id}" ${item.status!=='draft'?'disabled title="Somente rascunhos podem ser editados"':''}>Editar</button>${item.status==='draft'?`<button class="btn btn-sm btn-primary" type="button" data-action="publish" data-id="${item.id}">Publicar</button>`:''}${['published','active'].includes(item.status)?`<button class="btn btn-sm btn-outline-secondary" type="button" data-action="link" data-id="${item.id}">Copiar link</button><button class="btn btn-sm btn-outline-danger" type="button" data-action="close" data-id="${item.id}">Encerrar</button>`:''}<a class="btn btn-sm btn-outline-secondary" href="/Responses?surveyId=${item.id}">Respostas</a><a class="btn btn-sm btn-primary" href="/Diagnostics/${item.id}/Workspace">Abrir Workspace</a></div></td></tr>`).join('');
  }
  async function load(){ error.classList.add('d-none'); host.querySelector('[data-loading]').classList.remove('d-none'); try { const values=await Promise.all([SurveysApi.list(),FormsApi.list('?pageSize=100')]); surveys=unwrap(values[0]); forms=unwrap(values[1]); renderFormOptions(); render(); } catch(problem){ host.querySelector('[data-loading]').classList.add('d-none'); showError(problem); } }
  function renderFormOptions(selected=''){ const options=forms.filter(x=>x.status!=='archived').map(x=>`<option value="${x.id}" ${x.id===selected?'selected':''}>${text(x.name)} · ${x.questions||0} pergunta(s) · ${statusLabel[x.status]||x.status}</option>`).join(''); form.elements.formId.innerHTML='<option value="">Selecione um formulário</option>'+options; }
  function open(item){ form.reset(); form.elements.id.value=item?.id||''; form.elements.title.value=item?.title||''; form.elements.description.value=item?.description||''; renderFormOptions(item?.formId||item?.form_id||''); dialog.querySelector('h2').textContent=item?'Editar diagnóstico':'Novo diagnóstico'; dialog.showModal(); }
  async function status(id,value){ await SurveysApi.setStatus(id,value); notify('success',value==='closed'?'Diagnóstico encerrado.':'Diagnóstico publicado.'); await load(); }
  host.addEventListener('click',async event=>{ const button=event.target.closest('[data-action]'); if(!button)return; const item=surveys.find(x=>x.id===button.dataset.id); try { button.disabled=true; if(button.dataset.action==='edit')open(item); if(button.dataset.action==='publish'){ const selected=forms.find(x=>x.id===(item.formId||item.form_id)); if(!selected||selected.status!=='published'||Number(selected.questions)<1){ showError({message:'Publique um formulário com pelo menos uma pergunta antes de publicar o diagnóstico.'}); return; } if(confirm('Publicar este diagnóstico e habilitar a criação do link público?'))await status(item.id,'active'); } if(button.dataset.action==='close'&&confirm('Encerrar o diagnóstico? Novas respostas serão bloqueadas.'))await status(item.id,'closed'); if(button.dataset.action==='link'){ let links=unwrap(await SurveysApi.links(item.id)); let link=links.find(x=>x.status==='active'); if(!link)link=await SurveysApi.createLink(item.id); const url=new URL(link.publicUrl||link.public_url,location.origin).href; await navigator.clipboard.writeText(url); notify('success','Link público copiado.'); } } catch(problem){showError(problem);} finally {button.disabled=false;} });
  form.addEventListener('submit',async event=>{ event.preventDefault(); if(!form.reportValidity())return; const data=Object.fromEntries(new FormData(form)); const selected=forms.find(x=>x.id===data.formId); if(!selected){showError({message:'Selecione um formulário válido.'});return;} const save=form.querySelector('[data-save]'); save.disabled=true; try { if(data.id)await SurveysApi.update(data.id,{formId:data.formId,title:data.title,description:data.description,status:'draft'}); else await SurveysApi.create({formId:data.formId,title:data.title,description:data.description,status:'draft'}); dialog.close(); notify('success','Diagnóstico salvo como rascunho.'); await load(); } catch(problem){showError(problem);} finally {save.disabled=false;} });
  host.querySelectorAll('[data-new-survey],[data-action="new-survey"]').forEach(x=>x.addEventListener('click',()=>open()));
  host.querySelector('[data-refresh]').addEventListener('click',load); host.querySelector('[data-search]').addEventListener('input',render); host.querySelector('[data-status]').addEventListener('change',render);
  dialog.querySelectorAll('[data-dialog-close]').forEach(x=>x.addEventListener('click',()=>dialog.close())); load();
})();
