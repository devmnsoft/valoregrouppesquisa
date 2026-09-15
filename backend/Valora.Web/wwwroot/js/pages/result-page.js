(() => {
  const page = document.querySelector('[data-page="result-page"]');
  if (!page) return;
  const responseId = page.querySelector('input[name="ResponseId"],input[name="responseId"]')?.value || location.pathname.split('/').filter(Boolean).pop();
  const output = page.querySelector('[data-results]');
  const loading = page.querySelector('.valora-empty-state');
  const error = page.querySelector('.valora-error-state');
  const esc = value => String(value ?? '').replace(/[&<>'"]/g, character => ({ '&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;' }[character]));
  const present = value => value !== null && value !== undefined && value !== '';
  const date = value => present(value) ? new Date(value).toLocaleString('pt-BR') : 'Não informada';
  const score = (value, suffix = '') => present(value) ? `${esc(value)}${suffix}` : 'Não calculado';
  const labels = { awaiting_responses:'Aguardando respostas', insufficient_data:'Dados insuficientes', processing_pending:'Processamento pendente', processing:'Processamento em andamento', available:'Resultado disponível', failed:'Falha de processamento' };
  const statusClass = status => status === 'available' ? 'success' : status === 'failed' ? 'danger' : 'warning';
  const planStatus = status => ({draft:'Rascunho',proposed:'Proposto',approved:'Aprovado',in_execution:'Em execução',completed:'Concluído',canceled:'Cancelado'}[status] || status);
  const unwrap = value => value?.data ?? value;

  function render(data) {
    const dimensions = data.dimensions || [], reports = data.reports || [], plans = data.plans || [];
    const available = data.processingStatus === 'available' && present(data.resultId);
    const limitation = available
      ? (dimensions.length ? 'A leitura representa o resultado processado desta resposta e desta versão; comparações históricas exigem bases metodologicamente compatíveis.' : 'Não há cobertura por dimensão disponível para este processamento.')
      : labels[data.processingStatus] || 'O estado do processamento ainda não permite uma leitura executiva.';
    output.innerHTML = `
      <header class="result-identity"><div><p class="eyebrow">Diagnóstico</p><h2>${esc(data.surveyTitle || 'Não identificado')}</h2><p>${esc(data.organizationName || 'Organização não informada')}</p></div><span class="valora-status valora-status--${statusClass(data.processingStatus)}">${esc(labels[data.processingStatus] || data.processingStatus)}</span></header>
      <dl class="result-metadata"><div><dt>Período</dt><dd>${esc(date(data.periodStart))} — ${esc(date(data.periodEnd))}</dd></div><div><dt>Formulário</dt><dd>${esc(data.formName)} · versão ${esc(data.formVersion)}</dd></div><div><dt>Processado em</dt><dd>${esc(date(data.processedAt))}</dd></div><div><dt>Respostas elegíveis</dt><dd>${present(data.eligibleResponseCount) ? esc(data.eligibleResponseCount) : 'Não calculável'}</dd></div></dl>
      <section aria-labelledby="executive-reading"><h2 id="executive-reading">Leitura executiva</h2><div class="result-kpis"><article><span>Pontuação calculada</span><strong>${score(data.totalScore)}${present(data.maxScore) ? ` / ${esc(data.maxScore)}` : ''}</strong></article><article><span>Percentual calculado</span><strong>${score(data.percentage, '%')}</strong></article><article><span>Classificação calculada</span><strong>${esc(data.maturityLabel || 'Não determinada')}</strong></article><article><span>Cobertura dimensional</span><strong>${dimensions.length ? `${dimensions.length} dimensão(ões)` : 'Não calculável'}</strong></article></div>
      ${present(data.strategicTruth) ? `<div class="result-evidence"><h3>Evidência e interpretação</h3><p>${esc(data.strategicTruth)}</p>${present(data.riskIfNothingChanges) ? `<p><strong>Risco registrado:</strong> ${esc(data.riskIfNothingChanges)}</p>` : ''}</div>` : ''}
      <div class="valora-alert" role="note"><strong>Limitações</strong><br>${esc(limitation)}</div>
      <div class="result-dimensions">${dimensions.map(item => `<article><h3>${esc(item.dimensionName)}</h3><strong>${score(item.score)}${present(item.maxScore) ? ` / ${esc(item.maxScore)}` : ''}</strong><span>${score(item.percentage, '%')} · ${esc(item.levelLabel || 'Sem classificação')}</span></article>`).join('') || '<p>Nenhuma dimensão calculada está disponível.</p>'}</div></section>
      <section class="result-next" aria-labelledby="next-actions"><h2 id="next-actions">Próximas ações</h2><div class="result-actions"><a class="valora-button" href="/Methodology">Consultar metodologia</a>${available ? `<a class="valora-button" href="/Reports?responseId=${encodeURIComponent(responseId)}&returnUrl=${encodeURIComponent(location.pathname)}">Gerar ou abrir relatório</a><a class="valora-button valora-button--primary" href="/ActionCenter/Plans/Create?originType=result&originId=${encodeURIComponent(data.resultId)}&resultId=${encodeURIComponent(data.resultId)}&returnUrl=${encodeURIComponent(location.pathname)}">Criar plano de ação</a>` : ''}</div>${available ? '' : `<p class="action-impediment">${esc(limitation)} As ações dependentes do resultado serão exibidas após o processamento.</p>`}</section>
      <section aria-labelledby="linked-reports"><h2 id="linked-reports">Relatórios deste resultado</h2>${reports.length ? `<div class="table-responsive"><table><thead><tr><th>Relatório</th><th>Estado</th><th>Emissão</th><th>Ação</th></tr></thead><tbody>${reports.map(item => `<tr><td>${esc(item.title)}</td><td>${esc(item.status)}</td><td>${esc(date(item.createdAt))}</td><td><a href="/Reports?reportId=${encodeURIComponent(item.id)}&returnUrl=${encodeURIComponent(location.pathname)}">Abrir</a></td></tr>`).join('')}</tbody></table></div>` : '<p>Nenhum relatório foi emitido para esta resposta.</p>'}</section>
      <section aria-labelledby="linked-plans"><h2 id="linked-plans">Planos vinculados</h2>${plans.length ? `<div class="table-responsive"><table><thead><tr><th>Título</th><th>Responsável</th><th>Prazo</th><th>Situação</th><th>Progresso</th><th></th></tr></thead><tbody>${plans.map(item => `<tr><td>${esc(item.title)}</td><td>${esc(item.ownerName || 'Não atribuído')}</td><td>${esc(date(item.dueAt))}</td><td>${esc(planStatus(item.status))}</td><td>${esc(item.progressPercent)}%</td><td><a href="/ActionCenter/Plans/Details/${encodeURIComponent(item.id)}?returnUrl=${encodeURIComponent(location.pathname)}">Detalhes</a></td></tr>`).join('')}</tbody></table></div>` : '<p>Nenhum plano está vinculado a este resultado.</p>'}</section>`;
  }

  (async () => { try { window.Loading?.show('Preparando resultado executivo…'); render(unwrap(await window.AjaxClient.get(`/bff/responses/${encodeURIComponent(responseId)}/result`))); loading.hidden = true; loading.classList.add('d-none'); } catch (reason) { const message = window.formatFriendlyError?.(reason) || reason?.message || 'Não foi possível carregar o resultado.'; error.textContent = message; error.classList.remove('d-none'); window.Toast?.error(message); } finally { window.Loading?.hide(); } })();
})();
