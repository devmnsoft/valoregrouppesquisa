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
  const allowedTabs = new Set(['overview', 'preparation', 'responses', 'participation', 'evidence', 'metrics', 'indices', 'heatmap', 'radar', 'inferences', 'insights', 'actions', 'evolution', 'benchmark', 'report', 'campaign', 'certificates', 'exports', 'shares']);

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

  async function getPublicationReview(surveyId) {
    try {
      const response = await fetch(`/bff/surveys/${encodeURIComponent(surveyId)}/publication-review`, {
        headers: { Accept: 'application/json' }
      });
      if (!response.ok) return null;
      const payload = await response.json().catch(() => ({}));
      return payload.data ?? payload;
    } catch {
      return null;
    }
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
    if (!items?.length) return empty('Nenhum achado gerado ou em análise para este diagnóstico.');
    const returnUrl = `/Diagnostics/${diagnosticId}/Workspace?tab=insights`;
    content.innerHTML = `<h2>Achados Analíticos & Revisão Humana</h2>
      <p class="form-help">Achados sugeridos por inteligência artificial passam obrigatoriamente por revisão humana com justificativa e preservação de autoria antes de se tornarem planos aprovados no Action Center. Fatos calculados e hipóteses estatísticas são diferenciados explicitamente.</p>
      <div class="row g-3">${items.map(item => {
        const data = item.data || item;
        const status = item.status || data.status || 'proposed';
        const isApproved = status === 'approved';
        const isPending = status === 'proposed' || status === 'pending_review';
        const isConverted = status === 'converted_to_action';
        const isRejected = status === 'rejected';

        const nature = data.nature || (data.aiRunId ? 'sugestao_ia' : item.code === 'inference' ? 'hipotese' : data.calculatedScore != null ? 'fato_calculado' : 'sugestao_ia');
        const natureInfo = {
          fato_calculado: { label: 'Fato Calculado', desc: 'Medição quantitativa apurada a partir das respostas do ciclo', badgeClass: 'badge-nature badge-nature--fact' },
          observacao_informada: { label: 'Observação Informada', desc: 'Relato direto registrado por participante da pesquisa', badgeClass: 'badge-nature badge-nature--hypothesis' },
          hipotese: { label: 'Hipótese Estatística', desc: 'Correlação metodológica inferida pelo modelo estatístico', badgeClass: 'badge-nature badge-nature--hypothesis' },
          sugestao_ia: { label: 'Sugestão de IA™', desc: 'Achado gerado por inteligência com revisão humana mandatória', badgeClass: 'badge-nature badge-nature--ai' },
          decisao_revisada: { label: 'Decisão Revisada', desc: 'Achado analisado e validado humanamente pela liderança', badgeClass: 'badge-nature badge-nature--reviewed' }
        }[nature] || { label: 'Achado Metodológico', desc: 'Registro analítico do ciclo', badgeClass: 'badge-nature badge-nature--hypothesis' };

        const planLink = `/ActionCenter/Plans/Create?originType=ai_insight&originId=${encodeURIComponent(item.id)}&returnUrl=${encodeURIComponent(returnUrl)}`;
        const reviewLink = `/Insights/Details/${encodeURIComponent(item.id)}?returnUrl=${encodeURIComponent(returnUrl)}`;
        const linkedPlanUrl = data.linkedPlanId ? `/ActionCenter/Plans/Details/${encodeURIComponent(data.linkedPlanId)}?returnUrl=${encodeURIComponent(returnUrl)}` : null;

        const scoreDisplay = data.calculatedScore != null ? `${esc(data.calculatedScore)} pts` : data.resultScore != null ? `${esc(data.resultScore)} pts` : '<span class="text-muted">Não se aplica / sem cálculo</span>';
        const evidenceDisplay = data.evidenceSummary || data.evidence || (data.evidenceCount ? `${data.evidenceCount} evidências convergentes` : 'Evidências convergentes da amostra coletada');

        return `<article class="col-12 col-lg-6">
          <div class="insight-card-item h-100 d-flex flex-column justify-content-between" data-nature="${esc(nature)}">
            <div>
              <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
                <strong class="fs-5">${esc(data.title || item.code || 'Achado sugerido')}</strong>
                <span class="${natureInfo.badgeClass}">${esc(natureInfo.label)}</span>
              </div>
              <p class="text-muted small mb-3">${esc(data.description || data.summary || data.strategicTruth || 'Sem descrição detalhada.')}</p>
              <dl class="small mb-2">
                <div><dt>Dimensão ou conceito</dt><dd><strong>${esc(data.dimension || data.relatedDimension || data.conceptCode || 'Geral')}</strong></dd></div>
                <div><dt>Evidência de origem</dt><dd><em>${esc(evidenceDisplay)}</em></dd></div>
                <div><dt>Resultado calculado</dt><dd>${scoreDisplay}</dd></div>
                <div><dt>Limitação</dt><dd class="text-warning">${esc(data.limitation || 'Válido sob a amostragem coletada no ciclo.')}</dd></div>
                <div><dt>Data do achado</dt><dd>${esc(date(item.createdAt || data.createdAt))}</dd></div>
                <div><dt>Situação de revisão</dt><dd><span class="valora-status">${esc(label(status))}</span></dd></div>
              </dl>
            </div>
            <div class="d-flex gap-2 flex-wrap mt-3 pt-3 border-top">
              <a class="btn btn-sm btn-outline-primary" href="${reviewLink}">Revisar achado (aprovar / rejeitar)</a>
              ${isApproved ? `<a class="btn btn-sm btn-primary" href="${planLink}">Criar plano no Action Center</a>` : ''}
              ${isConverted && linkedPlanUrl ? `<a class="btn btn-sm btn-outline-success" href="${linkedPlanUrl}">Ver plano vinculado</a>` : isConverted ? `<span class="badge bg-success align-self-center">Vinculado a plano</span>` : ''}
              ${isPending ? `<small class="text-muted align-self-center">Aprovação exigida antes de criar plano.</small>` : ''}
              ${isRejected ? `<small class="text-danger align-self-center">Rejeitado na revisão humana.</small>` : ''}
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

  function computeNextAction(data) {
    const cycle = data.cycle;
    const isDraft = cycle.status === 'draft';
    const isClosed = cycle.status === 'closed';
    const responseCount = Number(cycle.responseCount || 0);
    const evidenceCount = Number(cycle.evidenceCount || 0);
    const lastJob = data.lastJob;
    const isProcessing = cycle.processingStatus === 'processing' || lastJob?.status === 'running' || lastJob?.status === 'pending';
    const hasJobFailed = lastJob?.status === 'failed';
    const hasPendingReviewInsights = data.mainInsights?.some(x => (x.status === 'proposed' || x.status === 'pending_review' || x.data?.reviewStatus === 'pending_review'));
    const hasActions = (data.recommendedActions?.length || 0) > 0;

    if (isDraft) {
      return {
        stageName: '1. Preparação & Rascunho',
        whatHappened: 'Diagnóstico criado no sistema com formulário e parâmetros vinculados.',
        whatIsMissing: 'Revisar checklist pré-publicação, resolver impedimentos técnicos e ativar a coleta.',
        whoMustAct: 'Gestor do Diagnóstico / Administrador com permissão de publicação.',
        operationLabel: 'Revisar preparação & publicar',
        operationTarget: 'preparation',
        buttonClass: 'btn-primary'
      };
    }
    if (isClosed) {
      return {
        stageName: '6. Ciclo Encerrado & Evolução',
        whatHappened: 'A coleta de respostas foi finalizada e o ciclo diagnóstico está formalmente encerrado.',
        whatIsMissing: 'Acompanhar a evolução histórica ou abrir novo ciclo comparativo no Evolution™.',
        whoMustAct: 'Comitê de Governança / Consultor de Evolução Organizacional.',
        operationLabel: 'Abrir reavaliação no Evolution™',
        operationTarget: 'evolution',
        buttonClass: 'btn-outline-primary'
      };
    }
    if (responseCount === 0) {
      return {
        stageName: '2. Coleta em Campo (Aguardando Respostas)',
        whatHappened: 'Diagnóstico publicado com sucesso. Link seguro disponível para distribuição.',
        whatIsMissing: 'Coletar as primeiras respostas de colaboradores ou respondentes convidados.',
        whoMustAct: 'Participantes convidados / Gestor de campanha para envio ou compartilhamento.',
        operationLabel: 'Compartilhar link de participação',
        operationTarget: 'campaign',
        buttonClass: 'btn-primary'
      };
    }
    if (isProcessing) {
      return {
        stageName: '3. Processamento Analítico em Andamento',
        whatHappened: `${responseCount} resposta(s) recebida(s). Job de inteligência está em execução no servidor.`,
        whatIsMissing: 'Conclusão da apuração de evidências e indicadores pelo pipeline analítico.',
        whoMustAct: 'Serviço de Inteligência Organizacional em segundo plano.',
        operationLabel: 'Atualizar status do processamento',
        operationTarget: 'overview',
        buttonClass: 'btn-outline-secondary'
      };
    }
    if (evidenceCount === 0 || hasJobFailed) {
      return {
        stageName: '3. Respostas Recebidas (Pronto para Processar)',
        whatHappened: `${responseCount} resposta(s) persistida(s) no banco de dados com integridade comprovada.`,
        whatIsMissing: 'Executar o processamento de inteligência para consolidar evidências, índices e inferências.',
        whoMustAct: 'Analista de Inteligência Organizacional / Gestor do Diagnóstico.',
        operationLabel: 'Processar Inteligência Valora™',
        operationCommand: 'process',
        buttonClass: 'btn-primary'
      };
    }
    if (hasPendingReviewInsights) {
      return {
        stageName: '4. Evidências Apuradas (Revisão de Achados)',
        whatHappened: `${evidenceCount} evidências metodológicas extraídas das respostas reais dos participantes.`,
        whatIsMissing: 'Revisão humana dos achados sugeridos antes da conversão em planos de ação.',
        whoMustAct: 'Liderança / Gestor responsável pela validação editorial.',
        operationLabel: 'Revisar achados analíticos',
        operationTarget: 'insights',
        buttonClass: 'btn-primary'
      };
    }
    if (hasActions) {
      return {
        stageName: '5. Planos de Ação & Execução',
        whatHappened: 'Achados validados pela liderança com planos e atividades em andamento no Action Center.',
        whatIsMissing: 'Acompanhar a evolução das atividades, evidências de conclusão e marcos de entrega.',
        whoMustAct: 'Responsáveis pelas atividades no Action Center.',
        operationLabel: 'Acompanhar execução no Action™',
        operationTarget: 'actions',
        buttonClass: 'btn-primary'
      };
    }
    return {
      stageName: '4. Resultados Consolidados',
      whatHappened: `${responseCount} resposta(s) e ${evidenceCount} evidência(s) apuradas com sucesso.`,
      whatIsMissing: 'Explorar os resultados nos painéis metodológicos ou gerar o relatório executivo.',
      whoMustAct: 'Gestor do Diagnóstico / Liderança Executiva.',
      operationLabel: 'Criar plano de ação vinculado',
      operationUrl: diagnosticPlanUrl(cycle),
      buttonClass: 'btn-primary'
    };
  }

  function preparationView(data, review) {
    const cycle = data.cycle;
    const isDraft = cycle.status === 'draft';
    const canPublish = review ? review.canPublish : isDraft;
    const blockers = review?.blockers || [];
    const warnings = review?.warnings || [];
    const surveyId = cycle.surveyId || diagnosticId;

    content.innerHTML = `<h2>Preparação Consolidada do Diagnóstico</h2>
      <p class="form-help">Parâmetros operacionais e metodológicos organizados para conduzir o ciclo de escuta com segurança.</p>
      <div class="row g-3 mb-4">
        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <strong>Objetivo do Diagnóstico</strong>
            <p class="small mt-2 mb-0">${esc(cycle.description || cycle.title || 'Diagnóstico de maturidade organizacional')}</p>
          </div>
        </article>
        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <strong>Responsável Designado</strong>
            <p class="small mt-2 mb-0">${esc(cycle.responsibleName || 'Gestor do Diagnóstico')}</p>
          </div>
        </article>
        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <strong>Público ou Recorte</strong>
            <p class="small mt-2 mb-0">${esc(cycle.targetAudience || 'Participantes elegíveis da organização')}</p>
          </div>
        </article>
        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <strong>Questionário & Versão</strong>
            <p class="small mt-2 mb-0">${esc(cycle.formName || 'Formulário do Diagnóstico')} <span class="badge bg-secondary">v${esc(cycle.formVersionNumber || '1')}</span></p>
          </div>
        </article>
        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <strong>Período de Coleta</strong>
            <p class="small mt-2 mb-0">${cycle.startsAt ? date(cycle.startsAt) : 'Início imediato'} até ${cycle.expiresAt ? date(cycle.expiresAt) : 'Sem expiração definida'}</p>
          </div>
        </article>
        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <strong>Canal de Distribuição</strong>
            <p class="small mt-2 mb-0">${cycle.distributionChannel === 'email' ? 'Disparo oficial por E-mail' : cycle.distributionChannel === 'whatsapp_manual' ? 'WhatsApp manual' : 'Compartilhamento manual de link seguro'}</p>
          </div>
        </article>
      </div>

      <section class="valora-card mb-4">
        <div class="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
          <div>
            <h3 class="mb-0">Revisão Pré-Publicação</h3>
            <small class="text-muted">Conferência dos requisitos técnicos, avisos analíticos e limites de contratação.</small>
          </div>
          <span class="badge ${canPublish ? 'bg-success' : 'bg-danger'} fs-6">${canPublish ? 'Apto para publicação' : 'Impedimentos pendentes'}</span>
        </div>

        ${blockers.length === 0 ? `
          <div class="alert alert-success d-flex align-items-center gap-2 mb-3">
            <span>✅</span> <div><strong>Requisitos técnicos atendidos:</strong> Questionário publicado vinculado, perguntas ativas disponíveis e limite comercial do plano verificado com sucesso.</div>
          </div>
        ` : `
          <div class="alert alert-danger mb-3">
            <strong>Impedimentos Técnicos (${blockers.length}):</strong> Estas pendências devem ser corrigidas antes de disponibilizar o link público.
          </div>
          <div class="table-responsive mb-3">
            <table class="valora-table">
              <thead><tr><th>Problema</th><th>Impacto</th><th>Ação Recomendada</th><th></th></tr></thead>
              <tbody>
                ${blockers.map(b => `<tr>
                  <td><strong>${esc(b.problem)}</strong></td>
                  <td>${esc(b.impact)}</td>
                  <td>${esc(b.action)}</td>
                  <td>${b.targetUrl ? `<a class="btn btn-sm btn-outline-primary" href="${esc(b.targetUrl)}">Corrigir</a>` : ''}</td>
                </tr>`).join('')}
              </tbody>
            </table>
          </div>
        `}

        ${warnings.length > 0 ? `
          <div class="alert alert-warning mb-3">
            <strong>Avisos Metodológicos (${warnings.length}):</strong> Não bloqueiam a coleta pública, mas definem o tipo de resultado que será gerado.
          </div>
          <ul class="list-group mb-3">
            ${warnings.map(w => `<li class="list-group-item list-group-item-warning">
              <strong>${esc(w.problem)}</strong> — ${esc(w.impact)}
              <div class="small mt-1 text-muted"><strong>Ação opcional:</strong> ${esc(w.action)}</div>
            </li>`).join('')}
          </ul>
        ` : ''}

        <div class="d-flex gap-2 flex-wrap pt-3 border-top">
          ${isDraft ? `
            <button class="btn btn-primary" data-publish-survey ${canPublish ? '' : 'disabled'}>Publicar Diagnóstico & Ativar Coleta</button>
            <a class="btn btn-outline-secondary" href="/Surveys/${encodeURIComponent(surveyId)}/Edit">Editar configurações</a>
          ` : `
            <button class="btn btn-outline-primary" onclick="document.querySelector('[data-tab=campaign]')?.click()">Acessar Campanha & Links</button>
            <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=participation]')?.click()">Ver Participação Real</button>
          `}
        </div>
      </section>`;

    const publishBtn = content.querySelector('[data-publish-survey]');
    if (publishBtn) {
      publishBtn.addEventListener('click', async () => {
        if (!await window.ValoraUI.confirm('Publicar este diagnóstico e ativar o link de coleta pública?')) return;
        publishBtn.disabled = true;
        const requestId = activeRequest;
        try {
          const res = await fetch(`/bff/surveys/${encodeURIComponent(surveyId)}/status`, {
            method: 'PATCH',
            headers: {
              'Content-Type': 'application/json',
              'X-CSRF-TOKEN': host.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ status: 'active' })
          });
          const payload = await res.json().catch(() => ({}));
          if (!res.ok) throw payload;
          if (requestId !== activeRequest) return;
          message.textContent = 'Diagnóstico publicado com sucesso! A coleta está ativa.';
          message.classList.remove('d-none');
          await tab('overview');
        } catch (ex) {
          if (requestId !== activeRequest) return;
          error.textContent = ex.message || 'Falha ao publicar diagnóstico.';
          error.classList.remove('d-none');
        } finally {
          publishBtn.disabled = false;
        }
      });
    }
  }

  function overview(data) {
    setOverviewHeader(data);
    const cycle = data.cycle;
    const isDraft = cycle.status === 'draft';
    const hasResponses = Number(cycle.responseCount || 0) > 0;
    const hasEvidence = Number(cycle.evidenceCount || 0) > 0;
    const nextAction = computeNextAction(data);

    content.innerHTML = `<h2>Visão Geral da Jornada Operacional</h2>
      <div class="row g-3 mb-3">
        <article class="col-12 col-md-6">
          <div class="valora-card h-100">
            <h3>Em que etapa está o diagnóstico?</h3>
            <p><span class="badge bg-primary fs-6">${esc(nextAction.stageName)}</span></p>
            <p>${isDraft ? 'O diagnóstico está em preparação/rascunho. Revise o questionário e publique para iniciar a coleta.' : hasResponses ? 'Coleta em andamento com participações registradas no banco de dados.' : 'Diagnóstico ativo aguardando as primeiras submissões dos participantes.'}</p>
          </div>
        </article>
        <article class="col-12 col-md-6">
          <div class="valora-card h-100">
            <h3>Situação das Etapas</h3>
            <ul class="list-unstyled mb-0 small">
              <li>${isDraft ? '⏳' : '✅'} <strong>1. Preparação & Publicação:</strong> ${isDraft ? 'Pendente de ativação' : 'Publicado e ativo'}</li>
              <li>${hasResponses ? '✅' : '⏳'} <strong>2. Coleta de Respostas:</strong> ${hasResponses ? `${cycle.responseCount} participações gravadas` : 'Nenhuma resposta recebida ainda'}</li>
              <li>${hasEvidence ? '✅' : '⏳'} <strong>3. Evidências & Resultados:</strong> ${hasEvidence ? `${cycle.evidenceCount} evidências metodológicas` : 'Aguardando processamento'}</li>
              <li>${data.recommendedActions?.length ? '✅' : '⏳'} <strong>4. Planos & Execução:</strong> Action Center integrado</li>
            </ul>
          </div>
        </article>
      </div>

      <section class="valora-card mb-3">
        <div class="d-flex justify-content-between align-items-center mb-2 flex-wrap gap-2">
          <h3 class="mb-0">Próxima Ação Autorizada</h3>
          <span class="valora-status">${esc(label(cycle.status))}</span>
        </div>
        <div class="row g-2 mb-3 small">
          <div class="col-12 col-md-6"><strong>O que já aconteceu:</strong> ${esc(nextAction.whatHappened)}</div>
          <div class="col-12 col-md-6"><strong>O que está faltando:</strong> ${esc(nextAction.whatIsMissing)}</div>
          <div class="col-12"><strong>Quem precisa agir:</strong> <span class="badge bg-light text-dark border">${esc(nextAction.whoMustAct)}</span></div>
        </div>
        <div class="d-flex gap-2 flex-wrap">
          ${nextAction.operationUrl ? `<a class="btn ${nextAction.buttonClass}" href="${esc(nextAction.operationUrl)}">${esc(nextAction.operationLabel)}</a>` : ''}
          ${nextAction.operationCommand ? `<button class="btn ${nextAction.buttonClass}" onclick="document.querySelector('[data-command=${nextAction.operationCommand}]')?.click()">${esc(nextAction.operationLabel)}</button>` : ''}
          ${nextAction.operationTarget ? `<button class="btn ${nextAction.buttonClass}" onclick="document.querySelector('[data-tab=${nextAction.operationTarget}]')?.click()">${esc(nextAction.operationLabel)}</button>` : ''}
          <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=preparation]')?.click()">Revisar Preparação</button>
          <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=campaign]')?.click()">Campanha & Links</button>
          <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=participation]')?.click()">Ver Participação Real</button>
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

    let statusDescription = 'Campanha salva';
    let statusBadgeClass = 'bg-secondary';
    if (data.status === 'scheduled') {
      statusDescription = 'Envio solicitado';
      statusBadgeClass = 'bg-info text-dark';
    } else if (data.status === 'queued') {
      statusDescription = 'Mensagem na fila';
      statusBadgeClass = 'bg-warning text-dark';
    } else if (data.status === 'sent' || data.status === 'active') {
      statusDescription = manual ? 'Compartilhamento manual ativo' : 'Envio registrado';
      statusBadgeClass = 'bg-success';
    } else if (data.status === 'failed') {
      statusDescription = 'Falha no envio';
      statusBadgeClass = 'bg-danger';
    } else if (data.status === 'draft') {
      statusDescription = 'Campanha salva (rascunho)';
      statusBadgeClass = 'bg-secondary';
    } else if (data.status === 'cancelled') {
      statusDescription = 'Campanha cancelada';
      statusBadgeClass = 'bg-dark';
    } else if (data.status === 'closed') {
      statusDescription = 'Campanha encerrada';
      statusBadgeClass = 'bg-dark';
    }

    content.innerHTML = `<div class="d-flex justify-content-between gap-3 flex-wrap align-items-center mb-3">
      <div>
        <h2 class="mb-1">Campanha & Distribuição</h2>
        <p class="text-muted mb-0">Canal: <strong>${esc(channelName)}</strong></p>
      </div>
      <div>
        <span class="badge ${statusBadgeClass} fs-6">${esc(statusDescription)}</span>
      </div>
    </div>
    <div class="metric-grid">
      <article class="metric-card"><span>Destinatários por e-mail</span><strong>${data.recipientCount}</strong></article>
      <article class="metric-card"><span>Mensagens na fila</span><strong>${data.queuedCount}</strong></article>
      <article class="metric-card"><span>Envios registrados</span><strong>${data.sentCount}</strong></article>
      <article class="metric-card"><span>Falhas</span><strong>${data.failedCount}</strong></article>
    </div>
    <p class="form-help mt-2">Mensagens na fila não comprovam entrega. Compartilhamento manual e cópia de link não são contabilizados como envio por e-mail.</p>

    <div class="valora-card mt-3">
      <h3>${esc(data.name)}</h3>
      <p class="mb-3">${esc(data.message)}</p>

      <div class="mb-3">
        <label class="form-label" for="campaign-public-link"><strong>Link de participação pública</strong></label>
        <div class="input-group">
          <input id="campaign-public-link" class="form-control" readonly value="${esc(campaignLink || '')}" placeholder="Gere ou visualize o link de coleta pública">
          <button class="btn btn-outline-primary" data-generate-link ${data.canDistribute && !data.readOnly ? '' : 'disabled'}>Gerar novo link</button>
          <button class="btn btn-primary" data-copy-link ${campaignLink ? '' : 'disabled'}>Copiar link</button>
          <button class="btn btn-outline-secondary" data-copy-message ${campaignLink ? '' : 'disabled'}>Copiar convite com link</button>
        </div>
        <div class="form-help mt-2">
          <strong>Comportamento dos links:</strong> Ao gerar um novo link de participação, um token adicional seguro é registrado mantendo os links anteriores ativos (a menos que revogados individualmente ou o diagnóstico expire). O token bruto é retornado apenas na criação e não é exposto em logs.
        </div>
      </div>

      <div class="d-flex gap-2 mt-4 pt-3 border-top flex-wrap">
        <button class="btn btn-primary" data-send ${canSend ? '' : 'disabled'}>${manual ? 'Confirmar compartilhamento manual' : 'Disparar fila de e-mail'}</button>
        <button class="btn btn-outline-danger" data-cancel ${canCancel ? '' : 'disabled'}>Cancelar envios pendentes</button>
      </div>
    </div>`;

    const showCopyResult = async text => {
      if (!text) return;
      try {
        if (navigator.clipboard && window.isSecureContext) {
          await navigator.clipboard.writeText(text);
          message.textContent = 'Link copiado com sucesso para a área de transferência. Nenhum envio ou entrega foi registrado por esta cópia.';
          message.classList.remove('d-none');
        } else {
          throw new Error('Clipboard API indisponível');
        }
      } catch {
        const linkInput = document.getElementById('campaign-public-link');
        if (linkInput) {
          linkInput.focus();
          linkInput.select();
          try {
            document.execCommand('copy');
            message.textContent = 'Link selecionado e copiado via atalho do navegador.';
            message.classList.remove('d-none');
            return;
          } catch {
            // fallback
          }
        }
        error.textContent = 'Não foi possível copiar automaticamente. O link foi selecionado no campo para cópia manual (Ctrl+C / Cmd+C).';
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
        message.textContent = 'Link gerado com sucesso. Revise o convite e compartilhe pelo canal escolhido.';
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

    host.querySelectorAll('[data-tab]').forEach(btn => {
      const isActive = btn.dataset.tab === name;
      btn.classList.toggle('btn-primary', isActive);
      btn.classList.toggle('active', isActive);
      btn.classList.toggle('btn-outline-secondary', !isActive);
      btn.setAttribute('aria-selected', isActive ? 'true' : 'false');
    });

    try {
      if (name === 'preparation') {
        if (!overviewState) {
          const overview = await api('overview');
          if (requestId !== activeRequest) return;
          setOverviewHeader(overview);
        }
        const surveyId = overviewState?.cycle?.surveyId || diagnosticId;
        const review = await getPublicationReview(surveyId);
        if (requestId !== activeRequest) return;
        preparationView(overviewState?.cycle || {}, review);
        return;
      }
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
