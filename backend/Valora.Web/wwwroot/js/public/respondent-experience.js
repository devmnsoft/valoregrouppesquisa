(() => {
  const portal = document.querySelector('[data-respondent-portal]');
  if (!portal) return;
  const token = portal.dataset.token;
  const api = `/api/v1/respondent/${encodeURIComponent(token)}`;
  const request = async (path, options = {}) => {
    const response = await fetch(`${api}/${path}`, { headers: { 'Content-Type': 'application/json', 'X-Correlation-ID': crypto.randomUUID() }, ...options });
    if (!response.ok) throw new Error('link-unavailable');
    return response.json();
  };
  const consent = portal.querySelector('[data-consent]');
  const start = portal.querySelector('[data-start]');
  consent?.addEventListener('change', () => { start.disabled = !consent.checked; });
  start?.addEventListener('click', async () => { start.disabled = true; try { await request('start', { method: 'POST' }); location.assign(`/r/${token}/questions`); } catch { location.assign(`/r/${token}?indisponivel=1`); } });
  const form = portal.querySelector('[data-question-form]');
  const save = async () => { const selected = form?.querySelector('input:checked'); if (!selected) { form?.reportValidity(); return false; } await request('progress', { method: 'PUT', body: JSON.stringify({ progressPercent: 35, answersJson: JSON.stringify({ strategy: selected.value }) }) }); const message = portal.querySelector('[data-save-message]'); if (message) message.textContent = 'Sua resposta foi salva.'; return true; };
  portal.querySelector('[data-save]')?.addEventListener('click', async () => { try { await save(); } catch { const message = portal.querySelector('[data-save-message]'); if (message) message.textContent = 'Não foi possível salvar agora. Verifique sua conexão e tente novamente.'; } });
  form?.addEventListener('submit', async event => { event.preventDefault(); try { if (await save()) location.assign(`/r/${token}/review`); } catch { portal.querySelector('[data-save-message]').textContent = 'Não foi possível salvar agora. Verifique sua conexão e tente novamente.'; } });
  portal.querySelector('[data-complete]')?.addEventListener('click', async event => { event.currentTarget.disabled = true; try { await request('complete', { method: 'POST' }); location.assign(`/r/${token}/completed`); } catch { event.currentTarget.disabled = false; } });
})();
