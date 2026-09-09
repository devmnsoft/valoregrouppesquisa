function safe(v,f){return v===undefined||v===null||Number.isNaN(v)?(f||'—'):String(v)}
function safeHtml(v,f){return safe(v,f).replace(/[&<>'"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]))}
function err(e){const m=formatFriendlyError(e); $('[data-page="result-page"] .valora-error-state').removeClass('d-none').text(m); Toast.error(m)}
function downloadBlob(blob,name){const a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download=name;document.body.appendChild(a);a.click();setTimeout(()=>{URL.revokeObjectURL(a.href);a.remove();},500);}
function whatsappUrl(data){const msg='Olá, quero falar com a Valora Group sobre o Diagnóstico Valora Insight™.'; return `https://wa.me/5591992545353?text=${encodeURIComponent(msg)}`;}
$(async function(){
 const page=$('[data-page="result-page"]'); if(!page.length)return; const responseId=$('input[name=responseId]').val()||location.pathname.split('/').pop();
 async function load(){try{Loading.show('Preparando seu resultado...'); const data=await AjaxClient.get(`/bff/responses/${encodeURIComponent(responseId)}/result`); const dims=data.dimensions||[]; $('[data-results]').html(`<div class='col-md-4'><div class='card'><div class='card-body'><h2>Resultado do Diagnóstico</h2><p>${safeHtml(data.survey?.title)}</p><p>Data: ${safeHtml((window.formatValoraDate||((v)=>v||'Data não informada'))(data.response?.completedAt))}</p><h3>${safeHtml(data.result?.percentage,'Dados insuficientes')}</h3><p>Nível: ${safeHtml(data.result?.maturityLabel,'Dados insuficientes')}</p></div></div></div><div class='col-md-8'><div class='card'><div class='card-body'><h2>Recomendação</h2><p>${safeHtml(data.result?.radarText,'A recomendação depende de evidências calculadas e revisão humana.')}</p><div class='row g-2'>${dims.map(d=>`<div class='col-md-6'><div class='border rounded p-3'><strong>${safeHtml(d.dimensionName)}</strong><br>${safeHtml(d.percentage,'Dados insuficientes')}</div></div>`).join('')}</div><hr><a href='/' class='btn btn-link'>Voltar para início</a></div></div></div>`); page.find('.valora-empty-state').addClass('d-none');}
 catch(e){err(e)}finally{Loading.hide();}}
 load();
});
