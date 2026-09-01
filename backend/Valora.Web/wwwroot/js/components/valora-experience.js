(function () {
  'use strict';
  document.documentElement.classList.add('valora-js');

  const banner = document.querySelector('[data-organization-context]');
  document.addEventListener('valora:account-context', event => { if (banner) banner.hidden = Boolean(event.detail?.organizationName); });

  let pendingConfirmation = null;
  document.addEventListener('click', event => {
    const dismiss = event.target.closest('[data-dismiss-alert]');
    if (dismiss) dismiss.closest('.valora-alert')?.remove();

    const destructive = event.target.closest('[data-confirm]');
    if (!destructive || destructive.dataset.confirmed === 'true') return;
    event.preventDefault();
    pendingConfirmation = destructive;
    const modalElement = document.getElementById('appModal');
    if (!modalElement || !window.bootstrap?.Modal) return;
    modalElement.querySelector('#appModalDescription').textContent = destructive.dataset.confirm || 'Confirme se deseja continuar com esta ação.';
    window.bootstrap.Modal.getOrCreateInstance(modalElement).show();
  });

  document.querySelector('[data-confirm-proceed]')?.addEventListener('click', () => {
    if (!pendingConfirmation) return;
    const target = pendingConfirmation;
    pendingConfirmation = null;
    target.dataset.confirmed = 'true';
    window.bootstrap.Modal.getInstance(document.getElementById('appModal'))?.hide();
    if (target.form) target.form.requestSubmit(target); else target.click();
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
}());
