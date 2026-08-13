(() => {
  const root = document.querySelector('[data-intelligence-workspace]'); if (!root) return;
  const recordsHost = root.querySelector('[data-records]'), loading = root.querySelector('[data-loading]');
  const empty = root.querySelector('[data-empty]'), error = root.querySelector('[data-error]'), drawer = root.querySelector('[data-drawer]');
  const fields = root.dataset.fields.split(',').filter(Boolean); let records = [];
  const esc = value => String(value ?? '').replace(/[&<>'"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]));
  const value = (item, key) => item?.[key] ?? item?.data?.[key];
  const list = data => Array.isArray(data) ? data : data?.latestRun?.insights || data?.evidence?.dimensions || data?.journey || data?.indicators || (data ? [data] : []);
  const render = () => {
    const search = root.querySelector('[data-search]').value.toLowerCase();
    const filtered = records.filter(item => (!search || JSON.stringify(item).toLowerCase().includes(search)) && [...root.querySelectorAll('[data-filter]')].every(select => !select.value || String(value(item, select.dataset.filter) ?? '').toLowerCase() === select.value));
    recordsHost.innerHTML = filtered.map((item, index) => `<article class="valora-card intelligence-module__record"><div><small>${esc(value(item,'status') || value(item,'mappingStatus') || 'rastreável')}</small><h3>${esc(value(item,'name') || value(item,'title') || value(item,'code') || value(item,'classification') || `${root.dataset.label} ${index + 1}`)}</h3><p>${esc(value(item,'description') || value(item,'interpretation') || value(item,'limitation') || 'Abra o detalhe para consultar contexto e origem.')}</p></div><button class="valora-button valora-button--ghost" type="button" data-detail-index="${records.indexOf(item)}">Ver detalhe e origem</button></article>`).join('');
    recordsHost.hidden = !filtered.length; empty.hidden = filtered.length > 0;
  };
  const summarize = () => {
    root.querySelector('[data-kpi-total]').textContent = records.length;
    root.querySelector('[data-kpi-evidence]').textContent = records.filter(x => Number(value(x,'evidenceCount') ?? value(x,'confidenceWeight')) > 0 || value(x,'mappingStatus') === 'mapped').length;
    root.querySelector('[data-kpi-pending]').textContent = records.filter(x => ['pending','insufficient_evidence','outdated'].includes(value(x,'status') || value(x,'mappingStatus'))).length;
    const confidence = records.map(x => Number(value(x,'confidence') ?? value(x,'confidenceWeight'))).filter(Number.isFinite);
    root.querySelector('[data-kpi-confidence]').textContent = confidence.length ? `${Math.round(confidence.reduce((a,b)=>a+b,0) / confidence.length * (Math.max(...confidence) <= 1 ? 100 : 1))}%` : '—';
    root.querySelectorAll('[data-filter]').forEach(select => [...new Set(records.map(x => value(x, select.dataset.filter)).filter(Boolean))].sort().forEach(v => select.add(new Option(v, String(v).toLowerCase()))));
  };
  root.addEventListener('input', event => { if (event.target.matches('[data-search],[data-filter]')) render(); });
  root.addEventListener('click', event => {
    const button = event.target.closest('[data-detail-index]');
    if (button) { const item = records[Number(button.dataset.detailIndex)]; root.querySelector('[data-detail]').innerHTML = `<p class="eyebrow">Contexto rastreável</p><h2>${esc(value(item,'title') || value(item,'name') || value(item,'code') || root.dataset.label)}</h2><dl>${fields.map(field => `<dt>${esc(field)}</dt><dd>${esc(typeof value(item,field) === 'object' ? JSON.stringify(value(item,field)) : value(item,field) ?? 'Não informado')}</dd>`).join('')}</dl><p>Dados insuficientes ou limitações permanecem explícitos; hipóteses não são apresentadas como certeza.</p>`; drawer.showModal(); }
    if (event.target.closest('[data-close]')) drawer.close();
  });
  const request = (method = 'GET') => fetch(`/bff/intelligence/${root.dataset.endpoint}${method === 'POST' ? '/preview' : ''}`, { method, credentials: 'same-origin', headers: {'Accept':'application/json','Content-Type':'application/json','RequestVerificationToken': document.querySelector('meta[name="csrf-token"]')?.content || ''}, body: method === 'POST' ? '{}' : undefined });
  const load = response => response.ok ? response.json() : response.json().catch(()=>({})).then(body => Promise.reject({status:response.status,message:body.message,correlationId:body.correlationId}));
  const show = data => { records = list(data); summarize(); render(); };
  request().then(load).then(show).catch(reason => { error.textContent = reason.status === 403 ? 'Este módulo não está disponível para seu plano ou perfil.' : `${reason.message || 'Não foi possível consultar os dados.'}${reason.correlationId ? ` Referência: ${reason.correlationId}` : ''}`; error.hidden = false; }).finally(() => loading.hidden = true);
  root.querySelector('[data-preview]')?.addEventListener('click', event => { event.currentTarget.disabled = true; request('POST').then(load).then(data => { show(data); }).catch(reason => { error.textContent = reason.message || 'Não foi possível gerar o preview.'; error.hidden = false; }).finally(() => event.currentTarget.disabled = false); });
})();
