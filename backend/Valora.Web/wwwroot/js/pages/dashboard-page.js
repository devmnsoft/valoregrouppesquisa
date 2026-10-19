(() => {
  const root = document.querySelector('[data-page="dashboard-page"]');
  if (!root) return;

  const unwrap = value => value?.data ?? value ?? {};
  const arrayOf = value => {
    const data = unwrap(value);
    return Array.isArray(data) ? data : Array.isArray(data.items) ? data.items : [];
  };
  const setText = (selector, value) => {
    const element = root.querySelector(selector);
    if (element) element.textContent = value;
  };
  const number = value => value === null || value === undefined ? '—' : new Intl.NumberFormat('pt-BR').format(Number(value));
  const unavailable = selector => { const node = root.querySelector(selector); if (node) { node.textContent = '—'; node.title = 'Este indicador será exibido após o primeiro ciclo de respostas.'; } };

  function renderKpis({ surveys, responses, usage, intelligence, actions }) {
    const activeSurveys = surveys.filter(item => ['active', 'scheduled'].includes(String(item.status).toLowerCase())).length;
    const completed = responses.filter(item => ['completed', 'submitted'].includes(String(item.status).toLowerCase())).length;
    const completionRate = responses.length ? Math.round((completed / responses.length) * 100) : 0;
    setText('[data-kpi="responses"]', responses.length ? number(responses.length) : '—');
    setText('[data-kpi="activeSurveys"]', number(activeSurveys));
    setText('[data-kpi="completionRate"]', responses.length ? `${completionRate}%` : '—');
    const run = intelligence.latestRun;
    setText('[data-kpi="averageLevel"]', run?.maturityIndex == null ? 'Não avaliado' : `${number(run.maturityIndex)} pontos`);
    setText('[data-kpi="evidence"]', intelligence.evidence?.total == null ? '—' : number(intelligence.evidence.total));
    setText('[data-kpi="actions"]', number(actions.filter(item => !['completed', 'cancelled', 'reviewed'].includes(String(item.status).toLowerCase())).length));
    root.querySelector('[data-metric-empty]').hidden = responses.length > 0;
    renderJourney(surveys, responses, usage, actions);
    renderAttention(surveys, responses, completionRate);
    renderStrategicReading(intelligence, actions);
  }

  function renderStrategicReading(intelligence, actions) {
    const panel = root.querySelector('[data-strategic-reading]');
    if (!panel) return;
    const run = intelligence.latestRun;
    const enoughEvidence = Boolean(run && run.maturityIndex != null && !run.warning && Number(run.evidenceCount) >= 3);
    panel.hidden = !enoughEvidence;
    root.querySelector('[data-score-panel]').hidden = !enoughEvidence;
    root.querySelector('[data-empty-score]').hidden = enoughEvidence;
    root.querySelector('[data-summary-cards]').hidden = !enoughEvidence;
    if (!enoughEvidence) return;
    const score = Number(run.maturityIndex);
    const level = run.maturityClassification || 'Não avaliado';
    setText('[data-score]', `${number(score)} pontos`);
    setText('[data-level]', level);
    setText('[data-maturity-score]', number(score));
    setText('[data-maturity-level]', level);
    setText('[data-maturity-explanation]', `${number(run.evidenceCount)} evidências sustentam esta leitura. Hipóteses devem ser validadas com as lideranças.`);
    setText('[data-strongest]', run.strongestDimension || 'Não determinada');
    setText('[data-weakest]', run.weakestDimension || 'Não determinada');
    const openCritical = actions.find(item => item.priority === 'critical' && !['completed', 'cancelled'].includes(item.status));
    setText('[data-blocker]', openCritical?.title || `Validar a dimensão ${run.weakestDimension || 'mais frágil'} com os responsáveis.`);
    setText('[data-next-step]', openCritical ? 'Definir responsável e prazo para a ação crítica.' : `Criar uma ação mensurável para ${run.weakestDimension || 'a principal fragilidade'}.`);
  }

  function renderJourney(surveys, responses, usage, actions = []) {
    const host = root.querySelector('[data-onboarding]');
    const restoreBtn = root.querySelector('[data-restore-onboarding]');
    if (!host) return;
    const isHidden = localStorage.getItem('valora.onboarding.hidden') === 'true';
    if (isHidden) {
      host.hidden = true;
      if (restoreBtn) restoreBtn.hidden = false;
      return;
    }
    if (restoreBtn) restoreBtn.hidden = true;
    const steps = [
      ['Configurar empresa', true, '/Organization', 'Revise marca e dados institucionais.'],
      ['Cadastrar unidades e setores', Boolean(usage.units || usage.departments), '/Organization#org-structure', 'Estruture análises com escopo seguro.'],
      ['Convidar equipe', Number(usage.activeUsers || 0) > 1, '/Users', 'Distribua responsabilidades.'],
      ['Escolher template', surveys.length > 0, '/Experience/Templates', 'Comece com um modelo oficial.'],
      ['Publicar pesquisa', surveys.some(x => ['active','published'].includes(String(x.status).toLowerCase())), '/Forms', 'Revise e publique a versão.'],
      ['Acompanhar respostas', responses.length > 0, '/Responses', 'Monitore a adesão do ciclo.'],
      ['Gerar relatório', responses.length > 0, '/Reports', 'Compartilhe a leitura executiva.'],
      ['Criar plano de ação', (actions?.length || 0) > 0, '/ActionCenter/Plans/Create', 'Transforme evidências em compromissos com responsáveis e prazos.']
    ];
    const progress = Math.round(steps.filter(x => x[1]).length / steps.length * 100);
    host.hidden = false;
    root.querySelector('[data-onboarding-progress]').textContent = `${progress}% concluído`; root.querySelector('[data-onboarding-bar]').style.width = `${progress}%`;
    root.querySelector('[data-onboarding-items]').innerHTML = steps.map(step => `<a class="${step[1] ? 'is-complete' : ''}" href="${step[2]}"><span>${step[1] ? '✓' : '○'}</span><div><strong>${step[0]}</strong><small>${step[3]}</small></div></a>`).join('');
  }

  function renderAttention(surveys, responses, completionRate) {
    const host = root.querySelector('[data-dashboard-attention]'); if (!host) return; const items = [];
    if (!surveys.some(x => String(x.status).toLowerCase() === 'active')) items.push(['Alta','Nenhuma pesquisa ativa','Inicie uma campanha para manter a escuta contínua.','/Experience/Campaigns']);
    if (completionRate < 60) items.push(['Média','Adesão pede atenção',`A taxa atual é ${completionRate}%. Compartilhe um lembrete.`, '/Experience/Campaigns']);
    if (!responses.length) items.push(['Informativa','Resultado executivo pendente','Receba as primeiras respostas para ativar a leitura.','/Surveys']);
    host.innerHTML = items.map(x => `<article><span>${x[0]}</span><div><strong>${x[1]}</strong><small>${x[2]}</small></div><a href="${x[3]}">Resolver</a></article>`).join('') || '<div class="valora-empty">Nenhuma prioridade crítica no momento.</div>';
  }

  function renderActivity(items) {
    const body = root.querySelector('[data-items]');
    const empty = root.querySelector('[data-empty-activities]');
    const table = root.querySelector('[data-activity-table]');
    if (!items.length) return;
    empty.hidden = true;
    table.hidden = false;
    body.replaceChildren(...items.slice(0, 8).map(item => {
      const row = document.createElement('tr');
      [item.name ?? item.title ?? 'Atividade', item.status ?? 'Registrada', item.description ?? item.email ?? ''].forEach(value => {
        const cell = document.createElement('td');
        cell.textContent = String(value);
        row.append(cell);
      });
      return row;
    }));
  }

  let currentLoadGeneration = 0;

  function showDashboardError(container, message, correlationId, retryAction) {
    container.replaceChildren();
    const alertBox = document.createElement('div');
    alertBox.className = 'alert alert-danger d-flex flex-column gap-2 p-3 my-3';

    const msg = document.createElement('div');
    msg.textContent = message;
    alertBox.append(msg);

    if (correlationId) {
      const refBox = document.createElement('div');
      refBox.className = 'd-flex align-items-center gap-2 flex-wrap text-muted small';
      const refLabel = document.createElement('span');
      refLabel.textContent = `Código de atendimento: `;
      const refCode = document.createElement('code');
      refCode.className = 'user-select-all px-2 py-1 bg-light rounded border';
      refCode.textContent = correlationId;

      const copyBtn = document.createElement('button');
      copyBtn.type = 'button';
      copyBtn.className = 'btn btn-sm btn-outline-secondary';
      copyBtn.textContent = 'Copiar código';
      copyBtn.onclick = async () => {
        if (navigator.clipboard?.writeText) {
          try {
            await navigator.clipboard.writeText(correlationId);
            copyBtn.textContent = 'Copiado!';
            setTimeout(() => { copyBtn.textContent = 'Copiar código'; }, 2000);
            return;
          } catch {
            // Se a permissão for negada, seleciona o texto para cópia manual
          }
        }
        const range = document.createRange();
        range.selectNodeContents(refCode);
        const sel = window.getSelection();
        sel?.removeAllRanges();
        sel?.addRange(range);
        copyBtn.textContent = 'Pressione Ctrl+C';
        setTimeout(() => { copyBtn.textContent = 'Copiar código'; }, 2500);
      };

      refBox.append(refLabel, refCode, copyBtn);
      alertBox.append(refBox);
    } else {
      const refBox = document.createElement('div');
      refBox.className = 'text-muted small';
      refBox.textContent = 'Código de atendimento: não informado pelo servidor.';
      alertBox.append(refBox);
    }

    if (retryAction) {
      const actions = document.createElement('div');
      actions.className = 'mt-2';
      const retry = document.createElement('button');
      retry.type = 'button';
      retry.className = 'valora-button valora-button--secondary';
      retry.textContent = 'Tentar novamente';
      retry.addEventListener('click', retryAction, { once: true });
      actions.append(retry);
      alertBox.append(actions);
    }

    container.append(alertBox);
    container.hidden = false;
  }

  function resetDashboardData() {
    setText('[data-kpi="responses"]', '—');
    setText('[data-kpi="activeSurveys"]', '—');
    setText('[data-kpi="completionRate"]', '—');
    setText('[data-kpi="averageLevel"]', '—');
    setText('[data-kpi="evidence"]', '—');
    setText('[data-kpi="actions"]', '—');
    setText('[data-score]', '—');
    setText('[data-level]', '—');
    setText('[data-maturity-score]', '—');
    setText('[data-maturity-level]', '—');
    setText('[data-maturity-explanation]', '');
    setText('[data-strongest]', '—');
    setText('[data-weakest]', '—');
    setText('[data-blocker]', '—');
    setText('[data-next-step]', '—');
    const body = root.querySelector('[data-items]');
    if (body) body.replaceChildren();
    const empty = root.querySelector('[data-empty-activities]');
    if (empty) empty.hidden = false;
    const table = root.querySelector('[data-activity-table]');
    if (table) table.hidden = true;
    const strategic = root.querySelector('[data-strategic-reading]');
    if (strategic) strategic.hidden = true;
    const scorePanel = root.querySelector('[data-score-panel]');
    if (scorePanel) scorePanel.hidden = true;
    const emptyScore = root.querySelector('[data-empty-score]');
    if (emptyScore) emptyScore.hidden = false;
    const summaryCards = root.querySelector('[data-summary-cards]');
    if (summaryCards) summaryCards.hidden = true;
    const metricEmpty = root.querySelector('[data-metric-empty]');
    if (metricEmpty) metricEmpty.hidden = false;
    const attention = root.querySelector('[data-dashboard-attention]');
    if (attention) attention.innerHTML = '<div class="valora-empty">Carregando prioridades…</div>';
    const onboarding = root.querySelector('[data-onboarding]');
    if (onboarding) onboarding.hidden = true;
  }

  async function load() {
    const thisGeneration = ++currentLoadGeneration;
    const errorContainer = root.querySelector('[data-error]');
    if (errorContainer) {
      errorContainer.hidden = true;
      errorContainer.replaceChildren();
    }
    root.classList.add('is-dashboard-loading');
    resetDashboardData();
    setText('[data-last-update]', 'Verificando contexto…');

    const platformPanel = root.querySelector('[data-platform-mode-panel]');
    const summaryCards = root.querySelector('[data-summary-cards]');
    const scorePanel = root.querySelector('[data-score-panel]');
    const emptyScore = root.querySelector('[data-empty-score]');
    const metricEmpty = root.querySelector('[data-metric-empty]');
    const strategic = root.querySelector('[data-strategic-reading]');
    const attention = root.querySelector('[data-dashboard-attention]');

    let account = null;
    try {
      const contextRes = await fetch('/bff/account/context', {
        credentials: 'same-origin',
        headers: { Accept: 'application/json' }
      });

      if (thisGeneration !== currentLoadGeneration) return;

      if (contextRes.status === 401) {
        root.classList.remove('is-dashboard-loading');
        setText('[data-last-update]', 'Sessão expirada');
        if (errorContainer) {
          const alert = document.createElement('div');
          alert.className = 'alert alert-warning p-3 my-3';
          alert.innerHTML = '<strong>Sua sessão expirou.</strong> É necessário entrar novamente para continuar. ' +
            `<a class="valora-button valora-button--primary btn-sm ms-2" href="/Account/Login?reason=session-expired&returnUrl=${encodeURIComponent(location.pathname + location.search)}">Entrar novamente</a>`;
          errorContainer.replaceChildren(alert);
          errorContainer.hidden = false;
        }
        return;
      }

      if (contextRes.status === 403) {
        root.classList.remove('is-dashboard-loading');
        const problem = await contextRes.json().catch(() => ({}));
        const requiresOrg = problem.requiresOrganizationSelection || problem.code === 'ORGANIZATION_REQUIRED';
        if (requiresOrg) {
          setText('[data-last-update]', 'Seleção de organização necessária');
          if (platformPanel) platformPanel.hidden = false;
          if (summaryCards) summaryCards.hidden = true;
          if (scorePanel) scorePanel.hidden = true;
          if (emptyScore) emptyScore.hidden = true;
          if (strategic) strategic.hidden = true;
          if (attention) attention.innerHTML = '<div class="valora-empty">Selecione uma organização no menu superior para visualizar o cockpit operacional.</div>';
          return;
        }

        setText('[data-last-update]', 'Acesso negado');
        if (errorContainer) {
          showDashboardError(errorContainer,
            problem.detail || problem.message || 'Seu perfil não possui autorização para consultar este painel.',
            problem.correlationId || problem.traceId,
            () => load());
        }
        return;
      }

      if (!contextRes.ok) {
        root.classList.remove('is-dashboard-loading');
        setText('[data-last-update]', 'Contexto temporariamente indisponível');
        const problem = await contextRes.json().catch(() => ({}));
        if (errorContainer) {
          showDashboardError(errorContainer,
            'Contexto temporariamente indisponível. Não foi possível validar o perfil ou organização.',
            problem.correlationId || problem.traceId,
            () => load());
        }
        return;
      }

      account = await contextRes.json();
    } catch {
      if (thisGeneration !== currentLoadGeneration) return;
      root.classList.remove('is-dashboard-loading');
      setText('[data-last-update]', 'Contexto temporariamente indisponível');
      if (errorContainer) {
        showDashboardError(errorContainer,
          'Falha de comunicação ao verificar o contexto da sessão. Verifique sua conexão com o servidor.',
          null,
          () => load());
      }
      return;
    }

    if (thisGeneration !== currentLoadGeneration) return;

    const isPlatformAdmin = account && (account.primaryRole === 'admin_valora'
      || (Array.isArray(account.roleCodes) && account.roleCodes.map(r => String(r).toLowerCase()).includes('admin_valora')));
    const hasOrg = Boolean(account?.organizationId);

    // Estado: MODO PLATAFORMA (Administrador Global sem organização selecionada)
    if (isPlatformAdmin && !hasOrg) {
      root.classList.remove('is-dashboard-loading');
      setText('[data-first-name]', account?.userName ? account.userName.split(' ')[0] : 'Administrador');
      setText('[data-organization]', 'Modo Plataforma (Visão Global)');
      setText('[data-last-update]', 'Modo Plataforma · Selecione uma organização para carregar o cockpit');

      if (platformPanel) platformPanel.hidden = false;
      if (scorePanel) scorePanel.hidden = true;
      if (emptyScore) emptyScore.hidden = true;
      if (summaryCards) summaryCards.hidden = true;
      if (metricEmpty) metricEmpty.hidden = true;
      if (strategic) strategic.hidden = true;
      if (attention) attention.innerHTML = '<div class="valora-empty">Selecione uma organização no topo para consultar as prioridades do cliente.</div>';
      return;
    }

    // Estado: SELEÇÃO NECESSÁRIA (Usuário comum sem organização atribuída)
    if (!hasOrg) {
      root.classList.remove('is-dashboard-loading');
      setText('[data-last-update]', 'Seleção de organização necessária');
      if (summaryCards) summaryCards.hidden = true;
      if (scorePanel) scorePanel.hidden = true;
      if (emptyScore) emptyScore.hidden = true;
      if (strategic) strategic.hidden = true;
      if (attention) attention.innerHTML = '<div class="valora-empty">Nenhuma organização associada ao seu usuário. Entre em contato com o administrador.</div>';
      return;
    }

    // Estado: ORGANIZAÇÃO VÁLIDA SELECIONADA
    if (platformPanel) platformPanel.hidden = true;
    if (account?.organizationName) {
      setText('[data-organization]', account.organizationName);
    }
    setText('[data-first-name]', account?.userName ? account.userName.split(' ')[0] : 'líder');
    setText('[data-last-update]', 'Atualizando leitura…');

    try {
      const [surveysResult, responsesResult, usageResult, intelligenceResult, actionsResult] = await Promise.allSettled([
        SurveysApi.list(), ResponsesApi.list({}), UsageApi.usage(), IntelligenceApi.dashboard(), IntelligenceApi.actions()
      ]);

      if (thisGeneration !== currentLoadGeneration) return;

      const surveys = surveysResult.status === 'fulfilled' ? arrayOf(surveysResult.value) : [];
      const responses = responsesResult.status === 'fulfilled' ? arrayOf(responsesResult.value) : [];
      surveys.__available = surveysResult.status === 'fulfilled';
      responses.__available = responsesResult.status === 'fulfilled';
      const usage = usageResult.status === 'fulfilled' ? { ...unwrap(usageResult.value), __available: true } : { __available: false };
      const intelligence = intelligenceResult.status === 'fulfilled' ? { ...unwrap(intelligenceResult.value), __available: true } : { __available: false };
      const actions = actionsResult.status === 'fulfilled' ? arrayOf(actionsResult.value) : [];
      actions.__available = actionsResult.status === 'fulfilled';

      renderKpis({ surveys, responses, usage, intelligence, actions });
      renderActivity([...surveys, ...responses]);

      const results = [surveysResult, responsesResult, usageResult, intelligenceResult, actionsResult];
      const succeeded = results.filter(item => item.status === 'fulfilled').length;
      if (!succeeded) {
        const firstError = results.find(item => item.status === 'rejected')?.reason;
        if (firstError?.status === 403 || firstError?.statusCode === 403) {
          setText('[data-last-update]', 'Organização necessária');
          if (platformPanel) platformPanel.hidden = false;
          return;
        }
        throw firstError || new Error('Dashboard indisponível');
      }

      setText('[data-last-update]', succeeded === results.length
        ? `Atualizado em ${new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date())}`
        : `Atualização parcial · ${succeeded} de ${results.length} fontes disponíveis`);
    } catch (exception) {
      if (thisGeneration !== currentLoadGeneration) return;
      const correlationId = exception?.correlationId || exception?.details?.correlationId || exception?.TraceId || exception?.traceId || null;
      if (errorContainer) {
        showDashboardError(errorContainer,
          'Não foi possível atualizar a leitura executiva.',
          correlationId,
          () => load());
      }
      setText('[data-last-update]', 'Atualização indisponível');
    } finally {
      if (thisGeneration === currentLoadGeneration) {
        root.classList.remove('is-dashboard-loading');
      }
    }
  }

  root.querySelector('[data-hide-onboarding]')?.addEventListener('click', () => {
    localStorage.setItem('valora.onboarding.hidden', 'true');
    const host = root.querySelector('[data-onboarding]');
    if (host) host.hidden = true;
    const restoreBtn = root.querySelector('[data-restore-onboarding]');
    if (restoreBtn) restoreBtn.hidden = false;
    window.Toast?.success?.('Jornada guiada ocultada.');
  });
  root.querySelector('[data-restore-onboarding]')?.addEventListener('click', () => {
    localStorage.removeItem('valora.onboarding.hidden');
    load();
    window.Toast?.success?.('Jornada guiada restaurada.');
  });
  load();
})();
