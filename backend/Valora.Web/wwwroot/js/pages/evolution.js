(() => {
  document.querySelectorAll('form[data-submit-once]').forEach(form => form.addEventListener('submit', event => {
    if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
    form.dataset.submitting = 'true';
    form.setAttribute('aria-busy', 'true');
    form.querySelectorAll('button[type="submit"],button:not([type])').forEach(button => { button.disabled = true; });
  }));
  const dialog = document.querySelector('[data-cycle-dialog]');
  let trigger;
  document.querySelector('[data-action="new-cycle"]')?.addEventListener('click', event => { trigger = event.currentTarget; dialog?.showModal(); });
  dialog?.querySelectorAll('[data-dialog-close],[data-form-cancel]').forEach(button => button.addEventListener('click', () => dialog.close()));
  dialog?.addEventListener('close', () => trigger?.focus());
  const select = document.querySelector('[data-measurement-select]');
  function updateMode() {
    const calculated = document.querySelector('[name="ReferenceMode"]:checked')?.value === 'calculated';
    const calculatedFields = document.querySelector('[data-calculated-fields]');
    const manualFields = document.querySelector('[data-manual-fields]');
    if (!calculatedFields || !manualFields) return;
    calculatedFields.hidden = !calculated;
    manualFields.hidden = calculated;
    select.disabled = !calculated;
    select.required = calculated;
    manualFields.querySelector('input').disabled = calculated;
    document.querySelector('[data-calculated-score]').value = select.selectedOptions[0]?.dataset.score || '';
  }
  document.querySelectorAll('[name="ReferenceMode"]').forEach(input => input.addEventListener('change', updateMode));
  select?.addEventListener('change', updateMode);
  updateMode();
  document.querySelectorAll('[data-measurements-more]').forEach(button => button.addEventListener('click', async () => {
    const target = document.querySelector('[data-measurement-select]');
    const status = document.querySelector('[data-measurements-status]');
    button.disabled = true;
    status.textContent = 'Carregando medições...';
    try {
      const query = new URLSearchParams({ page: button.dataset.page || '2' });
      if (button.dataset.diagnostic) query.set('diagnosticId', button.dataset.diagnostic);
      const response = await fetch('/Evolution/MeasurementOptions?' + query, { headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error('Não foi possível carregar as medições.');
      const data = await response.json();
      data.items.forEach(item => {
        if (Array.from(target.options).some(option => option.value === item.sourceSurveyId)) return;
        const option = new Option(item.diagnosticTitle + ' · ' + item.percentage + ' pontos · ' + item.scoredResponseCount + ' respostas · ' + item.methodologyVersion, item.sourceSurveyId);
        option.dataset.score = item.percentage;
        target.add(option);
      });
      button.dataset.page = String(data.page + 1);
      button.hidden = !data.hasMore;
      status.textContent = data.items.length ? 'Medições carregadas.' : 'Nenhuma medição elegível nesta página.';
    } catch (error) { status.textContent = error.message; }
    finally { button.disabled = false; }
  }));
})();
