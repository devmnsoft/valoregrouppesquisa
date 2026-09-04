(() => {
  const root = document.querySelector('[data-page="command-center"]');
  if (!root) return;
  root.querySelectorAll('form').forEach(form => form.addEventListener('submit', () => {
    const button = form.querySelector('[data-loading-text]');
    if (!button || !form.checkValidity()) return;
    button.disabled = true;
    button.textContent = button.dataset.loadingText;
  }));
  root.querySelector('[data-clear-filters]')?.addEventListener('click', event => {
    if (!root.querySelector('[data-filter-form] select:invalid')) return;
    event.preventDefault();
    window.Toast?.warning?.('Revise os filtros antes de continuar.');
  });
})();
