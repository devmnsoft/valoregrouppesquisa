(function(){
  'use strict';
  const vp=window.ValoraPublic; if(!vp) return;
  const page=document.querySelector('[data-page]')?.dataset.page || '';
  const safe=(value,fallback='')=>String(value ?? fallback).replace(/[<>&]/g,c=>({'<':'&lt;','>':'&gt;','&':'&amp;'}[c]));
  const formatDate=window.formatValoraDate || function formatValoraDate(value){ if(!value) return 'Data não informada'; const date=new Date(value); if(Number.isNaN(date.getTime())) return 'Data não informada'; return date.toLocaleDateString('pt-BR'); };
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
      const insight=result.insight || r?.insight || {};
      setText('[data-method-observation]', safe(insight.observation || result.observation || 'Leitura consolidada das dimensões respondidas.'));
      setText('[data-method-evidence]', safe(insight.evidence || result.evidence || 'Base amostral e convergência em validação.'));
      setText('[data-method-correlation]', safe(insight.correlation || result.correlation || 'Relações consideradas apenas no conjunto observado.'));
      setText('[data-method-cause]', safe(insight.probableCause || result.probableCause || 'Hipótese condicionada à suficiência de evidências.'));
      setText('[data-method-impact]', safe(insight.impact || result.impact || 'Efeito organizacional e criticidade em análise.'));
      setText('[data-method-plan]', safe(insight.evolutionPlan || result.nextLevel || 'Próximo movimento mensurável e verificável.'));
      setText('[data-evidence-warning]', safe(result.warning || 'Validação responsável: conclusões fortes exigem evidências convergentes; caso contrário, o resultado indicará dados insuficientes.'));
    })
    .catch(()=>{
      setText('[data-result-date]', 'Data não informada');
      setText('[data-executive-reading]', 'Resultado em preparação. Tente novamente em instantes ou fale com a Valora Group.');
    });
  document.querySelector('[data-send-result-email]')?.addEventListener('click',()=>vp.toast('Solicitação de envio registrada. Se o relatório ainda estiver em preparação, tente novamente em instantes ou fale com a Valora Group.'));
})();
