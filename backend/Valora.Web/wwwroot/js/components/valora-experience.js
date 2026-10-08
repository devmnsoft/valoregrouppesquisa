(function () {
  'use strict';
  document.documentElement.classList.add('valora-js');

  const banner = document.querySelector('[data-organization-context]');
  document.addEventListener('valora:account-context', event => { if (banner) banner.hidden = Boolean(event.detail?.organizationName); });

  // Confirmações de ações ([data-confirm]) são tratadas por /js/valora-ui.js no diálogo global de confirmação.
  document.addEventListener('click', event => {
    const dismiss = event.target.closest('[data-dismiss-alert]');
    if (dismiss) dismiss.closest('.valora-alert')?.remove();
  });

  document.addEventListener('submit', event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.checkValidity()) return;
    const button = event.submitter || form.querySelector('button[type="submit"]');
    if (!button || button.dataset.loading === 'true') return;
    button.dataset.loading = 'true';
    button.dataset.originalLabel = button.innerHTML;
    button.setAttribute('aria-busy', 'true');
    button.disabled = true;
    button.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span> Processando…';
    window.setTimeout(() => {
      if (button.dataset.loading !== 'true') return;
      button.innerHTML = button.dataset.originalLabel;
      button.dataset.loading = 'false';
      button.disabled = false;
      button.removeAttribute('aria-busy');
    }, 12000);
  });

  document.addEventListener('invalid', event => {
    const field = event.target;
    if (!(field instanceof HTMLElement)) return;
    field.setAttribute('aria-invalid', 'true');
    const form = field.closest('form');
    if (!form || form.dataset.validationAnnounced === 'true') return;
    form.dataset.validationAnnounced = 'true';
    window.ValoraToast?.warning?.('Revise os campos destacados antes de continuar.');
    window.setTimeout(() => { delete form.dataset.validationAnnounced; }, 800);
  }, true);

  document.addEventListener('input', event => {
    const field = event.target;
    if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement || field instanceof HTMLTextAreaElement) {
      if (field.checkValidity()) field.removeAttribute('aria-invalid');
    }
  });
}());
