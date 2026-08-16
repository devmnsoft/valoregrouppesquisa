(() => {
  'use strict';
  const palette = document.querySelector('[data-command-palette]');
  const search = document.querySelector('[data-global-search]');
  const results = document.querySelector('[data-command-results]');
  const notifications = document.querySelector('[data-notification-panel]');
  const userMenu = document.querySelector('[data-user-menu]');
  const notificationToggle = document.querySelector('[data-action="toggle-notifications"]');
  const userToggle = document.querySelector('[data-action="toggle-user-menu"]');
  let activeResult = 0;
  let timer;

  const commands = [
    ['Criar Novo Diagnóstico', 'Assistente do próximo ciclo de escuta', '/Diagnostics/New'], ['Criar formulário', 'Abrir o estúdio de diagnósticos', '/Forms'], ['Dashboard', 'Visão executiva', '/Dashboard'],
    ['Diagnósticos', 'Pesquisas e campanhas', '/Surveys'], ['Formulários', 'Estúdio de diagnósticos', '/Forms'],
    ['Resultados', 'Respostas e devolutivas', '/Responses'], ['Certificados', 'Emissão e validação', '/Certificates/Validate'],
    ['Usuários', 'Pessoas, papéis e acessos', '/Users'], ['Organização', 'Estrutura e identidade', '/Organization'],
    ['Abrir Estrutura Organizacional', 'Unidades, áreas e lideranças', '/Organization/Structure'], ['Ver Templates Oficiais', 'Biblioteca metodológica Valora', '/Experience/Templates'],
    ['Abrir Planos e Uso', 'Plano atual e limites', '/Plans'], ['Abrir Governança', 'Eventos executivos da plataforma', '/Intelligence/PlatformGovernance'], ['Auditoria', 'Eventos e rastreabilidade', '/Audit'],
    ['Processar Inteligência', 'Centro de processamento organizacional', '/Intelligence/Processing'], ['Gerar Executive Report', 'Relatório executivo rastreável', '/Intelligence/ExecutiveReport'],
    ['Inteligência', 'Evidências e recomendações', '/Intelligence'], ['Plano de ação', 'Compromissos e evolução', '/ActionPlans'],
    ['Relatórios executivos', 'Preview e exportações seguras', '/Reports'], ['Configurações', 'Preferências e segurança', '/Settings'],
    ['Sair', 'Encerrar a sessão com segurança', '/Account/Logout']
  ];
  const normalize = value => String(value || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  const setText = (selector, value) => document.querySelectorAll(selector).forEach(node => { node.textContent = value || 'Não informado'; });
  const closePopovers = except => {
    [[notifications, notificationToggle], [userMenu, userToggle]].forEach(([node, trigger]) => {
      if (node && node !== except) { node.hidden = true; trigger?.setAttribute('aria-expanded', 'false'); }
    });
  };
  const openPalette = () => {
    closePopovers();
    if (!palette?.open) palette?.showModal();
    search?.focus();
    renderCommands(search?.value || '');
  };
  const renderCommands = query => {
    if (!results) return;
    const term = normalize(query);
    const matches = commands.filter(item => !term || normalize(item.join(' ')).includes(term));
    activeResult = 0;
    if (!matches.length) { results.innerHTML = '<div class="valora-empty-state"><strong>Nenhum comando encontrado</strong><span>Tente buscar por módulo ou ação.</span></div>'; return; }
    results.replaceChildren(...matches.map((item, index) => {
      const link = document.createElement('a');
      link.className = `valora-command-result${index === 0 ? ' is-active' : ''}`;
      link.href = item[2]; link.dataset.commandResult = '';
      const title = document.createElement('strong'); title.textContent = item[0];
      const detail = document.createElement('small'); detail.textContent = item[1];
      link.append(title, detail); return link;
    }));
  };
  const moveResult = delta => {
    const items = [...results.querySelectorAll('[data-command-result]')];
    if (!items.length) return;
    items[activeResult]?.classList.remove('is-active');
    activeResult = (activeResult + delta + items.length) % items.length;
    items[activeResult].classList.add('is-active'); items[activeResult].scrollIntoView({ block: 'nearest' });
  };

  async function loadAccountContext() {
    try {
      const response = await fetch('/bff/account/context', { credentials: 'same-origin', headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error(`account-context-${response.status}`);
      const account = await response.json();
      setText('[data-user-name]', account.userName); setText('[data-user-profile]', account.primaryRole);
      setText('[data-user-email]', account.userEmail); setText('[data-user-initials]', account.userInitials);
      setText('[data-current-organization]', account.organizationName || 'Organização não vinculada');
      setText('[data-current-plan]', account.planName || 'Plano não informado');
    } catch (error) {
      document.querySelector('[data-admin-topbar]')?.setAttribute('data-account-context', 'unavailable');
    }
  }
  function loadNotifications() {
    const list = notifications?.querySelector('[data-notification-list]');
    const saved = JSON.parse(localStorage.getItem('valora.notifications') || '[]');
    if (!list || !Array.isArray(saved) || !saved.length) return;
    list.replaceChildren(...saved.map((item, index) => {
      const button = document.createElement('button'); button.type = 'button'; button.className = 'notification-item';
      button.innerHTML = `<strong></strong><small></small>`; button.querySelector('strong').textContent = item.title || 'Atualização';
      button.querySelector('small').textContent = item.message || ''; button.addEventListener('click', () => { saved[index].read = true; localStorage.setItem('valora.notifications', JSON.stringify(saved)); loadNotifications(); }); return button;
    }));
    const unread = saved.filter(item => !item.read).length; const badge = document.querySelector('[data-notification-count]');
    if (badge) { badge.textContent = String(unread); badge.hidden = unread === 0; }
  }

  document.querySelector('[data-action="open-command-palette"]')?.addEventListener('click', openPalette);
  notificationToggle?.addEventListener('click', event => { closePopovers(notifications); notifications.hidden = !notifications.hidden; event.currentTarget.setAttribute('aria-expanded', String(!notifications.hidden)); });
  userToggle?.addEventListener('click', event => { closePopovers(userMenu); userMenu.hidden = !userMenu.hidden; event.currentTarget.setAttribute('aria-expanded', String(!userMenu.hidden)); });
  document.querySelector('[data-action="read-all"]')?.addEventListener('click', () => { const saved = JSON.parse(localStorage.getItem('valora.notifications') || '[]'); saved.forEach(item => { item.read = true; }); localStorage.setItem('valora.notifications', JSON.stringify(saved)); loadNotifications(); });
  document.getElementById('logoutButton')?.addEventListener('click', () => { window.location.assign('/Account/Logout'); });
  document.addEventListener('keydown', event => {
    if (((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') || (event.key === '/' && !/input|textarea|select/i.test(document.activeElement?.tagName))) { event.preventDefault(); openPalette(); }
    if (palette?.open && event.key === 'ArrowDown') { event.preventDefault(); moveResult(1); }
    if (palette?.open && event.key === 'ArrowUp') { event.preventDefault(); moveResult(-1); }
    if (palette?.open && event.key === 'Enter' && document.activeElement === search) { event.preventDefault(); results.querySelectorAll('[data-command-result]')[activeResult]?.click(); }
    if (event.key === 'Escape') closePopovers();
  });
  document.addEventListener('click', event => { if (!event.target.closest('.topbar-actions, .topbar-popover')) closePopovers(); });
  search?.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(() => renderCommands(search.value), 80); });
  loadAccountContext(); loadNotifications(); renderCommands('');
})();
