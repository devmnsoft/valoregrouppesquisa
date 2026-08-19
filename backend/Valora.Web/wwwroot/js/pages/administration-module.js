(() => {
  const root = document.querySelector('[data-page="administration-module"]');
  if (!root) return;
  const body = root.querySelector('[data-items]');
  const error = root.querySelector('[data-error]');
  const empty = root.querySelector('[data-empty]');
  const value = (item, ...keys) => keys.map(key => item?.[key]).find(candidate => candidate !== undefined && candidate !== null);
  const encode = text => { const node = document.createElement('span'); node.textContent = String(text ?? '—'); return node.innerHTML; };
  async function load() {
    error.classList.add('d-none');
    body.innerHTML = '<tr><td colspan="4">Carregando…</td></tr>';
    try {
      const response = await fetch(root.dataset.endpoint, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error(response.status === 403 ? 'Seu perfil não possui permissão para acessar este recurso.' : 'Não foi possível carregar os dados administrativos.');
      const payload = await response.json();
      const items = Array.isArray(payload) ? payload : (payload.data ?? payload.details ?? []);
      root.querySelector('[data-total]').textContent = String(items.length);
      empty.classList.toggle('d-none', items.length > 0);
      body.innerHTML = items.length ? items.map(item => `<tr><td>${encode(value(item, 'type', 'module', 'component', 'requestType'))}</td><td>${encode(value(item, 'message', 'title', 'action', 'protocol'))}</td><td><span class="badge text-bg-light">${encode(value(item, 'status', 'severity') ?? (item.readAt ? 'lida' : 'não lida'))}</span></td><td>${encode(value(item, 'createdAt', 'requestedAt'))}</td></tr>`).join('') : '';
    } catch (reason) {
      error.textContent = reason.message;
      error.classList.remove('d-none');
      body.innerHTML = '';
    }
  }
  root.querySelector('[data-action="refresh"]')?.addEventListener('click', load);
  load();
})();
