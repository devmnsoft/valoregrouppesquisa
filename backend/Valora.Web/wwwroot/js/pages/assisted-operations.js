(() => {
  'use strict';
  const root=document.querySelector('[data-operation-module]'); if(!root)return;
  const module=root.dataset.operationModule;
  const configuration={
    support:{endpoint:'support/tickets',action:'Abrir chamado',fields:[['subject','Assunto'],['description','Descrição'],['type','Tipo'],['priority','Prioridade']]},
    feedback:{endpoint:'feedback',action:'Enviar feedback',fields:[['message','Feedback'],['type','Tipo'],['rating','Nota (1 a 5)'],['module','Módulo']]},
    'customer-success':{endpoint:'customer-success/organizations',readonly:true},
    'usage-analytics':{endpoint:'usage-analytics',readonly:true},
    onboarding:{endpoint:'onboarding',readonly:true},
    'upgrade-requests':{endpoint:'upgrade-requests',action:'Solicitar upgrade',fields:[['type','Tipo da solicitação'],['currentPlan','Plano atual'],['requestedResource','Recurso solicitado'],['notes','Contexto']]},
    incidents:{endpoint:'incidents',action:'Criar incidente',fields:[['title','Título'],['description','Descrição'],['severity','Severidade']]},
    'release-notes':{endpoint:'release-notes',action:'Criar release note',fields:[['version','Versão'],['title','Título'],['content','Conteúdo'],['type','Tipo'],['visibility','Visibilidade']]},
    'data-quality':{endpoint:'data-quality',action:'Rodar verificação',run:true}
  }[module];
  const action=root.querySelector('[data-primary-action]'),drawer=root.querySelector('[data-drawer]'),rows=root.querySelector('[data-rows]'),head=root.querySelector('[data-head]'),empty=root.querySelector('[data-empty]'),error=root.querySelector('[data-error]'); let data=[];
  action.textContent=configuration.action||'Atualizar visão'; if(configuration.readonly)action.textContent='Atualizar visão';
  const csrf=()=>document.querySelector('meta[name="csrf-token"]')?.content||'';
  const request=async(path,options={})=>{const response=await fetch(`/bff/${path}`,{...options,headers:{'Content-Type':'application/json','X-CSRF-TOKEN':csrf(),...(options.headers||{})}});const payload=await response.json().catch(()=>({}));if(!response.ok)throw new Error(payload.message||'Não foi possível concluir a operação.');return payload;};
  const label=value=>value.replace(/_/g,' ').replace(/\b\w/g,c=>c.toUpperCase());
  function render(items){data=Array.isArray(items)?items:[];root.querySelector('[data-metric="total"]').textContent=data.length;root.querySelector('[data-metric="attention"]').textContent=data.filter(x=>['critical','high','risk','blocked','open'].includes(x.priority||x.severity||x.health_status||x.status)).length;empty.classList.toggle('d-none',data.length>0);if(!data.length){head.innerHTML='';rows.innerHTML='';return;}const keys=Object.keys(data[0]).filter(x=>!['metadata_json','description','content','deleted_at'].includes(x)).slice(0,6);head.innerHTML=`<tr>${keys.map(k=>`<th>${label(k)}</th>`).join('')}</tr>`;rows.innerHTML=data.map(item=>`<tr>${keys.map(k=>`<td>${k.includes('status')||k==='priority'||k==='severity'?`<span class="status-badge status-${item[k]||'neutral'}">${label(String(item[k]||'—'))}</span>`:String(item[k]??'—')}</td>`).join('')}</tr>`).join('');}
  async function load(){error.classList.add('d-none');rows.innerHTML='<tr><td><span class="skeleton">Carregando dados operacionais…</span></td></tr>';try{render(await request(configuration.endpoint));}catch(e){error.textContent=e.message;error.classList.remove('d-none');rows.innerHTML='';}}
  function open(){if(configuration.readonly)return load();if(configuration.run)return run();root.querySelector('[data-drawer-title]').textContent=configuration.action;root.querySelector('[data-form-fields]').innerHTML=configuration.fields.map(([name,text])=>`<label class="form-field"><span>${text}</span>${['description','message','content','notes'].includes(name)?`<textarea name="${name}" required></textarea>`:`<input name="${name}" required>`}</label>`).join('');drawer.showModal();}
  async function save(){const form=new FormData(drawer.querySelector('form')),payload=Object.fromEntries(form);try{await request(configuration.endpoint,{method:'POST',body:JSON.stringify(payload)});drawer.close();await load();}catch(e){error.textContent=e.message;error.classList.remove('d-none');}}
  async function run(){try{await request('data-quality/run',{method:'POST',body:'{}'});await load();}catch(e){error.textContent=e.message;error.classList.remove('d-none');}}
  root.querySelector('[data-filter]').addEventListener('input',event=>{const q=event.target.value.toLowerCase();render(data.filter(item=>JSON.stringify(item).toLowerCase().includes(q)));});action.addEventListener('click',open);root.querySelector('[data-save]').addEventListener('click',save);load().then(()=>{const query=new URLSearchParams(location.search);if(query.has('new'))open();if(query.has('run')&&configuration.run)run();});
})();
