(() => {
  'use strict';
  const script = document.currentScript;
  const root = script?.closest('[data-action-center-module]') || document.querySelector('[data-action-center-module]');
  if (!root) return;
  let opener = null;
  const snapshot = form => new URLSearchParams(new FormData(form)).toString();
  const isDirty = form => Boolean(form) && form.dataset.initialState !== snapshot(form);
  const mayClose = dialog => !isDirty(dialog.querySelector('form')) || window.confirm('Há alterações não salvas. Deseja descartá-las?');
  const open = id => {
    const dialog = root.querySelector(`#${CSS.escape(id)}`);
    if (!dialog || typeof dialog.showModal !== 'function') return;
    dialog.showModal();
    const target = dialog.querySelector('[data-validation-summary]:not(:empty), [aria-invalid="true"], :invalid, input:not([type="hidden"]), textarea, select, button');
    (target || dialog).focus();
  };
  root.querySelectorAll('[data-action-dialog-open]').forEach(button => button.addEventListener('click', () => {
    opener = button;
    open(button.dataset.actionDialogOpen);
  }));
  root.querySelectorAll('dialog[data-action-dialog]').forEach(dialog => {
    const form = dialog.querySelector('form');
    if (form) form.dataset.initialState = snapshot(form);
    dialog.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', () => {
      if (mayClose(dialog)) dialog.close();
    }));
    dialog.addEventListener('cancel', event => { if (!mayClose(dialog)) event.preventDefault(); });
    dialog.addEventListener('close', () => { opener?.focus(); opener = null; });
    dialog.addEventListener('click', event => { if (event.target === dialog && mayClose(dialog)) dialog.close(); });
    form?.addEventListener('submit', event => {
      if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
      if (!form.checkValidity()) { event.preventDefault(); form.reportValidity(); return; }
      form.dataset.submitting = 'true';
      form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach(button => { button.disabled = true; });
      form.setAttribute('aria-busy', 'true');
    });
  });
  const requested = script?.dataset.openDialog;
  if (requested) open(requested);
})();
