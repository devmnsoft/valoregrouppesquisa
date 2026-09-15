(() => {
  'use strict';
  const page = document.querySelector('[data-closure-page]');
  const form = page?.querySelector('[data-closure-form]');
  if (!page || !form) return;
  const key = `valora:plan-closure:${page.dataset.planId}`;
  const fields = ['Command.Result', 'Command.Evidence', 'Command.Confirmed'];
  let submitted = false;
  const hasServerValues = fields.some(name => {
    const field = form.elements[name];
    return field?.type === 'checkbox' ? field.checked : Boolean(field?.value);
  });
  if (!hasServerValues) {
    try {
      const saved = JSON.parse(sessionStorage.getItem(key) || '{}');
      fields.forEach(name => { const field = form.elements[name]; if (!field || saved[name] == null) return; field.type === 'checkbox' ? field.checked = saved[name] : field.value = saved[name]; });
    } catch { sessionStorage.removeItem(key); }
  }
  const save = () => {
    const value = {};
    fields.forEach(name => { const field = form.elements[name]; if (field) value[name] = field.type === 'checkbox' ? field.checked : field.value; });
    sessionStorage.setItem(key, JSON.stringify(value));
  };
  form.addEventListener('input', save);
  form.addEventListener('submit', event => {
    if (submitted) { event.preventDefault(); return; }
    if (!form.checkValidity()) return;
    submitted = true;
    sessionStorage.removeItem(key);
    const button = form.querySelector('[data-submit-once]');
    if (button) { button.disabled = true; button.textContent = 'Concluindo…'; }
  });
  window.addEventListener('beforeunload', event => { if (!submitted && fields.some(name => { const field=form.elements[name]; return field?.type==='checkbox'?field.checked:Boolean(field?.value); })) { save(); event.preventDefault(); event.returnValue=''; } });
  const summary = page.querySelector('[data-validation-summary]:not(:empty)');
  summary?.focus();
})();
