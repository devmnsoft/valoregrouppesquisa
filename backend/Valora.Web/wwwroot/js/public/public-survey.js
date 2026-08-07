(function(){
  'use strict';
  const vp=window.ValoraPublic; if(!vp) return;
  const page=document.querySelector('[data-page]')?.dataset.page || '';
  const safe=(value,fallback='')=>String(value ?? fallback).replace(/[<>&]/g,c=>({'<':'&lt;','>':'&gt;','&':'&amp;'}[c]));
  const formatDate=window.formatValoraDate || function formatValoraDate(value){ if(!value) return 'Data não informada'; const date=new Date(value); if(Number.isNaN(date.getTime())) return 'Data não informada'; return date.toLocaleDateString('pt-BR'); };
  if(page==='public-survey'){
    const form=document.querySelector('[data-public-survey-form]'),id=document.querySelector('[name=surveyId]')?.value,token=new URLSearchParams(location.search).get('token')||'';
    PublicSurveyApi.validate(id,token).then(survey=>{const questions=survey.questions||survey.items||[];document.querySelector('.empty-state')?.remove();form.innerHTML=`<h2>${safe(survey.title,'Pesquisa Valora Insight™')}</h2><p>${safe(survey.description,'Sua participação apoia decisões melhores.')}</p><div class="public-consent"><h3>Privacidade e consentimento</h3><p>Usaremos as respostas para calcular o diagnóstico. Consulte a política de privacidade antes de continuar.</p><label>Nome (opcional em pesquisa anônima)<input name="name" autocomplete="name"></label><label>E-mail (opcional em pesquisa anônima)<input name="email" type="email" autocomplete="email"></label><label><input type="checkbox" name="anonymous"> Quero responder anonimamente</label><label><input type="checkbox" name="lgpd" required> Li e aceito o termo de consentimento LGPD <strong>versão 8.0</strong>.</label></div>${questions.map((q,i)=>`<fieldset><legend>${i+1}. ${safe(q.text||q.title)}</legend><select name="answer" data-question="${safe(q.id||i+1)}" required><option value="">Selecione</option>${[1,2,3,4,5].map(n=>`<option value="${n}">${n}</option>`).join('')}</select></fieldset>`).join('')}<button class="btn-public primary" type="submit">Enviar respostas com segurança</button>`;vp.bindOnce(form,async data=>{const anonymous=data.get('anonymous')==='on',email=String(data.get('email')||'');if(!anonymous&&!email.includes('@')){vp.toast('Informe um e-mail válido ou selecione resposta anônima.','error');return;}const answers=Object.fromEntries(Array.from(form.querySelectorAll('[data-question]')).map(x=>[x.dataset.question,x.value]));const participant=anonymous?{anonymous:true,consentVersion:'8.0'}:{name:data.get('name'),email,consentVersion:'8.0'};const result=await PublicSurveyApi.submit(id,{token,participant,answers,lgpdConsent:true,communicationConsent:!anonymous});if(result.resultToken)sessionStorage.setItem('valora.publicResultToken.'+result.responseId,result.resultToken);location.assign(`/public/results/${encodeURIComponent(result.responseId)}?token=${encodeURIComponent(result.resultToken||'')}`);});}).catch(()=>{form.innerHTML='<div class="empty-state"><strong>Pesquisa indisponível</strong><p>Confira o link ou fale com nosso suporte.</p></div>';});return;
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
      setText('[data-executive-reading]', 'Resultado em preparação. Tente novamente em instantes ou fale com a Valora Grup.');
    });
  document.querySelector('[data-send-result-email]')?.addEventListener('click',()=>vp.toast('Solicitação de envio registrada. Se o relatório ainda estiver em preparação, tente novamente em instantes ou fale com a Valora Grup.'));
})();
