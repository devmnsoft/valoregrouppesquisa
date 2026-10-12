(() => {
  'use strict';

  const host = document.querySelector('[data-page="diagnostic-workspace"]');
  if (!host) return;

  const diagnosticId = host.dataset.diagnosticId;
  const content = host.querySelector('[data-content]');
  const loading = host.querySelector('[data-loading]');
  const error = host.querySelector('[data-error]');
  const message = host.querySelector('[data-message]');
  let overviewState = null;

  const esc = value => {
    const span = document.createElement('span');
    span.textContent = value == null ? '' : String(value);
    return span.innerHTML;
  };
  const date = value => value ? new Date(value).toLocaleDateString('pt-BR') : 'A definir';
  const label = value => ({
    draft: 'Rascunho', proposed: 'Proposto', approved: 'Aprovado', in_execution: 'Em execução',
    pending: 'Pendente', in_progress: 'Em execução', blocked: 'Bloqueada', overdue: 'Atrasada',
    completed: 'Concluída', canceled: 'Cancelada', critical: 'Crítica', high: 'Alta',
    medium: 'Média', low: 'Baixa', diagnostic: 'Diagnóstico', result: 'Resultado',
    ai_insight: 'Insight revisado', manual: 'Manual'
  }[value] || value || 'Não informado');

  async function api(path = '', method = 'GET') {
    const response = await fetch(`/bff/diagnostics/${diagnosticId}/workspace${path ? `/${path}` : ''}`, {
      method,
      headers: {
        Accept: 'application/json',
        ...(method === 'POST' ? { RequestVerificationToken: host.querySelector('[name="__RequestVerificationToken"]').value } : {})
      }
    });
    const body = await response.json().catch(() => ({}));
    if (!response.ok) throw body;
    return body.data ?? body;
  }

  async function campaignApi(action = '', method = 'GET', body) {
    const response = await fetch(`/bff/diagnostics/${diagnosticId}/campaign${action ? `/${action}` : ''}`, {
      method,
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        ...(method === 'POST' ? { RequestVerificationToken: host.querySelector('[name="__RequestVerificationToken"]').value } : {})
      },
      body: body ? JSON.stringify(body) : undefined
    });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok) throw payload;
    return payload.data ?? payload;
  }

  const empty = text => `<div class="empty-state"><h3>Dados insuficientes</h3><p>${esc(text || 'As informações disponíveis ainda não permitem concluir esta análise com segurança.')}</p></div>`;

  function cards(items) {
    if (!items?.length) return empty();
    return `<div class="row g-3">${items.map(item => {
      const data = item.data || item;
      const rows = Object.entries(data)
        .filter(([key]) => !['data', 'organization_id', 'participant_email', 'participant_name', 'raw_value'].includes(key))
        .slice(0, 8)
        .map(([key, value]) => `<div><dt>${esc(key.replaceAll('_', ' '))}</dt><dd>${esc(typeof value === 'object' ? JSON.stringify(value) : value)}</dd></div>`)
        .join('');
      return `<article class="col-12 col-lg-6"><div class="metric-card h-100"><strong>${esc(item.code || data.title || 'Registro do ciclo')}</strong> <span class="valora-status">${esc(item.status || item.mappingStatus)}</span><dl>${rows}</dl></div></article>`;
    }).join('')}</div>`;
  }

  function setOverviewHeader(data) {
    const cycle = data.cycle;
    overviewState = data;
    host.querySelector('[data-title]').textContent = cycle.title;
    host.querySelector('[data-description]').textContent = cycle.description || 'Ciclo oficial de evolução organizacional.';
    host.querySelector('[data-status]').textContent = cycle.status;
    host.querySelector('[data-confidence]').textContent = `Confiança ${cycle.confidenceLevel ?? 'a calcular'}`;
    host.querySelector('[data-value="responses"]').textContent = cycle.responseCount;
    host.querySelector('[data-value="evidence"]').textContent = cycle.evidenceCount;
    host.querySelector('[data-value="coverage"]').textContent = cycle.evidenceCount ? 'Mapeada' : 'A calcular';
    host.querySelector('[data-value="processing"]').textContent = data.lastJob?.status || cycle.processingStatus;
    host.querySelector('[data-progress]').style.width = `${({ draft: 10, published: 30, active: 45, processing: 65, processed: 80, report_ready: 90, closed: 100 }[cycle.status] || 20)}%`;
  }

  function diagnosticPlanUrl(cycle) {
    const returnUrl = `/Diagnostics/${diagnosticId}/Workspace?tab=actions`;
    return `/ActionCenter/Plans/Create?originType=diagnostic&originId=${encodeURIComponent(cycle.surveyId)}&returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  function overview(data) {
    setOverviewHeader(data);
    const cycle = data.cycle;
    content.innerHTML = `<h2>Visão Geral</h2>
      <section class="valora-card"><h3>Como usar</h3><p>Revise respostas, evidências e limitações antes de converter um achado em plano. A execução continua no Action Center para preservar responsável, prazo, histórico e evidência de conclusão.</p></section>
      ${cycle.responseCount ? `${cards(data.mainIndices)}${cards(data.mainInsights)}${cards(data.recommendedActions)}` : '<div class="alert alert-info">Compartilhe o link público para iniciar a coleta de evidências.</div>'}
      <section class="valora-card"><h3>Próxima ação permitida</h3><p>${cycle.evidenceCount ? 'Há evidências registradas. Crie um plano vinculado ao diagnóstico ou revise atividades existentes na aba Action™.' : 'A criação de plano continua disponível, mas registre a limitação se ainda não houver evidência suficiente.'}</p><a class="valora-button valora-button--primary" href="${diagnosticPlanUrl(cycle)}">Criar plano vinculado</a></section>`;
  }

  function executionView(data) {
    const cycle = overviewState?.cycle;
    const returnUrl = `/Diagnostics/${diagnosticId}/Workspace?tab=actions`;
    const createUrl = cycle ? diagnosticPlanUrl(cycle) : '/ActionCenter/Plans/Create';
    const plans = data.plans?.length
      ? `<div class="table-responsive" tabindex="0"><table class="valora-table"><thead><tr><th>Plano</th><th>Responsável</th><th>Situação</th><th>Prioridade</th><th>Progresso</th><th>Prazo</th><th></th></tr></thead><tbody>${data.plans.map(plan => `<tr><td><strong>${esc(plan.title)}</strong><small class="d-block">Origem: ${esc(label(plan.originType))}</small></td><td>${esc(plan.ownerName || 'Sem responsável')}</td><td>${esc(label(plan.status))}</td><td>${esc(label(plan.priority))}</td><td><progress max="100" value="${plan.progressPercent}"></progress> ${plan.progressPercent}%</td><td>${esc(date(plan.dueAt))}</td><td><a href="/ActionCenter/Plans/Details/${encodeURIComponent(plan.id)}?returnUrl=${encodeURIComponent(returnUrl)}">Abrir</a></td></tr>`).join('')}</tbody></table></div>`
      : '<p>Nenhum plano está vinculado a este diagnóstico.</p>';
    const items = data.items?.length
      ? `<div class="table-responsive" tabindex="0"><table class="valora-table"><thead><tr><th>Atividade</th><th>Responsável</th><th>Situação</th><th>Prazo</th><th>Evidência</th><th></th></tr></thead><tbody>${data.items.map(item => `<tr><td><strong>${esc(item.title)}</strong><small class="d-block">${esc(item.planTitle)} · ${item.progressPercent}%</small></td><td>${esc(item.responsibleName || 'Sem responsável')}</td><td>${esc(label(item.status))}${item.isOverdue && item.status !== 'completed' ? '<small class="d-block text-danger">Também atrasada</small>' : ''}</td><td>${esc(date(item.dueAt))}</td><td>${esc(item.completionEvidence || item.evidenceSummary || 'Não informada')}</td><td><a href="/ActionCenter/Items/Details/${encodeURIComponent(item.id)}?returnUrl=${encodeURIComponent(returnUrl)}">Abrir</a></td></tr>`).join('')}</tbody></table></div>`
      : '<p>Nenhuma atividade vinculada aparece no escopo autorizado.</p>';
    const history = data.history?.length
      ? `<ol class="action-history">${data.history.map(row => `<li><strong>${esc(row.title)}</strong><span>${esc(label(row.operation))} · ${esc(row.actorName || 'Sistema')} · ${esc(date(row.changedAt))}</span><small>${esc([row.fromValue, row.toValue].filter(Boolean).join(' → ') || row.reason || 'Registro operacional preservado.')}</small></li>`).join('')}</ol>`
      : '<p>O histórico aparecerá quando houver criação, vínculo, atribuição, bloqueio, retomada ou conclusão.</p>';
    content.innerHTML = `<div class="d-flex justify-content-between gap-3 flex-wrap"><div><h2>Action™</h2><p>Planos e atividades vinculados ao diagnóstico com os mesmos dados do Action Center.</p></div><a class="valora-button valora-button--primary" href="${createUrl}">Criar plano vinculado</a></div><section class="valora-card"><h3>Planos vinculados</h3>${plans}</section><section class="valora-card"><h3>Atividades em execução</h3>${items}</section><section class="valora-card"><h3>Histórico no contexto</h3>${history}</section>`;
  }

  function campaignView(data) {
    content.innerHTML = `<div class="d-flex justify-content-between gap-3 flex-wrap"><div><h2>Campanha</h2><p>Convite e distribuição do link público com governança.</p></div><span class="valora-status">${esc(data.status)}</span></div><div class="metric-grid"><article class="metric-card"><span>Destinatários protegidos</span><strong>${data.recipientCount}</strong></article><article class="metric-card"><span>Enviados</span><strong>${data.sentCount}</strong></article><article class="metric-card"><span>Falhas</span><strong>${data.failedCount}</strong></article></div><div class="valora-card mt-3"><h3>${esc(data.name)}</h3><p>${esc(data.message)}</p><label class="form-label">Link público</label><div class="input-group"><input class="form-control" readonly value="${esc(data.publicUrl || 'Diagnóstico ainda não publicado')}"><button class="btn btn-primary" data-copy-link ${data.publicUrl ? '' : 'disabled'}>Copiar link</button><button class="btn btn-outline-primary" data-copy-message>Copiar mensagem</button></div><div class="d-flex gap-2 mt-3"><button class="btn btn-primary" data-send>Preparar envio</button><button class="btn btn-outline-danger" data-cancel>Cancelar pendentes</button></div></div>`;
    content.querySelector('[data-copy-link]')?.addEventListener('click', () => navigator.clipboard.writeText(data.publicUrl));
    content.querySelector('[data-copy-message]').addEventListener('click', () => navigator.clipboard.writeText(`${data.message}\n${data.publicUrl || ''}`));
    content.querySelector('[data-send]').addEventListener('click', () => campaignCommand('send'));
    content.querySelector('[data-cancel]').addEventListener('click', () => campaignCommand('cancel'));
  }

  async function campaignCommand(action) {
    try {
      const result = await campaignApi(action, 'POST');
      message.textContent = result.message;
      message.classList.remove('d-none');
      await tab('campaign');
    } catch (ex) {
      error.textContent = ex.message || 'Não foi possível concluir a ação.';
      error.classList.remove('d-none');
    }
  }

  function campaignEmpty() {
    content.innerHTML = `<div class="empty-state"><h2>Crie a campanha deste diagnóstico</h2><p>Configure uma mensagem real e use o link público quando o diagnóstico estiver publicado.</p><form data-create-campaign class="valora-form"><label>Nome<input name="name" required maxlength="180" class="form-control"></label><label>Canal<select name="channel" class="form-select"><option value="manual">Compartilhamento manual</option><option value="whatsapp_manual">WhatsApp manual</option><option value="email">E-mail configurado</option></select></label><label>Assunto (opcional)<input name="subject" maxlength="180" class="form-control"></label><label>Mensagem<textarea name="message" required maxlength="10000" rows="5" class="form-control"></textarea></label><p class="form-text">O e-mail somente será habilitado quando o SMTP deste ambiente estiver válido.</p><button class="btn btn-primary mt-3">Criar campanha</button></form></div>`;
    content.querySelector('form').addEventListener('submit', async event => {
      event.preventDefault();
      const form = new FormData(event.target);
      try {
        await campaignApi('', 'POST', { name: form.get('name'), channel: form.get('channel'), subject: form.get('subject'), message: form.get('message'), recipients: [] });
        message.textContent = 'Campanha criada. O link e a mensagem de convite já estão disponíveis para compartilhamento.';
        message.classList.remove('d-none');
        await tab('campaign');
      } catch (ex) {
        error.textContent = ex.message || 'Não foi possível criar a campanha.';
        error.classList.remove('d-none');
      }
    });
  }

  async function tab(name) {
    loading.classList.remove('d-none');
    error.classList.add('d-none');
    try {
      if (name === 'campaign') {
        try { campaignView(await campaignApi()); } catch (ex) { if (ex.code === 'CAMPAIGN_NOT_FOUND') campaignEmpty(); else throw ex; }
        return;
      }
      if (name === 'actions') {
        if (!overviewState) setOverviewHeader(await api('overview'));
        executionView(await api('execution'));
        return;
      }
      if (name === 'certificates') { location.href = '/Certificates'; return; }
      if (name === 'exports') { location.href = '/Exports'; return; }
      if (name === 'shares') {
        content.innerHTML = '<div class="empty-state"><h2>Compartilhamentos controlados</h2><p>Gere primeiro um snapshot do Executive Report™. Os links são criados no detalhe do relatório, com expiração e revogação.</p><a class="btn btn-primary" href="/Intelligence/ExecutiveReport">Abrir Executive Report™</a></div>';
        return;
      }
      const data = await api(name);
      if (name === 'overview') overview(data);
      else {
        const note = { evolution: 'Primeiro ciclo registrado. A evolução longitudinal será calculada a partir do próximo ciclo.', benchmark: 'Ainda não existem dados suficientes para gerar um Benchmark Externo confiável para este perfil organizacional.', report: 'Exportação PDF ainda não configurada neste ambiente. O preview executivo está disponível.' }[name];
        content.innerHTML = `<h2>${esc(host.querySelector(`[data-tab="${name}"]`).textContent)}</h2>${cards(data)}${note ? `<div class="alert alert-info">${note}</div>` : ''}`;
      }
    } catch (ex) {
      error.textContent = ex.message || 'Não foi possível carregar este módulo. Os dados originais permanecem preservados.';
      error.classList.remove('d-none');
    } finally {
      loading.classList.add('d-none');
    }
  }

  host.querySelector('[data-tabs]').addEventListener('click', event => {
    const button = event.target.closest('[data-tab]');
    if (button) tab(button.dataset.tab);
  });
  host.querySelectorAll('[data-command]').forEach(button => button.addEventListener('click', async () => {
    if (button.dataset.command === 'close' && !await window.ValoraUI.confirm('Encerrar este ciclo e bloquear novas respostas?')) return;
    button.disabled = true;
    try {
      const result = await api(button.dataset.command, 'POST');
      message.textContent = result.message;
      message.classList.remove('d-none');
      await tab('overview');
    } catch (ex) {
      error.textContent = ex.message || 'Não foi possível concluir a ação.';
      error.classList.remove('d-none');
    } finally {
      button.disabled = false;
    }
  }));

  tab(new URLSearchParams(location.search).get('tab') || 'overview');
})();
