(() => {
  const root = document.querySelector('[data-intelligence-module]'); if (!root) return;
  const loading = root.querySelector('[data-module-loading]'), content = root.querySelector('[data-module-content]');
  const empty = root.querySelector('[data-module-empty]'), error = root.querySelector('[data-module-error]');
  const meaningful = data => Array.isArray(data) ? data.length > 0 : data && (data.latestRun || data.evidence?.total > 0 || Object.keys(data).length > 0);
  const summary = data => {
    const list = Array.isArray(data) ? data : data.latestRun?.insights || data.evidence?.dimensions || data.journey || data.indicators || [];
    return `<div class="intelligence-module__results"><strong>${Array.isArray(list) ? list.length : 1}</strong><span>registros agregados disponíveis</span><p>Os detalhes são apresentados na Central de Inteligência para preservar contexto, confiança e rastreabilidade.</p></div>`;
  };
  const prefix = ['methodology', 'dictionary', 'cognitive-map'].includes(root.dataset.module) ? 'methodology' : 'intelligence';
  fetch(`/bff/${prefix}/${root.dataset.resource.split('/').map(encodeURIComponent).join('/')}`, { credentials: 'same-origin', headers: { Accept: 'application/json' } })
    .then(async response => { if (!response.ok) { const body = await response.json().catch(() => ({})); throw { status: response.status, message: body.message }; } return response.json(); })
    .then(data => { meaningful(data) ? (content.innerHTML = summary(data), content.hidden = false) : empty.hidden = false; })
    .catch(reason => { error.textContent = reason.status === 403 ? 'Este módulo não está incluído no plano ou perfil atual.' : (reason.message || 'Não foi possível consultar os dados neste momento.'); error.hidden = false; })
    .finally(() => loading.hidden = true);
})();
