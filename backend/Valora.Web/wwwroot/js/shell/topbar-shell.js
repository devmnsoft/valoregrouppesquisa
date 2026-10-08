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

  // Keep this catalog deliberately small: every destination is backed by an MVC
  // endpoint. Module-specific results continue to be discovered by the server search.
  const commands = [
    ['Criar novo diagnóstico', 'Configure o próximo ciclo de escuta', '/Diagnostics/New'],
    ['Criar formulário', 'Abra o estúdio de diagnósticos', '/Forms/Create'],
    ['Visão executiva', 'Indicadores e prioridades', '/Dashboard'],
    ['Diagnósticos', 'Coletas, públicos e campanhas', '/Surveys'],
    ['Formulários', 'Biblioteca e versões', '/Forms'],
    ['Resultados', 'Evidências e devolutivas', '/Results'],
    ['Relatórios', 'Geração e histórico', '/Reports'],
    ['Certificados', 'Emissão e validação', '/Certificates'],
    ['Planos de ação', 'Responsáveis, prazos e progresso', '/ActionCenter'],
    ['Evolução', 'Ciclos e linha do tempo', '/Evolution'],
    ['Administração', 'Usuários, organizações e auditoria', '/Administration'],
    ['Configurações', 'Preferências da plataforma', '/Settings'],
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
      const organizationBanner = document.querySelector('[data-organization-context]');
      if (organizationBanner) organizationBanner.hidden = Boolean(account.organizationName);
      document.dispatchEvent(new CustomEvent('valora:account-context', { detail: { organizationName: account.organizationName || null } }));
      initOrganizationPicker(account);
    } catch (error) {
      document.querySelector('[data-admin-topbar]')?.setAttribute('data-account-context', 'unavailable');
      document.dispatchEvent(new CustomEvent('valora:account-context', { detail: { unavailable: true } }));
    }
  }
  // O seletor de organização só existe para o papel de plataforma (admin_valora), que opera
  // com contexto explícito. Demais perfis permanecem no isolamento do próprio tenant.
  async function initOrganizationPicker(account) {
    const container = document.querySelector('[data-organization-picker]');
    const roles = Array.isArray(account.roleCodes) ? account.roleCodes.map(value => String(value).toLowerCase()) : [];
    if (!container || !roles.includes('admin_valora')) return;
    container.replaceChildren();
    try {
      const response = await fetch('/bff/auth/organizations', { credentials: 'same-origin', headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error(`organizations-${response.status}`);
      const organizations = await response.json();
      if (!Array.isArray(organizations) || !organizations.length) {
        container.append(pickerMessage('Nenhuma organização disponível para seleção.'));
        container.hidden = false;
        return;
      }
      organizations.forEach(organization => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'valora-button valora-button--ghost organization-switcher__option';
        button.textContent = organization.name;
        if (organization.organizationId && organization.organizationId === account.organizationId) {
          button.classList.add('is-current'); button.setAttribute('aria-current', 'true');
        }
        button.addEventListener('click', () => selectOrganization(organization.organizationId, button));
        container.append(button);
      });
      container.hidden = false;
    } catch (error) {
      container.append(pickerMessage('Não foi possível carregar as organizações disponíveis.'));
      container.hidden = false;
    }
  }
  function pickerMessage(text) {
    const message = document.createElement('p'); message.textContent = text; return message;
  }
  async function selectOrganization(organizationId, trigger) {
    trigger.disabled = true;
    try {
      const token = document.querySelector('meta[name="csrf-token"]')?.content || '';
      const response = await fetch('/bff/auth/select-organization', {
        method: 'POST', credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
        body: JSON.stringify({ organizationId })
      });
      if (!response.ok) {
        let problem = null;
        try { problem = await response.json(); } catch { /* mantém mensagem padrão */ }
        throw new Error(problem?.message || `select-organization-${response.status}`);
      }
      window.location.reload();
    } catch (error) {
      trigger.disabled = false;
      const status = containerWithStatus(trigger);
      status.textContent = error instanceof Error && error.message ? error.message : 'Não foi possível alterar a organização selecionada.';
      status.hidden = false;
    }
  }
  function containerWithStatus(trigger) {
    const container = trigger.closest('[data-organization-picker]');
    let status = container?.querySelector('[data-org-picker-status]');
    if (!status) {
      status = document.createElement('p');
      status.setAttribute('data-org-picker-status', '');
      status.hidden = true;
      status.className = 'alert alert-danger m-0';
      container?.append(status);
    }
    return status;
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
