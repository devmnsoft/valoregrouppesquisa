(() => {
  'use strict';
  const restore = form => {
    delete form.dataset.submitting;
    form.removeAttribute('aria-busy');
    form.querySelectorAll('[data-submit-disabled="true"]').forEach(control => {
      control.disabled = false;
      delete control.dataset.submitDisabled;
    });
  };
  document.querySelectorAll('form[data-prevent-double-submit]').forEach(form => {
    form.addEventListener('submit', event => {
      if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
      if (!form.checkValidity()) return;
      setTimeout(() => {
        if (event.defaultPrevented) return;
        form.dataset.submitting = 'true';
        form.setAttribute('aria-busy', 'true');
        form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach(control => {
          if (!control.disabled) control.dataset.submitDisabled = 'true';
          control.disabled = true;
        });
      }, 0);
    });
    form.addEventListener('invalid', () => restore(form), true);
  });
  window.addEventListener('pageshow', () => document.querySelectorAll('form[data-submitting="true"]').forEach(restore));
})();
