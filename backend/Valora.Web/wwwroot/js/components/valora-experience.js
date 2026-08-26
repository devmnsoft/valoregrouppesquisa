(function () {
  'use strict';
  const root = document.documentElement;
  root.classList.add('valora-js');

  const banner = document.querySelector('[data-organization-context]');
  document.addEventListener('valora:account-context', event => { if (banner) banner.hidden = Boolean(event.detail?.organizationName); });

  document.addEventListener('click', event => {
    const destructive = event.target.closest('[data-confirm]');
    if (destructive && !window.confirm(destructive.dataset.confirm || 'Confirma esta ação?')) event.preventDefault();
  });

  document.addEventListener('submit', event => {
    const button = event.target.matches('[data-auto-loading]') ? event.target.querySelector('button[type="submit"]') : null;
    if (!button || button.dataset.loading === 'true') return;
    button.dataset.loading = 'true';
    button.dataset.originalLabel = button.innerHTML;
    button.setAttribute('aria-busy', 'true');
    window.setTimeout(() => { if (button.dataset.loading === 'true') { button.innerHTML = button.dataset.originalLabel; button.dataset.loading = 'false'; button.removeAttribute('aria-busy'); } }, 12000);
  });
}());
