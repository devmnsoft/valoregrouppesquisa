(() => {
  'use strict';
  const root = document.querySelector('[data-page="administration-master"]');
  if (!root) return;
  const endpoint = root.dataset.endpoint;
  const render = payload => {
    const source = payload?.summary ?? payload?.data ?? payload ?? {};
    root.querySelectorAll('[data-metric]').forEach(node => {
      const value = Number(source[node.dataset.metric] ?? 0);
      node.textContent = new Intl.NumberFormat('pt-BR').format(Number.isFinite(value) ? value : 0);
    });
    const updated = root.querySelector('[data-updated]');
    if (updated) updated.textContent = `Atualizado às ${new Date().toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}`;
  };
  const load = async () => {
    try {
      const response = await fetch(endpoint, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
      render(response.ok ? await response.json() : {});
    } catch { render({}); }
  };
  root.addEventListener('click', event => { if (event.target.closest('[data-action="refresh"]')) load(); });
  load();
})();
