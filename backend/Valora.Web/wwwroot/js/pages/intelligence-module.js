(() => {
  const root = document.querySelector('[data-intelligence-module]'); if (!root) return;
  const loading = root.querySelector('[data-module-loading]'), content = root.querySelector('[data-module-content]');
  const empty = root.querySelector('[data-module-empty]'), error = root.querySelector('[data-module-error]');
  const escape = value => String(value ?? '').replace(/[&<>'"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]);
  const meaningful = data => Array.isArray(data) ? data.length > 0 : data && (data.latestRun || data.evidence?.total > 0 || Object.keys(data).length > 0);
  const summary = data => {
    const list = Array.isArray(data) ? data : data.latestRun?.insights || data.evidence?.dimensions || data.journey || data.indicators || [];
    const rows = (Array.isArray(list) ? list : [list]).slice(0, 12).map(item => {
      const payload = item.data || item;
      const title = item.code || item.conceptCode || payload.title || payload.classification || item.status || 'Registro metodológico';
      const detail = item.evidenceType ? `${item.sourceType} · ${item.capabilityCode}` :
        payload.executiveSummary || payload.limitation || payload.confidence || item.status || 'Registro rastreável';
      const evidence = item.confidenceWeight != null ? `Confiança ${Math.round(Number(item.confidenceWeight) * 100)}%` :
        payload.evidenceCount != null ? `${payload.evidenceCount} evidências` : `Versão ${item.methodologyVersion || item.version || 1}`;
      return `<article class="valora-card intelligence-module__record"><div><small>${escape(evidence)}</small><h3>${escape(title)}</h3><p>${escape(detail)}</p></div><span class="valora-badge">${escape(item.status || item.evidenceType || 'registrado')}</span></article>`;
    }).join('');
    return `<div class="intelligence-module__results"><div class="intelligence-module__count"><strong>${Array.isArray(list) ? list.length : 1}</strong><span> registros autorizados</span></div><div class="intelligence-module__records">${rows}</div><p class="text-muted">Leitura agregada e rastreável. Abra a origem antes de transformar uma hipótese em decisão.</p></div>`;
  };
  const prefix = ['methodology', 'dictionary', 'cognitive-map'].includes(root.dataset.module) ? 'methodology' : 'intelligence';
  fetch(`/bff/${prefix}/${root.dataset.resource.split('/').map(encodeURIComponent).join('/')}`, { credentials: 'same-origin', headers: { Accept: 'application/json' } })
    .then(async response => { if (!response.ok) { const body = await response.json().catch(() => ({})); throw { status: response.status, message: body.message }; } return response.json(); })
    .then(data => { meaningful(data) ? (content.innerHTML = summary(data), content.hidden = false) : empty.hidden = false; })
    .catch(reason => { error.textContent = reason.status === 403 ? 'Este módulo não está incluído no plano ou perfil atual.' : (reason.message || 'Não foi possível consultar os dados neste momento.'); error.hidden = false; })
    .finally(() => loading.hidden = true);
})();
