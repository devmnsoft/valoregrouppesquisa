(() => {
  const root = document.querySelector('[data-intelligence-module]'); if (!root) return;
  const loading = root.querySelector('[data-module-loading]'), content = root.querySelector('[data-module-content]');
  const empty = root.querySelector('[data-module-empty]'), error = root.querySelector('[data-module-error]');
  const drawer = root.querySelector('[data-module-drawer]'), drawerContent = root.querySelector('[data-drawer-content]');
  let records = [];
  const escape = value => String(value ?? '').replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]);
  const meaningful = data => Array.isArray(data) ? data.length > 0 : data && (data.latestRun || data.evidence?.total > 0 || Object.keys(data).length > 0);
  const summary = data => {
    const list = Array.isArray(data) ? data : data.latestRun?.insights || data.evidence?.dimensions || data.journey || data.indicators || [];
    records = Array.isArray(list) ? list : [list];
    if (root.dataset.module === 'evidence') {
      const summaryHost = root.querySelector('[data-evidence-summary]');
      const mapped = records.filter(item => item.mappingStatus === 'mapped').length;
      const confidenceValues = records.map(item => Number(item.confidenceWeight)).filter(Number.isFinite);
      const confidence = confidenceValues.length ? confidenceValues.reduce((total, value) => total + value, 0) / confidenceValues.length : 0;
      summaryHost.querySelector('[data-summary-total]').textContent = records.length;
      summaryHost.querySelector('[data-summary-mapped]').textContent = mapped;
      summaryHost.querySelector('[data-summary-pending]').textContent = records.length - mapped;
      summaryHost.querySelector('[data-summary-confidence]').textContent = `${Math.round(confidence * 100)}%`;
      summaryHost.hidden = false;
    }
    const rows = records.slice(0, 100).map((item, position) => {
      const payload = item.data || item;
      const title = item.code || item.conceptCode || payload.title || payload.classification || item.status || 'Registro metodológico';
      const detail = item.evidenceType ? `${item.sourceType} · ${item.capabilityCode}` :
        payload.executiveSummary || payload.limitation || payload.confidence || item.status || 'Registro rastreável';
      const evidence = item.confidenceWeight != null ? `Confiança ${Math.round(Number(item.confidenceWeight) * 100)}%` :
        payload.evidenceCount != null ? `${payload.evidenceCount} evidências` : `Versão ${item.methodologyVersion || item.version || 1}`;
      const mapping = item.mappingStatus === 'pending' ? 'Pendência metodológica' : (item.mappingStatus === 'mapped' ? 'Mapeado' : item.status || item.evidenceType || 'registrado');
      const action = root.dataset.module === 'insights' ? `<a class="valora-button" href="/Intelligence/action?insight=${encodeURIComponent(item.id)}">Transformar em ação</a>` : '';
      return `<article class="valora-card intelligence-module__record" data-record data-survey="${escape(item.surveyId || '')}" data-response="${escape(item.responseId || '')}" data-concept="${escape(item.conceptCode || '')}" data-metric="${escape(item.metricCode || '')}" data-index="${escape(item.indexCode || '')}" data-mapping="${escape(item.mappingStatus || '')}"><div><small>${escape(evidence)}</small><h3>${escape(title)}</h3><p>${escape(detail)}</p></div><div><span class="valora-badge">${escape(mapping)}</span><button class="valora-button valora-button--ghost" type="button" data-detail="${position}">Ver origem</button>${action}</div></article>`;
    }).join('');
    return `<div class="intelligence-module__results"><div class="intelligence-module__count"><strong>${Array.isArray(list) ? list.length : 1}</strong><span> registros autorizados</span></div><div class="intelligence-module__records">${rows}</div><p class="text-muted">Leitura agregada e rastreável. Abra a origem antes de transformar uma hipótese em decisão.</p></div>`;
  };
  root.addEventListener('click', event => {
    const detail = event.target.closest('[data-detail]');
    if (detail) {
      const item = records[Number(detail.dataset.detail)] || {};
      drawerContent.innerHTML = `<p class="eyebrow">Origem rastreável</p><h2>${escape(item.code || item.conceptCode || 'Evidência')}</h2><dl><dt>Diagnóstico</dt><dd>${escape(item.surveyId || 'Não informado')}</dd><dt>Resposta</dt><dd>${escape(item.responseId || 'Não informado')}</dd><dt>Pergunta</dt><dd>${escape(item.questionId || 'Não informada')}</dd><dt>Conceito</dt><dd>${escape(item.conceptCode || 'Pendente')}</dd><dt>Métrica / índice</dt><dd>${escape(item.metricCode || 'Pendente')} · ${escape(item.indexCode || 'Pendente')}</dd><dt>Confiança</dt><dd>${escape(item.confidenceWeight == null ? 'Não informada' : `${Math.round(Number(item.confidenceWeight) * 100)}%`)}</dd></dl><p>Esta leitura preserva a origem e não interpreta respostas qualitativas automaticamente.</p>`;
      drawer.showModal();
    }
    if (event.target.closest('[data-drawer-close]')) drawer.close();
  });
  root.querySelector('[data-evidence-filters]')?.addEventListener('input', () => {
    const survey = root.querySelector('[data-filter-survey]').value.trim().toLowerCase();
    const response = root.querySelector('[data-filter-response]').value.trim().toLowerCase();
    const concept = root.querySelector('[data-filter-concept]').value.trim().toLowerCase();
    const metric = root.querySelector('[data-filter-metric]').value.trim().toLowerCase();
    const index = root.querySelector('[data-filter-index]').value.trim().toLowerCase();
    const mapping = root.querySelector('[data-filter-mapping]').value;
    root.querySelectorAll('[data-record]').forEach(card => card.hidden =
      (survey && !card.dataset.survey.toLowerCase().includes(survey)) ||
      (response && !card.dataset.response.toLowerCase().includes(response)) ||
      (concept && !card.dataset.concept.toLowerCase().includes(concept)) ||
      (metric && !card.dataset.metric.toLowerCase().includes(metric)) ||
      (index && !card.dataset.index.toLowerCase().includes(index)) ||
      (mapping && card.dataset.mapping !== mapping));
  });
  const prefix = ['methodology', 'dictionary', 'cognitive-map'].includes(root.dataset.module) ? 'methodology' : 'intelligence';
  fetch(`/bff/${prefix}/${root.dataset.resource.split('/').map(encodeURIComponent).join('/')}`, { credentials: 'same-origin', headers: { Accept: 'application/json' } })
    .then(async response => { if (!response.ok) { const body = await response.json().catch(() => ({})); throw { status: response.status, message: body.message }; } return response.json(); })
    .then(data => { meaningful(data) ? (content.innerHTML = summary(data), content.hidden = false) : empty.hidden = false; })
    .catch(reason => { error.textContent = reason.status === 403 ? 'Este módulo não está incluído no plano ou perfil atual.' : (reason.message || 'Não foi possível consultar os dados neste momento.'); error.hidden = false; })
    .finally(() => loading.hidden = true);
})();
