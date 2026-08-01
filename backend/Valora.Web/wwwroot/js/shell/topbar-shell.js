(() => {
  'use strict';
  const palette = document.querySelector('[data-command-palette]');
  const search = document.querySelector('[data-global-search]');
  const results = document.querySelector('[data-command-results]');
  const notifications = document.querySelector('[data-notification-panel]');
  const userMenu = document.querySelector('[data-user-menu]');
  let timer;

  const closePopovers = (except) => [notifications, userMenu].forEach((node) => { if (node && node !== except) node.hidden = true; });
  document.querySelector('[data-action="open-command-palette"]')?.addEventListener('click', () => { palette?.showModal(); search?.focus(); });
  document.querySelector('[data-action="toggle-notifications"]')?.addEventListener('click', (event) => { closePopovers(notifications); notifications.hidden = !notifications.hidden; event.currentTarget.setAttribute('aria-expanded', String(!notifications.hidden)); });
  document.querySelector('[data-action="toggle-user-menu"]')?.addEventListener('click', (event) => { closePopovers(userMenu); userMenu.hidden = !userMenu.hidden; event.currentTarget.setAttribute('aria-expanded', String(!userMenu.hidden)); });
  document.addEventListener('keydown', (event) => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k' || (event.key === '/' && !/input|textarea/i.test(document.activeElement?.tagName))) { event.preventDefault(); palette?.showModal(); search?.focus(); }
    if (event.key === 'Escape') closePopovers();
  });
  document.addEventListener('click', (event) => { if (!event.target.closest('.topbar-actions, .topbar-popover')) closePopovers(); });
  search?.addEventListener('input', () => {
    clearTimeout(timer);
    const query = search.value.trim();
    if (query.length < 2) { results.innerHTML = '<p>Digite ao menos dois caracteres.</p>'; return; }
    results.innerHTML = '<p>Pesquisando…</p>';
    timer = setTimeout(async () => {
      try {
        const response = await fetch(`/bff/search?q=${encodeURIComponent(query)}`, { credentials: 'same-origin' });
        if (!response.ok) throw new Error();
        const data = await response.json();
        const items = data.items ?? data;
        results.replaceChildren(...items.map((item) => { const link = document.createElement('a'); link.href = item.url; link.textContent = `${item.title} — ${item.domain}`; return link; }));
        if (!items.length) results.innerHTML = '<p>Nenhum resultado encontrado neste escopo.</p>';
      } catch { results.innerHTML = '<p>Não foi possível concluir a busca agora.</p>'; }
    }, 280);
  });
})();
