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
        ...(body ? { 'Content-Type': 'application/json' } : {}),
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
    const respRecebidas = Number(data.respostasRecebidas || 0);
    const respValidas = Number(data.respostasValidas || 0);
    const respPontuadas = Number(data.respostasPontuadas || 0);
    const pendencias = Number(data.pendenciasProcessamento || 0);
    const fila = Number(data.mensagensFila || 0);
    const envios = Number(data.enviosRegistrados || 0);
    const falhas = Number(data.falhas || 0);
    const taxaAdesao = destEmail > 0 ? `${Math.round(respValidas / destEmail * 100)}%` : 'Não aplicável (sem lista de e-mails)';
    const cycle = overviewState?.cycle || {};
    const surveyId = cycle.surveyId || diagnosticId;

    content.innerHTML = `<h2>Participação & Coleta em Campo</h2>
      <p class="form-help">Métricas em tempo real persistidas com isolamento estrito para este diagnóstico. Nenhuma estatística ou percentual é estimado artificialmente.</p>

      <div class="row g-3 mb-4">
        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Respostas Recebidas</span>
            <div class="display-6 my-2"><strong>${esc(respRecebidas)}</strong> <small class="fs-6 text-muted">respostas</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Submissões gravadas</dd></div>
              <div><dt>Período</dt><dd>Ciclo diagnóstico atual</dd></div>
              <div><dt>Limitação</dt><dd>Inclui questionários completos e em andamento</dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Respostas Válidas</span>
            <div class="display-6 my-2"><strong>${esc(respValidas)}</strong> <small class="fs-6 text-muted">concluídas</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Questionários 100% íntegros</dd></div>
              <div><dt>Período</dt><dd>Status 'completed' sem exclusão</dd></div>
              <div><dt>Limitação</dt><dd>Elegíveis para apuração analítica</dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Respostas Pontuadas</span>
            <div class="display-6 my-2"><strong>${esc(respPontuadas)}</strong> <small class="fs-6 text-muted">calculadas</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Respostas com score consolidado</dd></div>
              <div><dt>Período</dt><dd>Apuradas em result_scores</dd></div>
              <div><dt>Limitação</dt><dd>Base para índices e inferências</dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Taxa de Adesão</span>
            <div class="display-6 my-2"><strong>${esc(taxaAdesao)}</strong></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>% de conclusão sobre lista</dd></div>
              <div><dt>Período</dt><dd>Acumulado da campanha</dd></div>
              <div><dt>Limitação</dt><dd>${destEmail > 0 ? 'Calculada estritamente sobre destinatários cadastrados' : 'Sem denominador fixo em compartilhamento manual'}</dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Destinatários Registrados</span>
            <div class="display-6 my-2"><strong>${esc(destEmail)}</strong> <small class="fs-6 text-muted">contatos</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Destinatários únicos</dd></div>
              <div><dt>Período</dt><dd>Campanhas e convites deste ciclo</dd></div>
              <div><dt>Ação</dt><dd><a href="#tab=campaign" onclick="document.querySelector('[data-tab=campaign]')?.click()">Ver lista na Campanha</a></dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Mensagens na Fila</span>
            <div class="display-6 my-2"><strong>${esc(fila)}</strong> <small class="fs-6 text-muted">mensagens</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Jobs em fila de despacho</dd></div>
              <div><dt>Status</dt><dd>Aguardando worker de e-mail</dd></div>
              <div><dt>Limitação</dt><dd>Disparos deste diagnóstico</dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Envios Registrados</span>
            <div class="display-6 my-2"><strong>${esc(envios)}</strong> <small class="fs-6 text-muted">disparos</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Disparos entregues / enviados</dd></div>
              <div><dt>Status</dt><dd>Confirmados pelo provedor</dd></div>
              <div><dt>Rastreio</dt><dd>Vinculados a este diagnóstico</dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Falhas de Entrega</span>
            <div class="display-6 my-2 ${falhas > 0 ? 'text-danger' : ''}"><strong>${esc(falhas)}</strong> <small class="fs-6 text-muted">falhas</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Erros ou devoluções (bounces)</dd></div>
              <div><dt>Status</dt><dd>${falhas > 0 ? 'Requer atenção operacional' : 'Nenhuma falha registrada'}</dd></div>
              <div><dt>Limitação</dt><dd>E-mails com falha de entrega</dd></div>
            </dl>
          </div>
        </article>

        <article class="col-12 col-md-6 col-lg-4">
          <div class="metric-card h-100">
            <span class="text-muted small">Pendências Analíticas</span>
            <div class="display-6 my-2"><strong>${esc(pendencias)}</strong> <small class="fs-6 text-muted">jobs</small></div>
            <dl class="small mb-0">
              <div><dt>Unidade</dt><dd>Tarefas de processamento</dd></div>
              <div><dt>Status</dt><dd>${pendencias > 0 ? 'Processamento ativo' : 'Pipeline atualizado'}</dd></div>
              <div><dt>Ação</dt><dd><button class="btn btn-sm btn-link p-0 text-decoration-none" onclick="document.querySelector('[data-command=process]')?.click()">Disparar recálculo</button></dd></div>
            </dl>
          </div>
        </article>
      </div>

      ${falhas > 0 ? `
        <div class="alert alert-warning d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
          <div>
            <strong>⚠️ Atenção: ${falhas} falha(s) de envio registradas.</strong> Mensagens de convite não foram entregues devido a erro de SMTP ou endereço rejeitado.
          </div>
          <a class="btn btn-sm btn-outline-dark" href="/Communications?surveyId=${encodeURIComponent(surveyId)}">Ver log de erros detalhado</a>
        </div>
      ` : ''}

      ${pendencias > 0 ? `
        <div class="alert alert-info d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
          <div>
            <strong>⚙️ Tarefas de inteligência em processamento (${pendencias} pendente(s)):</strong> Evidências e índices estão sendo atualizados no servidor.
          </div>
          <button class="btn btn-sm btn-primary" onclick="document.querySelector('[data-command=process]')?.click()">Recalcular agora</button>
        </div>
      ` : ''}

      <section class="valora-card mb-4">
        <h3>Navegação Rápida & Listas de Apoio</h3>
        <p class="small text-muted mb-3">Acesso direto aos registros canônicos vinculados exclusivamente a este diagnóstico:</p>
        <div class="d-flex gap-2 flex-wrap">
          <button class="btn btn-outline-primary" onclick="document.querySelector('[data-tab=campaign]')?.click()">📋 Destinatários da Campanha</button>
          <a class="btn btn-outline-primary" href="/Surveys/${encodeURIComponent(surveyId)}/Links">🔗 Links Públicos e Convites Diretos</a>
          <button class="btn btn-outline-primary" onclick="document.querySelector('[data-tab=responses]')?.click()">📥 Respostas Individuais Recebidas</button>
          <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=overview]')?.click()">⚙️ Acompanhar Pipeline de Inteligência</button>
        </div>
      </section>

      <section class="valora-card mb-4">
        <h3>Origens Auditadas dos Contadores</h3>
        <div class="table-responsive"><table class="valora-table"><thead><tr><th>Contador</th><th>Valor</th><th>Fonte Canônica</th></tr></thead><tbody>
          <tr><td><strong>Respostas recebidas</strong></td><td>${esc(respRecebidas)}</td><td>${esc(origens.respostasRecebidas || 'Tabela responses (is_deleted=false)')}</td></tr>
          <tr><td><strong>Respostas válidas</strong></td><td>${esc(respValidas)}</td><td>${esc(origens.respostasValidas || 'Tabela responses com status=completed')}</td></tr>
          <tr><td><strong>Respostas pontuadas</strong></td><td>${esc(respPontuadas)}</td><td>${esc(origens.respostasPontuadas || 'Tabela result_scores')}</td></tr>
          <tr><td><strong>Pendências de processamento</strong></td><td>${esc(pendencias)}</td><td>${esc(origens.pendenciasProcessamento || 'Tabela intelligence_processing_jobs')}</td></tr>
          <tr><td><strong>Destinatários por e-mail</strong></td><td>${esc(destEmail)}</td><td>${esc(origens.destinatariosEmail || 'Tabela survey_invites')}</td></tr>
          <tr><td><strong>Mensagens na fila</strong></td><td>${esc(fila)}</td><td>${esc(origens.mensagensFila || 'Tabela communications pendentes')}</td></tr>
          <tr><td><strong>Envios registrados</strong></td><td>${esc(envios)}</td><td>${esc(origens.enviosRegistrados || 'Tabela communications enviadas')}</td></tr>
          <tr><td><strong>Falhas</strong></td><td>${esc(falhas)}</td><td>${esc(origens.falhas || 'Tabela communications com falha')}</td></tr>
        </tbody></table></div>
      </section>

      <div class="valora-card">
        <h3>Limitações Metodológicas da Coleta</h3>
        <p class="small text-muted mb-0">
          ${respValidas > 0 ? `
            <strong>Representatividade da amostra:</strong> Os escores e indicadores deste ciclo refletem as respostas de ${respValidas} participante(s) concluinte(s). Em pesquisas organizacionais de maturidade, a amostragem é por adesão (não probabilística); as conclusões retratam a percepção dos participantes do ciclo sem extrapolação para públicos que não responderam.
          ` : `
            <strong>Aguardando respostas:</strong> Nenhuma resposta foi concluída até o momento. Indicadores e escores analíticos serão liberados automaticamente quando os primeiros questionários forem concluídos.
          `}
        </p>
      </div>`;
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

        const originType = data.originType || (data.aiRunId ? 'valora_ai_insight' : 'insight');
        const isAiInsight = originType === 'valora_ai_insight';

        const nature = data.nature || (isAiInsight ? 'sugestao_ia' : item.code === 'inference' ? 'hipotese' : data.calculatedScore != null ? 'fato_calculado' : 'sugestao_ia');
        const natureInfo = {
          fato_calculado: { label: 'Fato Calculado', desc: 'Medição quantitativa apurada a partir das respostas do ciclo', badgeClass: 'badge-nature badge-nature--fact' },
          observacao_informada: { label: 'Observação Informada', desc: 'Relato direto registrado por participante da pesquisa', badgeClass: 'badge-nature badge-nature--hypothesis' },
          hipotese: { label: 'Hipótese Estatística', desc: 'Correlação metodológica inferida pelo modelo estatístico', badgeClass: 'badge-nature badge-nature--hypothesis' },
          sugestao_ia: { label: 'Sugestão de IA™', desc: 'Achado gerado por inteligência com revisão humana mandatória', badgeClass: 'badge-nature badge-nature--ai' },
          decisao_revisada: { label: 'Decisão Revisada', desc: 'Achado analisado e validado humanamente pela liderança', badgeClass: 'badge-nature badge-nature--reviewed' }
        }[nature] || { label: 'Achado Metodológico', desc: 'Registro analítico do ciclo', badgeClass: 'badge-nature badge-nature--hypothesis' };

        const planLink = `/ActionCenter/Plans/Create?originType=${isAiInsight ? 'ai_insight' : 'diagnostic'}&originId=${encodeURIComponent(isAiInsight ? item.id : (overviewState?.cycle?.surveyId || diagnosticId))}&returnUrl=${encodeURIComponent(returnUrl)}`;
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
                <div><dt>Origem técnica</dt><dd><span class="badge bg-light text-dark border">${isAiInsight ? 'Fila Editorial (valora_ai_insight)' : 'Pipeline Analítico (insight)'}</span></dd></div>
                <div><dt>Evidência de origem</dt><dd><em>${esc(evidenceDisplay)}</em></dd></div>
                <div><dt>Resultado calculado</dt><dd>${scoreDisplay}</dd></div>
                <div><dt>Limitação</dt><dd class="text-warning">${esc(data.limitation || 'Válido sob a amostragem coletada no ciclo.')}</dd></div>
                <div><dt>Data do achado</dt><dd>${esc(date(item.createdAt || data.createdAt))}</dd></div>
                <div><dt>Situação de revisão</dt><dd><span class="valora-status">${esc(label(status))}</span></dd></div>
              </dl>
            </div>
            <div class="d-flex gap-2 flex-wrap mt-3 pt-3 border-top">
              ${isAiInsight ? `<a class="btn btn-sm btn-outline-primary" href="${reviewLink}">Revisar achado (aprovar / rejeitar)</a>` : `<button class="btn btn-sm btn-outline-secondary" onclick="document.querySelector('[data-tab=evidence]')?.click()">Ver evidências de apoio</button>`}
              ${isApproved || !isAiInsight ? `<a class="btn btn-sm btn-primary" href="${planLink}">Criar plano no Action Center</a>` : ''}
              ${isConverted && linkedPlanUrl ? `<a class="btn btn-sm btn-outline-success" href="${linkedPlanUrl}">Ver plano vinculado</a>` : isConverted ? `<span class="badge bg-success align-self-center">Vinculado a plano</span>` : ''}
              ${isPending && isAiInsight ? `<small class="text-muted align-self-center">Aprovação exigida antes de criar plano.</small>` : ''}
              ${isRejected && isAiInsight ? `<small class="text-danger align-self-center">Rejeitado na revisão humana.</small>` : ''}
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
        const displayVal = hasValue ? esc(data.value ?? data.score ?? data.normalizedValue) : '<span class="text-muted">Aguardando dados (sem cálculo fictício)</span>';
        const unit = data.unit || (hasValue ? 'pts' : '');
        return `<article class="col-12 col-lg-6">
          <div class="metric-card h-100">
            <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
              <strong>${esc(data.name || data.title || item.code || 'Indicador')}</strong>
              <span class="valora-status">${esc(label(item.status || data.status || 'Ativo'))}</span>
            </div>
            <div class="display-6 my-2">${displayVal} <small class="fs-6 text-muted">${esc(unit)}</small></div>
            <dl class="small mb-0">
              <div><dt>O que mede</dt><dd>${esc(data.description || data.whatItMeasures || 'Medição metodológica do ciclo.')}</dd></div>
              <div><dt>Fonte</dt><dd>${esc(data.source || 'Respostas apuradas no banco de dados')}</dd></div>
              <div><dt>Período / Recorte</dt><dd>${esc(data.period || 'Ciclo vigente sem exclusão')}</dd></div>
              <div><dt>Limitações</dt><dd class="text-warning">${esc(data.limitation || 'Válido estritamente para a amostra respondente.')}</dd></div>
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
              <div><dt>O que mede</dt><dd>${esc(data.whatItMeasures || data.statement || 'Correlação entre dimensões')}</dd></div>
              <div><dt>Fonte</dt><dd>${esc(data.source || 'Respostas agregadas do diagnóstico')}</dd></div>
              <div><dt>Período / Recorte</dt><dd>${esc(data.period || 'Amostra do ciclo vigente')}</dd></div>
              <div><dt>Confiança</dt><dd>${esc(data.confidence || 'Padrão estatístico')}</dd></div>
              <div><dt>Premissas</dt><dd>${esc(data.premises || 'Respostas apuradas no ciclo')}</dd></div>
              <div><dt>Limitações</dt><dd class="text-warning">${esc(data.limitation || 'Não estabelece nexo de causalidade determinístico.')}</dd></div>
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
    const isExpired = cycle.expiresAt ? new Date(cycle.expiresAt) < new Date() : false;
    const hasReport = Boolean(cycle.status === 'report_ready' || cycle.reportGeneratedAt);

    if (isDraft) {
      const hasForm = Boolean(cycle.formId || cycle.formVersionId);
      if (!hasForm) {
        return {
          stageName: '1. Rascunho sem Formulário Vinculado',
          whatHappened: 'Diagnóstico criado no sistema, porém nenhuma metodologia ou formulário ativo foi vinculado.',
          whatIsMissing: 'Configurar o questionário na preparação para definir perguntas, escalas e blocos temáticos.',
          whoMustAct: 'Gestor do Diagnóstico / Administrador.',
          operationLabel: 'Configurar formulário',
          operationTarget: 'preparation',
          buttonClass: 'btn-primary'
        };
      }
      return {
        stageName: '1. Rascunho Pronto para Publicação',
        whatHappened: 'Formulário vinculado e parâmetros definidos. Diagnóstico pronto para auditoria técnica.',
        whatIsMissing: 'Revisar checklist pré-publicação, resolver impedimentos técnicos e ativar a coleta.',
        whoMustAct: 'Gestor do Diagnóstico / Administrador com permissão de publicação.',
        operationLabel: 'Revisar preparação & publicar',
        operationTarget: 'preparation',
        buttonClass: 'btn-primary'
      };
    }

    if (isClosed) {
      if (hasActions) {
        return {
          stageName: '6. Ciclo Encerrado (Planos em Execução)',
          whatHappened: 'Coleta finalizada e planos de ação registrados no Action Center.',
          whatIsMissing: 'Acompanhar a conclusão das atividades, comprovações de entrega e eficácia das ações.',
          whoMustAct: 'Líderes de iniciativas e executores no Action Center.',
          operationLabel: 'Acompanhar execução no Action™',
          operationTarget: 'actions',
          buttonClass: 'btn-primary'
        };
      }
      return {
        stageName: '6. Ciclo Encerrado (Sem Planos Ativos)',
        whatHappened: 'Coleta de respostas finalizada e ciclo formalmente encerrado sem planos de intervenção.',
        whatIsMissing: 'Definir planos de intervenção para os achados ou abrir novo ciclo comparativo no Evolution™.',
        whoMustAct: 'Comitê de Governança / Consultor de Evolução Organizacional.',
        operationLabel: 'Definir planos ou abrir ciclo no Evolution™',
        operationTarget: 'evolution',
        buttonClass: 'btn-outline-primary'
      };
    }

    if (responseCount === 0) {
      return {
        stageName: '2. Publicado (Aguardando Respostas)',
        whatHappened: 'Diagnóstico publicado com sucesso. Link seguro disponível para distribuição.',
        whatIsMissing: 'Divulgar o questionário aos participantes ou enviar convites por e-mail.',
        whoMustAct: 'Participantes convidados / Gestor de campanha para envio ou compartilhamento.',
        operationLabel: 'Divulgar ou enviar convites',
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
      if (isExpired) {
        return {
          stageName: '3. Prazo Vencido com Respostas (Pronto para Processar)',
          whatHappened: `Prazo da pesquisa encerrado com ${responseCount} resposta(s) persistida(s) no banco de dados.`,
          whatIsMissing: 'Disparar o processamento analítico para consolidar evidências, índices e inferências.',
          whoMustAct: 'Analista de Inteligência Organizacional / Gestor do Diagnóstico.',
          operationLabel: 'Processar Inteligência Valora™',
          operationCommand: 'process',
          buttonClass: 'btn-primary'
        };
      }
      return {
        stageName: '2. Coleta em Andamento no Prazo',
        whatHappened: `${responseCount} resposta(s) recebida(s) dentro do período ativo da pesquisa.`,
        whatIsMissing: 'Aguardar encerramento do prazo de coleta ou disparar processamento analítico parcial antecipado.',
        whoMustAct: 'Gestor do Diagnóstico / Respondentes adicionais.',
        operationLabel: 'Processar parcialmente',
        operationCommand: 'process',
        buttonClass: 'btn-outline-primary'
      };
    }

    if (hasPendingReviewInsights) {
      return {
        stageName: '4. Evidências Apuradas (Achados Pendentes de Revisão)',
        whatHappened: `${evidenceCount} evidências metodológicas extraídas das respostas reais dos participantes.`,
        whatIsMissing: 'Revisão humana dos achados sugeridos (aprovar/rejeitar com justificativa) antes de gerar planos.',
        whoMustAct: 'Liderança / Gestor responsável pela validação editorial.',
        operationLabel: 'Revisar achados analíticos',
        operationTarget: 'insights',
        buttonClass: 'btn-primary'
      };
    }

    if (hasReport) {
      return {
        stageName: '4. Processado com Relatório Disponível',
        whatHappened: `${responseCount} resposta(s) e ${evidenceCount} evidência(s) apuradas com relatório executivo gerado.`,
        whatIsMissing: 'Consultar o relatório executivo ou criar planos de ação preventivos e corretivos.',
        whoMustAct: 'Gestor do Diagnóstico / Liderança Executiva.',
        operationLabel: 'Consultar relatório executivo',
        operationTarget: 'report',
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
      whatIsMissing: 'Explorar os resultados nos painéis metodológicos ou criar plano de ação vinculado.',
      whoMustAct: 'Gestor do Diagnóstico / Liderança Executiva.',
      operationLabel: 'Criar plano de ação vinculado',
      operationUrl: diagnosticPlanUrl(cycle),
      buttonClass: 'btn-primary'
    };
  }

  function preparationView(data, review, prepOptions) {
    const cycle = data.cycle || data;
    const isDraft = cycle.status === 'draft';
    const isReadOnly = host.dataset.readOnly === 'true';
    const surveyId = cycle.surveyId || diagnosticId;

    let reviewState = 'unavailable';
    if (review === 'loading') {
      reviewState = 'loading';
    } else if (review && review.ok) {
      reviewState = (review.canPublish && (!review.blockers || review.blockers.length === 0)) ? 'ready' : 'blocked';
    } else {
      reviewState = 'unavailable';
    }

    const canPublish = !isReadOnly && isDraft && reviewState === 'ready';
    const blockers = (review && review.blockers) ? review.blockers : [];
    const warnings = (review && review.warnings) ? review.warnings : [];

    let badgeHtml = '';
    let alertHtml = '';

    if (reviewState === 'loading') {
      badgeHtml = '<span class="badge bg-info text-dark fs-6">Avaliando pré-requisitos…</span>';
      alertHtml = `
        <div class="alert alert-info d-flex align-items-center gap-2 mb-3">
          <div class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></div>
          <div>Consultando questionário vinculado, perguntas ativas e conformidade comercial no servidor…</div>
        </div>`;
    } else if (reviewState === 'ready') {
      badgeHtml = '<span class="badge bg-success fs-6">Apto para publicação</span>';
      alertHtml = `
        <div class="alert alert-success d-flex align-items-center gap-2 mb-3">
          <span>✅</span> <div><strong>Requisitos técnicos atendidos:</strong> Questionário publicado vinculado, perguntas ativas disponíveis e limite comercial do plano verificado com sucesso.</div>
        </div>`;
    } else if (reviewState === 'blocked') {
      badgeHtml = '<span class="badge bg-danger fs-6">Impedimentos pendentes</span>';
      alertHtml = `
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
        </div>`;
    } else {
      badgeHtml = '<span class="badge bg-secondary fs-6">Revisão indisponível</span>';
      alertHtml = `
        <div class="alert alert-warning d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
          <div>
            <strong>⚠️ Revisão técnica indisponível:</strong> Não foi possível validar os requisitos e limites de publicação no servidor. Por segurança, a publicação não está liberada até que a revisão seja concluída com êxito.
          </div>
          <button type="button" class="btn btn-sm btn-outline-dark" data-retry-review>Tentar novamente</button>
        </div>`;
    }

    const eligibleUsers = prepOptions?.eligibleUsers || [];
    const availableVersions = prepOptions?.availableVersions || [];
    const canChangeMethodology = prepOptions?.canChangeMethodology ?? (isDraft && Number(cycle.responseCount || 0) === 0);
    const lockReason = prepOptions?.methodologyLockReason || (Number(cycle.responseCount || 0) > 0 ? `Metodologia fixada porque já existem ${cycle.responseCount} respostas coletadas neste ciclo.` : null);

    const startsAtVal = cycle.startsAt ? new Date(cycle.startsAt).toISOString().split('T')[0] : '';
    const expiresAtVal = cycle.expiresAt ? new Date(cycle.expiresAt).toISOString().split('T')[0] : '';

    content.innerHTML = `<h2>Preparação Consolidada do Diagnóstico</h2>
      <p class="form-help">Parâmetros operacionais e metodológicos organizados para conduzir o ciclo de escuta com segurança e rastreabilidade.</p>

      <section class="valora-card mb-4" id="preparation-parameters-card">
        <div class="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
          <div>
            <h3 class="mb-0">Parâmetros Operacionais do Ciclo</h3>
            <small class="text-muted">Formulário editável com validação, concorrência otimista e proteção metodológica.</small>
          </div>
          <span class="badge bg-light text-dark border">Versão dos dados: v${esc(cycle.rowVersion || 1)}</span>
        </div>

        <div class="alert alert-danger d-none" data-form-error></div>
        <div class="alert alert-success d-none" data-form-success></div>
        <div class="alert alert-warning d-none" data-concurrency-alert>
          <strong>⚠️ Conflito de versão detectado:</strong> Este diagnóstico foi modificado por outro usuário ou sessão.
          <button type="button" class="btn btn-sm btn-outline-dark ms-2" data-reload-prep>Recarregar dados atualizados</button>
        </div>

        <form data-prep-form class="needs-validation">
          <input type="hidden" name="rowVersion" value="${esc(cycle.rowVersion || 1)}" />

          <div class="row g-3">
            <div class="col-12 col-md-8">
              <label class="form-label" for="prep-title"><strong>Título do Diagnóstico *</strong></label>
              <input type="text" class="form-control" id="prep-title" name="title" required minlength="3" value="${esc(cycle.title || '')}" ${isReadOnly ? 'disabled' : ''} />
              <div class="form-text">Mínimo de 3 caracteres. Identifica o ciclo nos painéis e relatórios.</div>
            </div>

            <div class="col-12 col-md-4">
              <label class="form-label" for="prep-responsible"><strong>Responsável Designado</strong></label>
              <select class="form-select" id="prep-responsible" name="responsibleUserId" ${isReadOnly ? 'disabled' : ''}>
                <option value="">Não atribuído</option>
                ${eligibleUsers.map(u => `
                  <option value="${esc(u.id)}" ${u.id === cycle.responsibleUserId ? 'selected' : ''}>
                    ${esc(u.name)} (${esc(u.email)})${u.department ? ` - ${esc(u.department)}` : ''}
                  </option>
                `).join('')}
              </select>
              <div class="form-text">Usuários ativos elegíveis da organização.</div>
            </div>

            <div class="col-12">
              <label class="form-label" for="prep-description"><strong>Objetivo / Descrição do Diagnóstico</strong></label>
              <textarea class="form-control" id="prep-description" name="description" rows="2" ${isReadOnly ? 'disabled' : ''}>${esc(cycle.description || '')}</textarea>
            </div>

            <div class="col-12 col-md-6">
              <label class="form-label" for="prep-targetAudience"><strong>Público-Alvo ou Recorte Organizacional</strong></label>
              <input type="text" class="form-control" id="prep-targetAudience" name="targetAudience" value="${esc(cycle.targetAudience || '')}" placeholder="Ex: Lideranças Regionais, Operações, Todos os Colaboradores" ${isReadOnly ? 'disabled' : ''} />
              <div class="form-text">Segmento que norteia a amostragem e a interpretação dos resultados.</div>
            </div>

            <div class="col-12 col-md-6">
              <label class="form-label" for="prep-formVersion"><strong>Questionário & Versão Metodológica</strong></label>
              ${canChangeMethodology && availableVersions.length > 0 ? `
                <select class="form-select" id="prep-formVersion" name="formVersionId" ${isReadOnly ? 'disabled' : ''}>
                  ${availableVersions.map(v => `
                    <option value="${esc(v.id)}" ${v.id === cycle.formVersionId ? 'selected' : ''}>
                      Versão ${esc(v.version)} (${v.isImmutable ? 'Imutável' : 'Publicada'}) - ${date(v.createdAt)}
                    </option>
                  `).join('')}
                </select>
                <div class="form-text text-success">Permitido alterar versão antes do início da coleta (0 respostas).</div>
              ` : `
                <input type="text" class="form-control" disabled value="${esc(cycle.formName || 'Formulário do Diagnóstico')} - v${esc(cycle.formVersionNumber || 1)}" />
                <div class="alert alert-secondary py-2 px-3 small mt-2 mb-0">
                  🔒 <strong>${esc(lockReason || 'Metodologia fixada.')}</strong>
                </div>
              `}
            </div>

            <div class="col-12 col-md-6">
              <label class="form-label" for="prep-startsAt"><strong>Data de Início da Coleta</strong></label>
              <input type="date" class="form-control" id="prep-startsAt" name="startsAt" value="${esc(startsAtVal)}" ${isReadOnly ? 'disabled' : ''} />
            </div>

            <div class="col-12 col-md-6">
              <label class="form-label" for="prep-expiresAt"><strong>Data de Término / Expiração</strong></label>
              <input type="date" class="form-control" id="prep-expiresAt" name="expiresAt" value="${esc(expiresAtVal)}" ${isReadOnly ? 'disabled' : ''} />
              <div class="form-text">Deve ser posterior à data de início.</div>
            </div>
          </div>

          ${!isReadOnly ? `
            <div class="d-flex gap-2 justify-content-end mt-4 pt-3 border-top">
              <button type="submit" class="btn btn-primary" data-save-prep-btn>
                <span class="spinner-border spinner-border-sm d-none me-1" role="status" aria-hidden="true" data-save-spinner></span>
                💾 Salvar Parâmetros
              </button>
            </div>
          ` : ''}
        </form>
      </section>

      <section class="valora-card mb-4">
        <div class="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
          <div>
            <h3 class="mb-0">Revisão Pré-Publicação</h3>
            <small class="text-muted">Conferência dos requisitos técnicos, avisos analíticos e limites de contratação.</small>
          </div>
          ${badgeHtml}
        </div>

        ${alertHtml}

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

        <div class="d-flex gap-2 flex-wrap pt-3 border-top align-items-center">
          ${isDraft ? `
            <button class="btn btn-primary" data-publish-survey ${canPublish ? '' : 'disabled'}>Publicar Diagnóstico & Ativar Coleta</button>
            ${!isReadOnly ? `<a class="btn btn-outline-secondary" href="/Surveys/${encodeURIComponent(surveyId)}/Edit">Editar configurações</a>` : ''}
          ` : `
            <button class="btn btn-outline-primary" onclick="document.querySelector('[data-tab=campaign]')?.click()">Acessar Campanha & Links</button>
            <button class="btn btn-outline-secondary" onclick="document.querySelector('[data-tab=participation]')?.click()">Ver Participação Real</button>
          `}
          ${isReadOnly ? `<span class="text-muted small ms-2">🔒 Modo somente leitura: ações operacionais indisponíveis.</span>` : ''}
        </div>
      </section>`;

    const prepForm = content.querySelector('[data-prep-form]');
    if (prepForm) {
      prepForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const formError = content.querySelector('[data-form-error]');
        const formSuccess = content.querySelector('[data-form-success]');
        const concurrencyAlert = content.querySelector('[data-concurrency-alert]');
        const saveBtn = content.querySelector('[data-save-prep-btn]');
        const spinner = content.querySelector('[data-save-spinner]');

        formError.classList.add('d-none');
        formSuccess.classList.add('d-none');
        concurrencyAlert.classList.add('d-none');

        const formData = new FormData(prepForm);
        const title = (formData.get('title') || '').toString().trim();
        const description = (formData.get('description') || '').toString().trim();
        const targetAudience = (formData.get('targetAudience') || '').toString().trim();
        const responsibleUserId = formData.get('responsibleUserId') ? formData.get('responsibleUserId').toString() : null;
        const startsAt = formData.get('startsAt') ? formData.get('startsAt').toString() : null;
        const expiresAt = formData.get('expiresAt') ? formData.get('expiresAt').toString() : null;
        const formVersionId = formData.get('formVersionId') ? formData.get('formVersionId').toString() : null;
        const rowVersion = parseInt(formData.get('rowVersion') || '1', 10);

        if (!title || title.length < 3) {
          formError.textContent = 'O título do diagnóstico é obrigatório e deve ter no mínimo 3 caracteres.';
          formError.classList.remove('d-none');
          return;
        }

        if (startsAt && expiresAt && new Date(expiresAt) <= new Date(startsAt)) {
          formError.textContent = 'O prazo final de coleta deve ser posterior à data de início.';
          formError.classList.remove('d-none');
          return;
        }

        if (saveBtn) saveBtn.disabled = true;
        if (spinner) spinner.classList.remove('d-none');

        try {
          const res = await fetch(`/bff/diagnostics/${encodeURIComponent(diagnosticId)}/workspace/preparation`, {
            method: 'PUT',
            headers: {
              'Content-Type': 'application/json',
              'Accept': 'application/json',
              'X-CSRF-TOKEN': host.querySelector('[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({
              title,
              description: description || null,
              targetAudience: targetAudience || null,
              responsibleUserId: responsibleUserId || null,
              startsAt: startsAt ? new Date(startsAt).toISOString() : null,
              expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null,
              formVersionId: formVersionId || null,
              rowVersion
            })
          });

          const result = await res.json().catch(() => ({}));
          if (res.status === 409) {
            concurrencyAlert.classList.remove('d-none');
            return;
          }
          if (!res.ok) {
            formError.textContent = result.message || 'Falha ao salvar parâmetros do diagnóstico.';
            formError.classList.remove('d-none');
            return;
          }

          formSuccess.textContent = 'Parâmetros atualizados com sucesso no banco de dados!';
          formSuccess.classList.remove('d-none');

          const updatedCycle = result.data || result;
          if (overviewState) overviewState.cycle = updatedCycle;
          setOverviewHeader(overviewState || { cycle: updatedCycle });

          setTimeout(async () => {
            const [refreshedReview, refreshedOptions] = await Promise.all([
              getPublicationReview(surveyId),
              api('preparation/options').catch(() => null)
            ]);
            preparationView(updatedCycle, refreshedReview, refreshedOptions);
          }, 800);
        } catch (ex) {
          formError.textContent = ex.message || 'Erro de comunicação ao salvar parâmetros.';
          formError.classList.remove('d-none');
        } finally {
          if (saveBtn) saveBtn.disabled = false;
          if (spinner) spinner.classList.add('d-none');
        }
      });
    }

    const reloadPrepBtn = content.querySelector('[data-reload-prep]');
    if (reloadPrepBtn) {
      reloadPrepBtn.addEventListener('click', () => tab('preparation'));
    }

    const retryBtn = content.querySelector('[data-retry-review]');
    if (retryBtn) {
      retryBtn.addEventListener('click', async () => {
        preparationView(data, 'loading', prepOptions);
        const refreshed = await getPublicationReview(surveyId);
        preparationView(data, refreshed, prepOptions);
      });
    }

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
          if (requestId === activeRequest && publishBtn) publishBtn.disabled = false;
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
        preparationView(overviewState?.cycle || {}, 'loading', null);
        const [review, prepOptions] = await Promise.all([
          getPublicationReview(surveyId),
          api('preparation/options').catch(() => null)
        ]);
        if (requestId !== activeRequest) return;
        preparationView(overviewState?.cycle || {}, review, prepOptions);
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
  if (isReadOnly) {
    host.querySelectorAll('[data-command]').forEach(button => {
      button.disabled = true;
      button.title = 'Modo de consulta (somente leitura): Ação desabilitada.';
    });
  }

  host.querySelectorAll('[data-command]').forEach(button => button.addEventListener('click', async () => {
    if (isReadOnly) {
      error.textContent = 'Ações operacionais não são permitidas no perfil somente leitura.';
      error.classList.remove('d-none');
      return;
    }
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
      if (!isReadOnly) button.disabled = false;
    }
  }));

  const initialTab = new URLSearchParams(location.search).get('tab') || 'overview';
  tab(allowedTabs.has(initialTab) ? initialTab : 'overview');
})();
