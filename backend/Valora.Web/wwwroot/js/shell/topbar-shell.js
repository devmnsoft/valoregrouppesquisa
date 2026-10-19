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
  let organizationRequest = 0;
  let organizationAbort;

  // Keep this catalog deliberately small: every destination is backed by an MVC
  // endpoint. Module-specific results continue to be discovered by the server search.
  const commands = [
    ['Visão executiva', 'Indicadores e prioridades da organização', '/Dashboard'],
    ['Criar novo diagnóstico', 'Configure o próximo ciclo de escuta', '/Diagnostics/New'],
    ['Workspace do diagnóstico', 'Gestão integrada do ciclo ativo', '/Diagnostics'],
    ['Formulários e questionários', 'Biblioteca oficial e versões', '/Forms'],
    ['Criar formulário', 'Estruture novas perguntas e dimensões', '/Forms/Create'],
    ['Pesquisas e coletas', 'Campanhas, públicos e links seguros', '/Surveys'],
    ['Resultados e evidências', 'Devolutivas e leitura executiva', '/Results'],
    ['Relatórios executivos', 'Central de relatórios e exportações', '/Reports'],
    ['Planos de ação', 'Responsáveis, prazos e progresso', '/ActionCenter'],
    ['Criar plano de ação', 'Defina compromissos e entregáveis', '/ActionCenter/Plans/Create'],
    ['Evolução temporal', 'Ciclos sucessivos e baseline', '/Evolution'],
    ['Empresa e estrutura', 'Marca, unidades e departamentos', '/Organization'],
    ['Gestão de equipe', 'Usuários, perfis e permissões', '/Users'],
    ['Segurança e privacidade', 'Conformidade LGPD e auditoria', '/SecurityCompliance'],
    ['Central de ajuda', 'Guias rápidos e atendimento', '/Help'],
    ['Sair da conta', 'Encerrar a sessão com segurança', '/Account/Logout']
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

    // 1. Cabeçalho com Modo de Operação atual e ação de retorno ao Modo Plataforma
    const headerBox = document.createElement('div');
    headerBox.className = 'organization-switcher__header mb-2 pb-2 border-bottom';

    const currentMode = document.createElement('div');
    currentMode.className = 'small fw-semibold text-muted mb-1';
    currentMode.textContent = account.organizationId
      ? `Organização ativa: ${account.organizationName || 'Cliente Selecionado'}`
      : 'Modo Plataforma (Visão Global Ativa)';
    headerBox.append(currentMode);

    if (account.organizationId) {
      const platformBtn = document.createElement('button');
      platformBtn.type = 'button';
      platformBtn.className = 'valora-button valora-button--outline-secondary valora-button--sm w-100 my-1';
      platformBtn.textContent = '🌐 Alternar para Modo Plataforma (Visão Global)';
      platformBtn.addEventListener('click', () => clearOrganization(platformBtn));
      headerBox.append(platformBtn);
    }
    container.append(headerBox);

    // 2. Campo de busca no servidor com debounce
    const searchBox = document.createElement('div');
    searchBox.className = 'organization-switcher__search mb-2';
    const searchInput = document.createElement('input');
    searchInput.type = 'search';
    searchInput.className = 'form-control form-control-sm';
    searchInput.placeholder = 'Buscar organização no servidor…';
    searchInput.setAttribute('aria-label', 'Buscar organização');
    searchBox.append(searchInput);
    container.append(searchBox);

    // 3. Status e lista de resultados
    const statusBox = document.createElement('div');
    statusBox.className = 'small text-muted mb-2';
    statusBox.setAttribute('aria-live', 'polite');
    container.append(statusBox);

    const listContainer = document.createElement('div');
    listContainer.className = 'organization-switcher__list';
    container.append(listContainer);

    // 4. Botão "Carregar mais"
    const moreBtn = document.createElement('button');
    moreBtn.type = 'button';
    moreBtn.className = 'valora-button valora-button--ghost valora-button--sm w-100 mt-2';
    moreBtn.textContent = 'Carregar mais organizações…';
    moreBtn.hidden = true;
    container.append(moreBtn);

    let state = {
      term: '',
      page: 1,
      pageSize: 20,
      hasMore: false,
      items: [],
      isLoading: false
    };

    async function fetchOrganizations(term, page = 1, append = false) {
      state.isLoading = true;
      const request = ++organizationRequest;
      organizationAbort?.abort();
      organizationAbort = new AbortController();

      moreBtn.disabled = true;
      statusBox.textContent = append ? 'Carregando mais organizações…' : 'Buscando organizações…';

      try {
        const params = new URLSearchParams({
          page: String(page),
          pageSize: String(state.pageSize)
        });
        if (term) params.set('search', term);
        if (account.organizationId) params.set('selectedOrganizationId', account.organizationId);

        const response = await fetch(`/bff/auth/organizations?${params}`, {
          credentials: 'same-origin',
          headers: { Accept: 'application/json' },
          signal: organizationAbort.signal
        });

        if (!response.ok) throw new Error(`organizations-${response.status}`);
        const data = await response.json();
        if (request !== organizationRequest) return;

        const rows = Array.isArray(data) ? data : (data.items || []);
        state.hasMore = Boolean(data.hasMore || (data.totalCount && (page * state.pageSize < data.totalCount)));
        state.page = page;
        state.term = term;

        if (append) {
          state.items = [...state.items, ...rows];
        } else {
          state.items = rows;
        }

        renderList(state.items);

        if (!state.items.length) {
          statusBox.textContent = term
            ? `Nenhuma organização encontrada para "${term}".`
            : 'Nenhuma organização disponível para seleção.';
          moreBtn.hidden = true;
        } else {
          const totalInfo = data.totalCount ? ` de ${data.totalCount}` : '';
          statusBox.textContent = `${state.items.length}${totalInfo} organizações disponíveis.`;
          moreBtn.hidden = !state.hasMore;
          moreBtn.disabled = false;
        }
      } catch (err) {
        if (err.name === 'AbortError' || request !== organizationRequest) return;
        statusBox.textContent = 'Não foi possível carregar as organizações.';
        moreBtn.hidden = true;
      } finally {
        state.isLoading = false;
      }
    }

    function renderList(organizations) {
      listContainer.replaceChildren();
      organizations.forEach(org => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'valora-button valora-button--ghost organization-switcher__option w-100 text-start py-2';
        button.textContent = org.name;
        if (org.organizationId && org.organizationId === account.organizationId) {
          button.classList.add('is-current');
          button.setAttribute('aria-current', 'true');
        }
        button.addEventListener('click', () => selectOrganization(org.organizationId, button));
        listContainer.append(button);
      });
    }

    let debounceTimer;
    searchInput.addEventListener('input', () => {
      clearTimeout(debounceTimer);
      debounceTimer = setTimeout(() => {
        fetchOrganizations(searchInput.value.trim(), 1, false);
      }, 300);
    });

    moreBtn.addEventListener('click', () => {
      if (!state.isLoading && state.hasMore) {
        fetchOrganizations(state.term, state.page + 1, true);
      }
    });

    container.hidden = false;
    await fetchOrganizations('', 1, false);
  }
  async function clearOrganization(trigger) {
    trigger.disabled = true;
    try {
      const token = document.querySelector('meta[name="csrf-token"]')?.content || '';
      const response = await fetch('/bff/auth/selected-organization', {
        method: 'DELETE', credentials: 'same-origin',
        headers: { 'X-CSRF-TOKEN': token }
      });
      if (!response.ok) throw new Error(`clear-organization-${response.status}`);
      sessionStorage.setItem('valora.context.switching', String(Date.now()));
      localStorage.removeItem('valora.workspace.cache');
      localStorage.removeItem('valora.dashboard.cache');
      window.location.reload();
    } catch (error) {
      trigger.disabled = false;
      const status = containerWithStatus(trigger);
      status.textContent = 'Não foi possível alternar para o Modo Plataforma.';
      status.hidden = false;
    }
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
      sessionStorage.setItem('valora.context.switching', String(Date.now()));
      localStorage.removeItem('valora.workspace.cache');
      localStorage.removeItem('valora.dashboard.cache');
      document.dispatchEvent(new CustomEvent('valora:organization-changing', { detail: { organizationId } }));
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
