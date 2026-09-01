(() => {
  document.querySelectorAll('.bm form, .ind form').forEach(form => {
    form.addEventListener('submit', () => {
      if (!form.checkValidity()) return;
      const button = form.querySelector('button[type="submit"], button:not([type])');
      if (!button) return;
      button.disabled = true;
      button.setAttribute('aria-busy', 'true');
      button.dataset.originalText = button.textContent;
      button.textContent = 'Processando…';
    });
  });
})();
