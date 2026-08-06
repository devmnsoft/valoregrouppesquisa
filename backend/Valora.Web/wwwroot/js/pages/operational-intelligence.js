(() => {
  const modal = document.querySelector('[data-upgrade-modal]');
  document.querySelectorAll('[data-upgrade]').forEach(button => button.addEventListener('click', () => modal?.showModal()));
  document.querySelector('.ops-filters')?.addEventListener('submit', event => {
    event.preventDefault();
    window.ValoraToast?.show?.('Filtros aplicados à análise.', 'success');
  });
})();
