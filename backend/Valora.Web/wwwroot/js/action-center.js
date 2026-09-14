(() => {
  'use strict';
  const root = document.currentScript;
  let opener = null;
  const open = id => {
    const dialog = document.getElementById(id);
    if (!dialog || typeof dialog.showModal !== 'function') return;
    dialog.showModal();
    (dialog.querySelector('[aria-invalid="true"], input:not([type="hidden"]), textarea, select, button') || dialog).focus();
  };
  document.querySelectorAll('[data-action-dialog-open]').forEach(button => button.addEventListener('click', () => { opener = button; open(button.dataset.actionDialogOpen); }));
  document.querySelectorAll('.action-detail dialog').forEach(dialog => {
    dialog.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
    dialog.addEventListener('close', () => opener?.focus());
    dialog.addEventListener('click', event => { if (event.target === dialog && !dialog.querySelector('form')?.matches(':has(:user-invalid)')) dialog.close(); });
    dialog.querySelector('form')?.addEventListener('submit', event => {
      if (!event.currentTarget.checkValidity()) return;
      event.currentTarget.querySelectorAll('button').forEach(button => { button.disabled = true; });
      event.currentTarget.setAttribute('aria-busy', 'true');
    });
  });
  const requested = root?.dataset.openDialog;
  if (requested) { open(requested); document.querySelector('[data-validation-summary]')?.focus(); }
})();
