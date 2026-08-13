(function(){
  'use strict';
  const vp=window.ValoraPublic; if(!vp) return;
  const page=document.querySelector('[data-page]')?.dataset.page || '';
  const safe=(value,fallback='')=>String(value ?? fallback).replace(/[<>&]/g,c=>({'<':'&lt;','>':'&gt;','&':'&amp;'}[c]));
  const formatDate=window.formatValoraDate || function formatValoraDate(value){ if(!value) return 'Data não informada'; const date=new Date(value); if(Number.isNaN(date.getTime())) return 'Data não informada'; return date.toLocaleDateString('pt-BR'); };
  if(page==='public-survey'){
    const form=document.querySelector('[data-public-survey-form]'),id=document.querySelector('[name=surveyId]')?.value,token=new URLSearchParams(location.search).get('token')||'';
    PublicSurveyApi.validate(id,token).then(payload=>{const survey=payload.survey||payload;const questions=payload.form?.questions||[];if(!questions.length)throw new Error('FORM_WITHOUT_QUESTIONS');const input=q=>{const type=String(q.type||'').toLowerCase();if(['short_text','text_short'].includes(type))return `<input data-question="${safe(q.id)}" ${q.required?'required':''}>`;if(['long_text','text_long'].includes(type))return `<textarea data-question="${safe(q.id)}" ${q.required?'required':''}></textarea>`;const options=(q.options||[]).length?q.options:[1,2,3,4,5].map(n=>({id:n,text:String(n),score:n}));return `<select data-question="${safe(q.id)}" ${q.required?'required':''}><option value="">Selecione</option>${options.map(option=>`<option value="${safe(option.id||option.value||option.score)}">${safe(option.text||option.label||option.value)}</option>`).join('')}</select>`;};document.querySelector('.empty-state')?.remove();form.innerHTML=`<div class="public-progress" aria-label="Progresso"><span style="width:0" data-progress></span></div><h2>${safe(survey.title,'Pesquisa Valora Insight™')}</h2><p>${safe(survey.description,'Sua participação apoia decisões melhores.')}</p><div class="public-consent"><h3>Privacidade e consentimento</h3><p>Usaremos as respostas para calcular o diagnóstico. Consulte a política de privacidade antes de continuar.</p><label>Nome (opcional em pesquisa anônima)<input name="name" autocomplete="name"></label><label>E-mail (opcional em pesquisa anônima)<input name="email" type="email" autocomplete="email"></label><label><input type="checkbox" name="anonymous"> Quero responder anonimamente</label><label><input type="checkbox" name="lgpd" required> Li e aceito o termo de consentimento LGPD <strong>versão 8.0</strong>.</label></div>${questions.map((q,i)=>`<fieldset><legend>${i+1}. ${safe(q.text||q.title)}${q.required?' *':''}</legend>${input(q)}</fieldset>`).join('')}<p class="public-feedback" role="status" aria-live="polite" data-submit-feedback></p><button class="btn-public primary" type="submit">Enviar respostas com segurança</button>`;const fields=Array.from(form.querySelectorAll('[data-question]'));const updateProgress=()=>{const done=fields.filter(x=>x.value).length;form.querySelector('[data-progress]').style.width=`${Math.round(done/fields.length*100)}%`;};form.addEventListener('input',updateProgress);vp.bindOnce(form,async data=>{const anonymous=data.get('anonymous')==='on',email=String(data.get('email')||'');if(!anonymous&&!email.includes('@')){vp.toast('Informe um e-mail válido ou selecione resposta anônima.','error');return;}const answers=Object.fromEntries(fields.map(x=>[x.dataset.question,x.value]));const participant=anonymous?{anonymous:true,consentVersion:'8.0'}:{name:data.get('name'),email,consentVersion:'8.0'};form.querySelector('[data-submit-feedback]').textContent='Salvando sua resposta com segurança…';const result=await PublicSurveyApi.submit(id,{token,participant,answers,lgpdConsent:true,communicationConsent:!anonymous});if(!result.responseId||!result.resultToken)throw new Error('RESPONSE_NOT_CONFIRMED');sessionStorage.setItem('valora.publicResultToken.'+result.responseId,result.resultToken);location.assign(`/public/results/${encodeURIComponent(result.responseId)}?token=${encodeURIComponent(result.resultToken)}`);});}).catch(error=>{const incomplete=error?.message==='FORM_WITHOUT_QUESTIONS';form.innerHTML=`<div class="empty-state" role="alert"><strong>${incomplete?'Diagnóstico em preparação':'Pesquisa indisponível'}</strong><p>${incomplete?'O formulário ainda não possui perguntas publicadas. Tente novamente mais tarde.':'Confira se o link está completo e se o período de respostas continua aberto.'}</p></div>`;});return;
  }
  if(page!=='public-result') return;
  const id=document.querySelector('[name=responseId]')?.value;
  const token=new URLSearchParams(location.search).get('token') || sessionStorage.getItem('valora.publicResultToken.'+id) || '';
  const setText=(selector,value)=>{ const el=document.querySelector(selector); if(el) el.textContent=value; };
  vp.api('/api/public/results/'+encodeURIComponent(id)+(token?'?token='+encodeURIComponent(token):''))
    .then(r=>{
      const response=r?.response || r || {};
      const result=r?.result || r || {};
      const dims=r?.dimensions || [];
      setText('[data-result-date]', formatDate(response.completedAt || response.createdAt || result.completedAt));
      setText('[data-result-score]', safe(result.percentage ?? result.score ?? r?.score ?? '--'));
      setText('[data-result-level]', safe(result.maturityLabel || result.level || 'Nível de maturidade em processamento'));
      setText('[data-executive-reading]', safe(result.executiveSummary || result.reading || 'Leitura executiva direta da maturidade organizacional, sem adoçamento e sem repetição visual.'));
      setText('[data-radar-text]', safe(result.radarText || 'Radar visual textual carregado com fallback seguro.'));
      if(dims.length){
        setText('[data-dimensions-text]', dims.map(d=>`${d.dimensionName || d.name}: ${d.percentage ?? d.score ?? 'em análise'}`).join(' • '));
      }
    })
    .catch(()=>{
      setText('[data-result-date]', 'Data não informada');
      setText('[data-executive-reading]', 'Resultado em preparação. Tente novamente em instantes ou fale com a Valora Group.');
    });
  document.querySelector('[data-send-result-email]')?.addEventListener('click',()=>vp.toast('Solicitação de envio registrada. Se o relatório ainda estiver em preparação, tente novamente em instantes ou fale com a Valora Group.'));
})();
