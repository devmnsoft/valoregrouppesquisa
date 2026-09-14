(() => {
  'use strict';
  document.querySelectorAll('form[data-prevent-double-submit]').forEach(form => {
    form.addEventListener('submit', event => {
      if (form.dataset.submitting === 'true') {
        event.preventDefault();
        return;
      }
      if (!form.checkValidity()) return;
      form.dataset.submitting = 'true';
      form.setAttribute('aria-busy', 'true');
      form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach(control => {
        control.disabled = true;
      });
    });
  });
})();
