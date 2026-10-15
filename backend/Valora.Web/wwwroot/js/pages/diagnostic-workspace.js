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
  let campaignLink = null;
  let activeRequest = 0;
  const allowedTabs = new Set(['overview', 'responses', 'participation', 'evidence', 'metrics', 'indices', 'heatmap', 'radar', 'inferences', 'insights', 'actions', 'evolution', 'benchmark', 'report', 'campaign', 'certificates', 'exports', 'shares']);

  const esc = value => {
    const span = document.createElement('span');
    span.textContent = value == null ? '' : String(value);
    return span.innerHTML;
  };
  const date = (value, timeZone) => value ? new Date(value).toLocaleDateString('pt-BR', timeZone ? { timeZone } : undefined) : 'A definir';
  const label = value => ({
    draft: 'Rascunho', proposed: 'Proposto', approved: 'Aprovado', in_execution: 'Em execução',
    pending: 'Pendente', in_progress: 'Em execução', blocked: 'Bloqueada', overdue: 'Atrasada',
    completed: 'Concluída', canceled: 'Cancelada', critical: 'Crítica', high: 'Alta',
    medium: 'Média', low: 'Baixa', diagnostic: 'Diagnóstico', result: 'Resultado',
    ai_insight: 'Insight revisado', manual: 'Manual', active: 'Ativo', open: 'Aberto',
    closed: 'Encerrado', archived: 'Arquivado', reviewed: 'Reavaliado', monitoring: 'Em acompanhamento',
    sending: 'Envio em preparação', scheduled: 'Agendada', paused: 'Pausada', failed: 'Falha',
    running: 'Em processamento', retry_scheduled: 'Nova tentativa agendada', cancelled: 'Cancelado'
  }[value] || value || 'Não informado');

  async function api(path = '', method = 'GET', signal) {
    const response = await fetch(`/bff/diagnostics/${diagnosticId}/workspace${path ? `/${path}` : ''}`, {
      method,
      signal,
      headers: {
        Accept: 'application/json',
        ...(method === 'POST' ? { 'X-CSRF-TOKEN': host.querySelector('[name="__RequestVerificationToken"]').value } : {})
      }
    });
    const body = await response.json().catch(() => ({}));
    if (!response.ok) throw body;
    return body.data ?? body;
  }

  async function campaignApi(action = '', method = 'GET', body, signal) {
    const response = await fetch(`/bff/diagnostics/${diagnosticId}/campaign${action ? `/${action}` : ''}`, {
      method,
      signal,
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
        ...(method === 'POST' ? { 'X-CSRF-TOKEN': host.querySelector('[name="__RequestVerificationToken"]').value } : {})
      },
      body: body ? JSON.stringify(body) : undefined
    });
    const payload = await response.json().catch(() => ({}));
    const access = {
      canManage: response.headers.get('X-Valora-Campaign-Manage') === 'true',
      canDistribute: response.headers.get('X-Valora-Campaign-Distribute') === 'true',
      readOnly: response.headers.get('X-Valora-Module-Access') === 'read-only'
    };
    if (!response.ok) throw { ...payload, ...access };
    return method === 'GET' ? { ...(payload.data ?? payload), ...access } : payload.data ?? payload;
  }

  const empty = text => `<div class="empty-state"><h3>Dados insuficientes</h3><p>${esc(text || 'As informações disponíveis ainda não permitem concluir esta análise com segurança.')}</p></div>`;
  const coverageLabel = cycle => {
    const total = Number(cycle.evidenceCount || 0);
    const mapped = Number(cycle.mappedEvidenceCount || 0);
    const pending = Number(cycle.pendingEvidenceCount || Math.max(total - mapped, 0));
    if (!total) return 'Sem evidências';
    if (!mapped && pending) return `${pending}/${total} pendentes`;
    if (pending) return `${mapped}/${total} mapeadas`;
    return `${mapped}/${total} mapeadas`;
  };
  const stage = status => ({
    draft: 'Rascunho', published: 'Publicado', active: 'Coleta ativa', processing: 'Processamento',
    processed: 'Processado', report_ready: 'Relatório preparado', closed: 'Encerrado'
  }[status] || label(status));

  function cards(items) {
    if (!items?.length) return empty();
    return `<div class="row g-3">${items.map(item => {
      const data = item.data || item;
      const title = data.title || data.name || item.code || 'Registro do ciclo';
      const statusBadge = item.status || item.mappingStatus || data.status;
      const rows = Object.entries(data)
        .filter(([key]) => !['data', 'organization_id', 'participant_email', 'participant_name', 'raw_value', 'title', 'name', 'status', 'id', 'survey_id', 'cycle_id'].includes(key))
        .slice(0, 6)
        .map(([key, value]) => `<div><dt>${esc(key.replaceAll('_', ' '))}</dt><dd>${esc(typeof value === 'object' ? JSON.stringify(value) : value)}</dd></div>`)
        .join('');
      return `<article class="col-12 col-lg-6"><div class="metric-card h-100"><div class="d-flex justify-content-between align-items-start gap-2 mb-2"><strong>${esc(title)}</strong> ${statusBadge ? `<span class="valora-status">${esc(label(statusBadge))}</span>` : ''}</div><dl class="small mb-0">${rows}</dl></div></article>`;
    }).join('')}</div>`;
  }

  function participationView(items) {
    const item = items?.[0];
    const data = item?.data || {};
    const origens = data.origens || {};
    const destEmail = Number(data.destinatariosEmail || 0);
    const respValidas = Number(data.respostasValidas || 0);
    const taxaAdesao = destEmail > 0 ? `${Math.round(respValidas / destEmail * 100)}%` : 'Não aplicável (sem lista de e-mails)';
    content.innerHTML = `<h2>Participação & Coleta</h2>
      <p class="form-help">Os contadores refletem o estado real persistido no banco de dados. Compartilhamento manual não é contado como envio confirmado.</p>
      <div class="metric-grid">
        <article class="metric-card"><span>Respostas recebidas</span><strong>${esc(data.respostasRecebidas ?? 0)}</strong><small>total gravado</small></article>
        <article class="metric-card"><span>Respostas válidas</span><strong>${esc(data.respostasValidas ?? 0)}</strong><small>concluídas e íntegras</small></article>
        <article class="metric-card"><span>Respostas pontuadas</span><strong>${esc(data.respostasPontuadas ?? 0)}</strong><small>com maturidade apurada</small></article>
        <article class="metric-card"><span>Pendências de processamento</span><strong>${esc(data.pendenciasProcessamento ?? 0)}</strong><small>jobs em processamento</small></article>
        <article class="metric-card"><span>Destinatários por e-mail</span><strong>${esc(data.destinatariosEmail ?? 0)}</strong><small>cadastrados na campanha</small></article>
        <article class="metric-card"><span>Mensagens na fila</span><strong>${esc(data.mensagensFila ?? 0)}</strong><small>aguardando envio</small></article>
        <article class="metric-card"><span>Envios registrados</span><strong>${esc(data.enviosRegistrados ?? 0)}</strong><small>entregues/enviados</small></article>
        <article class="metric-card"><span>Falhas de entrega</span><strong>${esc(data.falhas ?? 0)}</strong><small>erros ou devoluções</small></article>
        <article class="metric-card"><span>Taxa de adesão</span><strong>${esc(taxaAdesao)}</strong><small>${destEmail > 0 ? 'respostas válidas / destinatários' : 'denominador ausente'}</small></article>
      </div>
      <section class="valora-card mt-3">
        <h3>Origens Auditadas dos Contadores</h3>
        <div class="table-responsive"><table class="valora-table"><thead><tr><th>Contador</th><th>Valor</th><th>Fonte Canônica</th></tr></thead><tbody>
          <tr><td><strong>Respostas recebidas</strong></td><td>${esc(data.respostasRecebidas ?? 0)}</td><td>${esc(origens.respostasRecebidas || 'Tabela responses (is_deleted=false)')}</td></tr>
          <tr><td><strong>Respostas válidas</strong></td><td>${esc(data.respostasValidas ?? 0)}</td><td>${esc(origens.respostasValidas || 'Tabela responses com status=completed')}</td></tr>
          <tr><td><strong>Respostas pontuadas</strong></td><td>${esc(data.respostasPontuadas ?? 0)}</td><td>${esc(origens.respostasPontuadas || 'Tabela result_scores')}</td></tr>
          <tr><td><strong>Pendências de processamento</strong></td><td>${esc(data.pendenciasProcessamento ?? 0)}</td><td>${esc(origens.pendenciasProcessamento || 'Tabela intelligence_processing_jobs')}</td></tr>
          <tr><td><strong>Destinatários por e-mail</strong></td><td>${esc(data.destinatariosEmail ?? 0)}</td><td>${esc(origens.destinatariosEmail || 'Tabela survey_invites')}</td></tr>
          <tr><td><strong>Mensagens na fila</strong></td><td>${esc(data.mensagensFila ?? 0)}</td><td>${esc(origens.mensagensFila || 'Tabela communications pendentes')}</td></tr>
          <tr><td><strong>Envios registrados</strong></td><td>${esc(data.enviosRegistrados ?? 0)}</td><td>${esc(origens.enviosRegistrados || 'Tabela communications enviadas')}</td></tr>
          <tr><td><strong>Falhas</strong></td><td>${esc(data.falhas ?? 0)}</td><td>${esc(origens.falhas || 'Tabela communications com falha')}</td></tr>
        </tbody></table></div>
      </section>`;
  }

  function evidenceView(items) {
    if (!items?.length) return empty('Nenhuma evidência foi registrada para este ciclo.');
    content.innerHTML = `<h2>Evidências Rastreáveis</h2>
      <p class="form-help">Evidências coletadas de respostas reais anonimizadas ou mascaradas para análise metodológica.</p>
      <div class="row g-3">${items.map(item => {
        const data = item.data || item;
        return `<article class="col-12 col-lg-6">
          <div class="metric-card h-100">
            <div class="d-flex justify-content-between align-items-center mb-2">
              <strong>${esc(data.conceptCode || data.metricCode || 'Evidência')}</strong>
              <span class="valora-status">${esc(label(data.mappingStatus || item.status || 'Registrado'))}</span>
            </div>
            <p class="mb-2"><em>${esc(data.maskedAnswer || data.textExcerpt || 'Valor coletado registrado com sucesso.')}</em></p>
            <dl class="small mb-2">
              <div><dt>Tipo de evidência</dt><dd>${esc(data.evidenceType || 'Resposta')}</dd></div>
              <div><dt>Peso metodológico</dt><dd>${esc(data.weight ?? 1)} (Confiança: ${esc(data.confidenceWeight ?? 1)})</dd></div>
              <div><dt>Registro</dt><dd>${esc(date(item.createdAt || data.createdAt))}</dd></div>
            </dl>
          </div>
        </article>`;
      }).join('')}</div>`;
  }

  function insightsView(items) {
    if (!items?.length) return empty('Nenhum insight gerado ou em análise para este diagnóstico.');
    const returnUrl = `/Diagnostics/${diagnosticId}/Workspace?tab=insights`;
    content.innerHTML = `<h2>Insights IA™ & Revisão Humana</h2>
      <p class="form-help">Achados sugeridos por inteligência artificial passam obrigatoriamente por revisão humana antes de se tornarem planos aprovados no Action Center.</p>
      <div class="row g-3">${items.map(item => {
        const data = item.data || item;
        const isApproved = (item.status === 'approved' || data.status === 'approved');
        const isPending = (item.status === 'proposed' || item.status === 'pending_review' || data.status === 'proposed' || data.status === 'pending_review');
        const isConverted = (item.status === 'converted_to_action' || data.status === 'converted_to_action');
        const planLink = `/ActionCenter/Plans/Create?originType=ai_insight&originId=${encodeURIComponent(item.id)}&returnUrl=${encodeURIComponent(returnUrl)}`;
        const reviewLink = `/Insights/Details/${encodeURIComponent(item.id)}?returnUrl=${encodeURIComponent(returnUrl)}`;
        return `<article class="col-12 col-lg-6">
          <div class="metric-card h-100 d-flex flex-column justify-content-between">
            <div>
              <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
                <strong>${esc(data.title || item.code || 'Achado sugerido')}</strong>
                <span class="valora-status">${esc(label(item.status || data.status))}</span>
              </div>
              <p class="text-muted small mb-2">${esc(data.description || data.summary || data.strategicTruth || 'Sem descrição detalhada.')}</p>
              <dl class="small mb-2">
                <div><dt>Natureza</dt><dd>Sugestão de IA™ com revisão humana</dd></div>
                <div><dt>Dimensão</dt><dd>${esc(data.dimension || data.dimensionName || 'Geral')}</dd></div>
                ${data.limitation ? `<div><dt>Limitação</dt><dd class="text-warning">${esc(data.limitation)}</dd></div>` : ''}
              </dl>
            </div>
            <div class="d-flex gap-2 flex-wrap mt-3 pt-2 border-top">
              <a class="btn btn-sm btn-outline-primary" href="${reviewLink}">Revisar achado</a>
              ${isApproved ? `<a class="btn btn-sm btn-primary" href="${planLink}">Criar plano no Action Center</a>` : ''}
              ${isConverted ? `<span class="badge bg-success align-self-center">Vinculado a plano</span>` : ''}
              ${isPending ? `<small class="text-muted align-self-center">Aprovação exigida antes de criar plano.</small>` : ''}
            </div>
          </div>
        </article>`;
      }).join('')}</div>`;
  }

  function metricsIndicesView(name, items) {
    if (!items?.length) return empty(`Nenhum registro de ${name} apurado até o momento.`);
    const tabLabel = host.querySelector(`[data-tab="${name}"]`)?.textContent || name;
    return `<h2>${esc(tabLabel)}</h2>
      <p class="form-help">Indicadores apurados a partir das medições do ciclo. Quando não há dados suficientes, a ausência é informada sem substituição por zero.</p>
      <div class="row g-3">${items.map(item => {
        const data = item.data || item;
        const hasValue = data.value != null || data.score != null || data.normalizedValue != null;
        const displayVal = hasValue ? esc(data.value ?? data.score ?? data.normalizedValue) : '<span class="text-muted">Aguardando dados</span>';
        return `<article class="col-12 col-lg-6">
          <div class="metric-card h-100">
            <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
              <strong>${esc(data.name || data.title || item.code || 'Indicador')}</strong>
              <span class="valora-status">${esc(label(item.status || data.status || 'Ativo'))}</span>
            </div>
            <div class="display-6 my-2">${displayVal} <small class="fs-6 text-muted">${esc(data.unit || '')}</small></div>
            <dl class="small mb-0">
              <div><dt>O que mede</dt><dd>${esc(data.description || data.whatItMeasures || 'Medição metodológica do ciclo.')}</dd></div>
              <div><dt>Fonte</dt><dd>${esc(data.source || 'Respostas do diagnóstico')}</dd></div>
              <div><dt>Período / Recorte</dt><dd>${esc(data.period || 'Ciclo atual')}</dd></div>
              <div><dt>Limitações</dt><dd>${esc(data.limitation || 'Válido sob a amostragem coletada.')}</dd></div>
            </dl>
          </div>
        </article>`;
      }).join('')}</div>`;
  }

  function inferencesView(items) {
    if (!items?.length) return empty('Nenhuma inferência calculada até o momento.');
    return `<h2>Inferências Organizacionais</h2>
      <p class="form-help">Hipóteses e relações identificadas pelo modelo estatístico a partir das correlações observadas.</p>
      <div class="row g-3">${items.map(item => {
        const data = item.data || item;
        return `<article class="col-12 col-lg-6">
          <div class="metric-card h-100">
            <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
              <strong>${esc(data.title || item.code || 'Inferência')}</strong>
              <span class="valora-status">${esc(label(item.status || 'Identificada'))}</span>
            </div>
            <p>${esc(data.statement || data.hypothesis || data.description || 'Relação identificada.')}</p>
            <dl class="small mb-0">
              <div><dt>Natureza</dt><dd>Hipótese metodológica</dd></div>
              <div><dt>Confiança</dt><dd>${esc(data.confidence || 'Padrão estatístico')}</dd></div>
              <div><dt>Premissas</dt><dd>${esc(data.premises || 'Respostas apuradas no ciclo')}</dd></div>
            </dl>
          </div>
        </article>`;
      }).join('')}</div>`;
  }

  function setOverviewHeader(data) {
    const cycle = data.cycle;
    overviewState = data;
    host.querySelector('[data-title]').textContent = cycle.title;
    host.querySelector('[data-description]').textContent = cycle.description || 'Ciclo oficial de evolução organizacional.';
    host.querySelector('[data-status]').textContent = stage(cycle.status);
    host.querySelector('[data-confidence]').textContent = `Confiança ${cycle.confidenceLevel ?? 'a calcular'}`;
    host.querySelector('[data-value="responses"]').textContent = cycle.responseCount;
    host.querySelector('[data-value="evidence"]').textContent = cycle.evidenceCount;
    host.querySelector('[data-value="coverage"]').textContent = coverageLabel(cycle);
    host.querySelector('[data-value="processing"]').textContent = label(data.lastJob?.status || cycle.processingStatus);
  }

  function diagnosticPlanUrl(cycle) {
    const returnUrl = `/Diagnostics/${diagnosticId}/Workspace?tab=actions`;
    return `/ActionCenter/Plans/Create?originType=diagnostic&originId=${encodeURIComponent(cycle.surveyId)}&returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  function overview(data) {
    setOverviewHeader(data);
    const cycle = data.cycle;
    const isDraft = cycle.status === 'draft';
    const hasResponses = Number(cycle.responseCount || 0) > 0;
    const hasEvidence = Number(cycle.evidenceCount || 0) > 0;

    content.innerHTML = `<h2>Visão Geral da Jornada</h2>
      <div class="row g-3 mb-3">
        <article class="col-12 col-md-6">
          <div class="valora-card h-100">
            <h3>Em que etapa está o diagnóstico?</h3>
            <p><span class="badge bg-primary fs-6">${esc(stage(cycle.status))}</span></p>
            <p>${isDraft ? 'O diagnóstico está em preparação/rascunho. Revise o questionário e publique para iniciar a coleta.' : hasResponses ? 'Coleta em andamento com participações registradas. O processamento analítico pode ser acionado.' : 'Diagnóstico ativo aguardando as primeiras submissões dos participantes.'}</p>
          </div>
        </article>
        <article class="col-12 col-md-6">
          <div class="valora-card h-100">
            <h3>Situação das Etapas</h3>
            <ul class="list-unstyled mb-0 small">
              <li>${isDraft ? '⏳' : '✅'} <strong>1. Publicação:</strong> ${isDraft ? 'Pendente de ativação' : 'Publicado e ativo'}</li>
              <li>${hasResponses ? '✅' : '⏳'} <strong>2. Coleta de Respostas:</strong> ${hasResponses ? `${cycle.responseCount} participações gravadas` : 'Nenhuma resposta recebida ainda'}</li>
              <li>${hasEvidence ? '✅' : '⏳'} <strong>3. Evidências & Resultados:</strong> ${hasEvidence ? `${cycle.evidenceCount} evidências metodológicas` : 'Aguardando processamento'}</li>
              <li>${data.recommendedActions?.length ? '✅' : '⏳'} <strong>4. Planos & Execução:</strong> Action Center integrado</li>
            </ul>
          </div>
        </article>
      </div>

      <section class="valora-card mb-3">
        <h3>Próxima Ação Autorizada</h3>
        <p>${hasEvidence ? 'Há evidências e respostas registradas. Crie um plano vinculado ou navegue nos achados para revisão humana.' : hasResponses ? 'Respostas foram recebidas. Processe a inteligência para calcular índices e evidências.' : 'Compartilhe o link da campanha ou convide participantes para iniciar a coleta.'}</p>
        <div class="d-flex gap-2 flex-wrap">
          <a class="valora-button valora-button--primary" href="${diagnosticPlanUrl(cycle)}">Criar plano vinculado</a>
          <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=campaign]').click()">Acessar Campanha & Links</button>
          <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=participation]').click()">Ver Participação Real</button>
        </div>
      </section>

      ${hasResponses ? `${cards(data.mainIndices)}${cards(data.mainInsights)}${cards(data.recommendedActions)}` : '<div class="alert alert-info">Compartilhe o link público na aba Campanha para iniciar a coleta de evidências reais.</div>'}`;
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
    const more = [data.hasMorePlans && 'planos', data.hasMoreItems && 'atividades', data.hasMoreHistory && 'histórico'].filter(Boolean);
    const actionCenterUrl = `/ActionCenter/Plans?diagnosticId=${encodeURIComponent(cycle?.surveyId || diagnosticId)}&returnUrl=${encodeURIComponent(returnUrl)}`;
    const moreNotice = more.length ? `<div class="alert alert-info">Esta visão mostra os registros mais recentes de ${esc(more.join(', '))}. <a href="${actionCenterUrl}">Navegar no Action Center</a>.</div>` : '';
    content.innerHTML = `<div class="d-flex justify-content-between gap-3 flex-wrap"><div><h2>Action™</h2><p>Planos e atividades vinculados ao diagnóstico com os mesmos dados do Action Center.</p></div><a class="valora-button valora-button--primary" href="${createUrl}">Criar plano vinculado</a></div>${moreNotice}<section class="valora-card"><h3>Planos vinculados</h3>${plans}</section><section class="valora-card"><h3>Atividades em execução</h3>${items}</section><section class="valora-card"><h3>Histórico no contexto</h3>${history}</section>`;
  }

  function campaignView(data) {
    const manual = data.channel !== 'email';
    const canSend = data.canManage && !data.readOnly && ['draft', 'scheduled', 'failed'].includes(data.status);
    const canCancel = data.canManage && !data.readOnly && !['closed', 'cancelled'].includes(data.status);
    const channelName = data.channel === 'whatsapp_manual' ? 'WhatsApp manual' : manual ? 'Compartilhamento manual' : 'E-mail';
    content.innerHTML = `<div class="d-flex justify-content-between gap-3 flex-wrap"><div><h2>Campanha</h2><p>${esc(channelName)}</p></div><span class="valora-status">${esc(label(data.status))}</span></div>
      <div class="metric-grid"><article class="metric-card"><span>Destinatários por e-mail</span><strong>${data.recipientCount}</strong></article><article class="metric-card"><span>Na fila</span><strong>${data.queuedCount}</strong></article><article class="metric-card"><span>Envios registrados</span><strong>${data.sentCount}</strong></article><article class="metric-card"><span>Falhas</span><strong>${data.failedCount}</strong></article></div>
      <p class="form-help">Fila não comprova entrega. Compartilhamento manual e cópia de link não são contabilizados como envio.</p>
      <div class="valora-card mt-3"><h3>${esc(data.name)}</h3><p>${esc(data.message)}</p><label class="form-label" for="campaign-public-link">Link público gerado nesta sessão</label><div class="input-group"><input id="campaign-public-link" class="form-control" readonly value="${esc(campaignLink || '')}" placeholder="Gere um link para compartilhar"><button class="btn btn-outline-primary" data-generate-link ${data.canDistribute && !data.readOnly ? '' : 'disabled'}>Gerar link de participação</button><button class="btn btn-primary" data-copy-link ${campaignLink ? '' : 'disabled'}>Copiar link</button><button class="btn btn-outline-primary" data-copy-message ${campaignLink ? '' : 'disabled'}>Copiar convite</button></div>
      <p class="form-help">O token é exibido somente na criação do link. Ao reabrir esta página, gere outro link pelo fluxo existente.</p>
      <div class="d-flex gap-2 mt-3"><button class="btn btn-primary" data-send ${canSend ? '' : 'disabled'}>${manual ? 'Ativar compartilhamento manual' : 'Preparar envio por e-mail'}</button><button class="btn btn-outline-danger" data-cancel ${canCancel ? '' : 'disabled'}>Cancelar pendentes</button></div></div>`;
    const showCopyResult = async text => {
      try {
        await navigator.clipboard.writeText(text);
        message.textContent = 'Conteúdo copiado. Nenhum envio ou entrega foi registrado por esta cópia.';
        message.classList.remove('d-none');
      } catch {
        error.textContent = 'Não foi possível copiar. Selecione o link e copie manualmente.';
        error.classList.remove('d-none');
      }
    };
    content.querySelector('[data-copy-link]').addEventListener('click', () => showCopyResult(campaignLink));
    content.querySelector('[data-copy-message]').addEventListener('click', () => showCopyResult(`${data.message}\n${campaignLink}`));
    content.querySelector('[data-generate-link]').addEventListener('click', async event => {
      const requestId = activeRequest;
      event.currentTarget.disabled = true;
      try {
        const surveyId = overviewState?.cycle?.surveyId || diagnosticId;
        const response = await fetch(`/bff/surveys/${encodeURIComponent(surveyId)}/links`, {
          method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': host.querySelector('[name="__RequestVerificationToken"]').value }, body: '{}'
        });
        const payload = await response.json().catch(() => ({}));
        if (!response.ok) throw payload;
        const result = payload.data ?? payload;
        if (!result.publicUrl) throw new Error('O servidor não retornou um link de participação.');
        campaignLink = new URL(result.publicUrl, location.origin).href;
        if (requestId !== activeRequest) return;
        campaignView(data);
        message.textContent = 'Link gerado. Revise o convite e compartilhe pelo canal escolhido.';
        message.classList.remove('d-none');
      } catch (ex) {
        if (requestId !== activeRequest) return;
        campaignView(data);
        error.textContent = ex.message || 'Não foi possível gerar o link. Verifique publicação, permissão e contratação.';
        error.classList.remove('d-none');
      }
    });
    content.querySelector('[data-send]').addEventListener('click', event => campaignCommand('send', data.version, event.currentTarget));
    content.querySelector('[data-cancel]').addEventListener('click', event => campaignCommand('cancel', data.version, event.currentTarget));
  }

  async function campaignCommand(action, expectedVersion, button) {
    if (button.disabled) return;
    button.disabled = true;
    const requestId = activeRequest;
    try {
      const result = await campaignApi(action, 'POST', { expectedVersion });
      if (requestId !== activeRequest) return;
      message.textContent = result.message;
      message.classList.remove('d-none');
      await tab('campaign');
    } catch (ex) {
      if (requestId !== activeRequest) return;
      error.textContent = ex.message || 'Não foi possível concluir a ação.';
      error.classList.remove('d-none');
    } finally {
      button.disabled = false;
    }
  }

  function campaignEmpty() {
    content.innerHTML = `<div class="empty-state"><h2>Crie a campanha deste diagnóstico</h2><p>Configure uma mensagem real e use o link público quando o diagnóstico estiver publicado.</p><form data-create-campaign class="valora-form"><label>Nome<input name="name" required maxlength="180" class="form-control"></label><label>Canal<select name="channel" class="form-select"><option value="manual">Compartilhamento manual</option><option value="whatsapp_manual">WhatsApp manual</option><option value="email">E-mail configurado</option></select></label><div data-email-fields hidden><label>Assunto do e-mail<input name="subject" maxlength="180" class="form-control"></label><label>Destinatários<input name="recipients" class="form-control" placeholder="nome@empresa.com; outro@empresa.com"></label><div class="form-check"><input class="form-check-input" type="checkbox" id="campaign-consent" name="hasConsent"><label class="form-check-label" for="campaign-consent">Tenho consentimento registrado para estes destinatários.</label></div><div class="form-check"><input class="form-check-input" type="checkbox" id="campaign-legal" name="hasLegalBasis"><label class="form-check-label" for="campaign-legal">Há base legal documentada para este envio.</label></div><p class="form-text" data-recipient-count>0 destinatários válidos informados.</p></div><label>Mensagem<textarea name="message" required maxlength="10000" rows="5" class="form-control"></textarea></label><p class="form-text" data-channel-help>Este canal cria a campanha e preserva o link para cópia manual.</p><button class="btn btn-primary mt-3">Criar campanha</button></form></div>`;
    const formElement = content.querySelector('form');
    const emailFields = content.querySelector('[data-email-fields]');
    const recipientInput = formElement.elements.recipients;
    const subjectInput = formElement.elements.subject;
    const recipientCount = content.querySelector('[data-recipient-count]');
    const channelHelp = content.querySelector('[data-channel-help]');
    const parseRecipients = () => recipientInput.value.split(/[\s,;]+/).map(x => x.trim().toLowerCase()).filter(Boolean);
    const uniqueRecipients = () => [...new Set(parseRecipients())];
    const updateChannel = () => {
      const email = formElement.elements.channel.value === 'email';
      emailFields.hidden = !email;
      subjectInput.required = email;
      recipientInput.required = email;
      channelHelp.textContent = email ? 'O envio só será preparado se o SMTP do ambiente estiver válido.' : 'Este canal cria a campanha e preserva o link para cópia manual.';
      recipientCount.textContent = `${uniqueRecipients().length} destinatário(s) único(s) informado(s).`;
    };
    formElement.elements.channel.addEventListener('change', updateChannel);
    recipientInput.addEventListener('input', updateChannel);
    updateChannel();
    formElement.addEventListener('submit', async event => {
      event.preventDefault();
      const requestId = activeRequest;
      const form = new FormData(event.target);
      const channel = form.get('channel');
      const recipients = channel === 'email'
        ? uniqueRecipients().map(email => ({ email, hasConsent: form.has('hasConsent'), hasLegalBasis: form.has('hasLegalBasis') }))
        : [];
      if (channel === 'email' && parseRecipients().length !== recipients.length) {
        error.textContent = 'Remova e-mails duplicados antes de criar a campanha.';
        error.classList.remove('d-none');
        return;
      }
      try {
        await campaignApi('', 'POST', { name: form.get('name'), channel, subject: channel === 'email' ? form.get('subject') : null, message: form.get('message'), recipients });
        if (requestId !== activeRequest) return;
        message.textContent = 'Campanha salva. Gere um link para o compartilhamento manual; o preparo de e-mail depende da configuração do ambiente.';
        message.classList.remove('d-none');
        await tab('campaign');
      } catch (ex) {
        if (requestId !== activeRequest) return;
        error.textContent = ex.message || 'Não foi possível criar a campanha.';
        error.classList.remove('d-none');
      }
    });
  }

  async function tab(name) {
    if (!allowedTabs.has(name)) name = 'overview';
    const requestId = ++activeRequest;
    loading.classList.remove('d-none');
    error.classList.add('d-none');
    try {
      if (name === 'campaign') {
        try { const data = await campaignApi(); if (requestId === activeRequest) campaignView(data); } catch (ex) { if (requestId !== activeRequest) return; if (ex.code === 'CAMPAIGN_NOT_FOUND') { if (ex.canManage && !ex.readOnly) campaignEmpty(); else content.innerHTML = '<p>Nenhuma campanha registrada. Solicite a configuração a um usuário autorizado.</p>'; } else throw ex; }
        return;
      }
      if (name === 'actions') {
        if (!overviewState) {
          const overview = await api('overview');
          if (requestId !== activeRequest) return;
          setOverviewHeader(overview);
        }
        const data = await api('execution');
        if (requestId === activeRequest) executionView(data);
        return;
      }
      if (name === 'certificates') { location.href = '/Certificates'; return; }
      if (name === 'exports') { location.href = '/Exports'; return; }
      if (name === 'shares') {
        content.innerHTML = '<div class="empty-state"><h2>Compartilhamentos controlados</h2><p>Gere primeiro um snapshot do Executive Report™. Os links são criados no detalhe do relatório, com expiração e revogação.</p><a class="btn btn-primary" href="/Intelligence/ExecutiveReport">Abrir Executive Report™</a></div>';
        return;
      }
      if (name === 'evolution' && !overviewState) {
        const overview = await api('overview');
        if (requestId !== activeRequest) return;
        setOverviewHeader(overview);
      }
      const data = await api(name);
      if (requestId !== activeRequest) return;
      if (name === 'overview') overview(data);
      else if (name === 'participation') participationView(data);
      else if (name === 'evidence') evidenceView(data);
      else if (name === 'insights') insightsView(data);
      else if (name === 'inferences') content.innerHTML = inferencesView(data);
      else if (name === 'metrics') content.innerHTML = metricsIndicesView('metrics', data);
      else if (name === 'indices') content.innerHTML = metricsIndicesView('indices', data);
      else if (name === 'evolution') {
        const back = `/Diagnostics/${diagnosticId}/Workspace?tab=evolution`;
        const records = Array.isArray(data) ? data : data.items || [];
        content.innerHTML = `<h2>Evolução</h2>${records.length ? `<div class="table-responsive"><table class="valora-table"><thead><tr><th>Ciclo</th><th>Situação</th><th>Último acompanhamento</th><th>Última reavaliação</th><th>Pendência</th><th></th></tr></thead><tbody>${records.map(item => { const cycle = item.data || item; return `<tr><td>${esc(cycle.title || 'Ciclo de evolução')}</td><td>${esc(label(item.status || cycle.status))}</td><td>${esc(cycle.lastMonitoringAt ? date(cycle.lastMonitoringAt, cycle.timeZone) : 'Nenhum registro')}</td><td>${esc(cycle.lastReevaluationAt ? date(cycle.lastReevaluationAt, cycle.timeZone) : 'Nenhum registro')}</td><td>${cycle.referenceReviewRequired ? 'Revisar referência inicial' : cycle.lastReevaluationAt ? 'Nenhuma pendência de medição' : 'Reavaliação ainda não registrada'}</td><td><a href="/Evolution/Cycles/Details/${encodeURIComponent(item.id)}?returnUrl=${encodeURIComponent(back)}">Continuar ciclo</a></td></tr>`; }).join('')}</tbody></table></div>` : '<p>Nenhum ciclo vinculado.</p>'}<a class="valora-button" href="/Evolution/Cycles?diagnosticId=${encodeURIComponent(overviewState.cycle.surveyId)}&returnUrl=${encodeURIComponent(back)}">Novo ciclo</a>`;
      }
      else {
        const contextualAction = name === 'evolution' && overviewState?.cycle
          ? `<div class="valora-card"><h3>Reavaliação vinculada</h3><p>Abra um ciclo Evolution™ a partir deste diagnóstico para selecionar um resultado calculado elegível. Valor manual ficará identificado como informado.</p><a class="valora-button valora-button--primary" href="/Evolution/Cycles?diagnosticId=${encodeURIComponent(overviewState.cycle.surveyId)}&returnUrl=${encodeURIComponent(`/Diagnostics/${diagnosticId}/Workspace?tab=evolution`)}">Criar ciclo vinculado</a></div>`
          : '';
        content.innerHTML = `<h2>${esc(host.querySelector(`[data-tab="${name}"]`).textContent)}</h2>${contextualAction}${cards(data)}`;
      }
    } catch (ex) {
      if (requestId !== activeRequest) return;
      error.textContent = ex.message || 'Não foi possível carregar este módulo. Os dados originais permanecem preservados.';
      error.classList.remove('d-none');
    } finally {
      if (requestId !== activeRequest) return;
      loading.classList.add('d-none');
    }
  }

  host.querySelector('[data-tabs]').addEventListener('click', event => {
    const button = event.target.closest('[data-tab]');
    if (button) tab(button.dataset.tab);
  });
  host.querySelectorAll('[data-command]').forEach(button => button.addEventListener('click', async () => {
    if (button.dataset.command === 'close' && !await window.ValoraUI.confirm('Encerrar este ciclo e bloquear novas respostas?')) return;
    const requestId = activeRequest;
    button.disabled = true;
    try {
      const result = await api(button.dataset.command, 'POST');
      if (requestId !== activeRequest) return;
      message.textContent = result.message;
      message.classList.remove('d-none');
      await tab('overview');
    } catch (ex) {
      if (requestId !== activeRequest) return;
      error.textContent = ex.message || 'Não foi possível concluir a ação.';
      error.classList.remove('d-none');
    } finally {
      button.disabled = false;
    }
  }));

  const initialTab = new URLSearchParams(location.search).get('tab') || 'overview';
  tab(allowedTabs.has(initialTab) ? initialTab : 'overview');
})();
