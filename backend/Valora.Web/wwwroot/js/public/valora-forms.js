(() => {
  'use strict';
  const digits = value => value.replace(/\D/g, '');
  const phone = value => {
    const raw = digits(value).slice(0, 11);
    if (raw.length <= 2) return raw;
    if (raw.length <= 6) return `(${raw.slice(0, 2)}) ${raw.slice(2)}`;
    const split = raw.length === 11 ? 7 : 6;
    return `(${raw.slice(0, 2)}) ${raw.slice(2, split)}-${raw.slice(split)}`;
  };
  document.querySelectorAll('[data-mask="phone"]').forEach(input => input.addEventListener('input', () => { input.value = phone(input.value); }));
  document.querySelectorAll('[data-professional-form]').forEach(form => form.addEventListener('submit', event => {
    form.classList.add('was-validated');
    if (!form.checkValidity()) { event.preventDefault(); form.querySelector(':invalid')?.focus(); return; }
    const button = form.querySelector('[type="submit"]');
    if (!button || button.disabled) { event.preventDefault(); return; }
    button.disabled = true; button.classList.add('is-loading');
    const label = button.querySelector('[data-button-label]');
    if (label) label.textContent = button.dataset.loadingText || 'Enviando…';
  }));
})();
