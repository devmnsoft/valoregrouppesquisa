(() => {
  const root = document.querySelector('[data-intelligence-page]'); if (!root) return;
  const errorBox = root.querySelector('[data-error]');
  const escapeHtml = value => String(value ?? '').replace(/[&<>'"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c]));
  const showError = error => { errorBox.hidden = false; errorBox.textContent = error?.status === 403 ? 'Seu perfil ou plano não possui acesso a este módulo.' : (error?.message || 'Não foi possível carregar a leitura.'); };
  const percent = value => `${Number(value || 0).toLocaleString('pt-BR', {maximumFractionDigits: 1})}%`;
  function render(data) {
    const run = data.latestRun, evidence = data.evidence;
    root.querySelector('[data-kpi="maturity"]').textContent = run ? percent(run.maturityIndex) : '—';
    root.querySelector('[data-kpi="confidence"]').textContent = run?.confidenceLevel?.replace('_', ' ') || '—';
    root.querySelector('[data-kpi="gap"]').textContent = run ? percent(run.structuralGap) : '—';
    root.querySelector('[data-kpi="evidence"]').textContent = run?.evidenceCount ?? evidence?.total ?? 0;
    const warning = root.querySelector('[data-warning]'); warning.hidden = !run?.warning; warning.textContent = run?.warning || '';
    root.querySelector('[data-heatmap]').innerHTML = (run?.heatmap || evidence?.dimensions || []).map(x => `<div><div class="d-flex justify-content-between"><strong>${escapeHtml(x.name)}</strong><span>${percent(x.score)}</span></div><progress max="100" value="${Number(x.score)}" class="w-100"></progress><small>${x.evidenceCount} evidências agregadas</small></div>`).join('') || '<p>Ainda não há dimensões avaliadas.</p>';
    root.querySelector('[data-insights]').innerHTML = (run?.insights || []).map(x => `<article class="valora-card"><p class="eyebrow">${escapeHtml(x.priority)} · ${escapeHtml(x.dimension)}</p><h3>${escapeHtml(x.observation)}</h3><dl><dt>Evidências</dt><dd>${escapeHtml(x.evidence)}</dd><dt>Correlação</dt><dd>${escapeHtml(x.correlation)}</dd><dt>Causa provável</dt><dd>${escapeHtml(x.probableCause)}</dd><dt>Impacto</dt><dd>${escapeHtml(x.impact)}</dd><dt>Plano de evolução</dt><dd>${escapeHtml(x.evolutionPlan)}</dd></dl></article>`).join('') || '<article class="valora-card"><p>Gere uma leitura quando houver evidências disponíveis.</p></article>';
    root.querySelector('[data-journey]').innerHTML = (data.journey || []).map(x => `<li><span></span><div><strong>${escapeHtml(x.title)}</strong><small>${escapeHtml(x.description)}</small></div><time>${new Date(x.occurredAt).toLocaleDateString('pt-BR')}</time></li>`).join('') || '<li>Nenhum marco registrado.</li>';
  }
  async function load() { errorBox.hidden = true; try { render(await IntelligenceApi.dashboard()); } catch (e) { showError(e); } }
  root.querySelector('[data-generate]').addEventListener('click', async e => { e.currentTarget.disabled = true; try { await IntelligenceApi.generate(); window.ValoraToast?.success('Nova leitura gerada com sucesso.'); await load(); } catch (err) { showError(err); } finally { e.currentTarget.disabled = false; } });
  const modal = root.querySelector('[data-journey-modal]'); root.querySelector('[data-open-journey]').onclick = () => modal.showModal(); root.querySelector('[data-close-journey]').onclick = () => modal.close();
  root.querySelector('[data-journey-form]').addEventListener('submit', async e => { e.preventDefault(); const form = new FormData(e.currentTarget); try { await IntelligenceApi.createJourney(Object.fromEntries(form)); modal.close(); e.currentTarget.reset(); await load(); } catch (err) { showError(err); } });
  load();
})();
