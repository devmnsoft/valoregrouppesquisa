(function () {
  'use strict';

  let activeDialog = null;
  const focusable = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

  function close(result) {
    if (!activeDialog) return;
    const { root, previousFocus, resolve, cleanup } = activeDialog;
    activeDialog = null;
    cleanup?.();
    root.hidden = true;
    root.remove();
    document.body.classList.remove('valora-dialog-open');
    previousFocus?.focus?.();
    resolve(result);
  }

  function open(options) {
    if (activeDialog) close(false);
    const config = Object.assign({ title: 'Confirmar ação', description: 'Confirme se deseja continuar com esta operação.', confirmText: 'Continuar', cancelText: 'Cancelar', cancelable: true, tone: 'info' }, options);
    config.title = String(config.title || '').trim() || 'Confirmar ação';
    config.description = String(config.description || '').trim() || 'Confirme se deseja continuar com esta operação.';
    const previousFocus = document.activeElement;
    const root = document.createElement('div');
    root.className = 'valora-dialog-layer';
    root.innerHTML = `<div class="valora-dialog-backdrop" data-dialog-cancel></div>
      <section class="valora-dialog valora-dialog--${config.tone}" role="dialog" aria-modal="true" aria-labelledby="valora-dialog-title" aria-describedby="valora-dialog-description">
        <header><h2 id="valora-dialog-title"></h2>${config.cancelable ? '<button class="valora-dialog-close" type="button" data-dialog-cancel aria-label="Fechar" title="Fechar">&times;</button>' : ''}</header>
        <div id="valora-dialog-description" class="valora-dialog-content"></div>
        <footer>${config.cancelable ? '<button class="btn btn-outline-secondary" type="button" data-dialog-cancel></button>' : ''}<button class="btn btn-primary" type="button" data-dialog-confirm></button></footer>
      </section>`;
    root.querySelector('#valora-dialog-title').textContent = config.title;
    root.querySelector('#valora-dialog-description').textContent = config.description;
    root.querySelector('[data-dialog-confirm]').textContent = config.confirmText;
    const cancelButton = root.querySelector('footer [data-dialog-cancel]');
    if (cancelButton) cancelButton.textContent = config.cancelText;
    root.hidden = false;
    document.body.append(root);
    document.body.classList.add('valora-dialog-open');

    return new Promise(resolve => {
      let settled = false;
      const finish = value => { if (settled) return; settled = true; close(value); };
      const onClick = event => {
        if (event.target.closest('[data-dialog-confirm]')) finish(true);
        else if (config.cancelable && event.target.matches('[data-dialog-cancel]')) finish(false);
      };
      const onKeydown = event => {
        if (event.key === 'Escape' && config.cancelable) { event.preventDefault(); finish(false); }
        if (event.key !== 'Tab') return;
        const items = [...root.querySelectorAll(focusable)];
        if (!items.length) return;
        const first = items[0], last = items[items.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
      };
      activeDialog = { root, previousFocus, resolve, cleanup: () => { root.removeEventListener('click', onClick); document.removeEventListener('keydown', onKeydown); } };
      root.addEventListener('click', onClick);
      document.addEventListener('keydown', onKeydown);
      root.querySelector('[data-dialog-confirm]').focus();
    });
  }

  const message = (tone, options) => open(Object.assign({ tone, cancelable: false, confirmText: 'Entendi' }, typeof options === 'string' ? { description: options } : options));
  window.ValoraDialogs = Object.freeze({
    confirm: options => open(Object.assign({ tone: 'warning' }, options)),
    info: options => message('info', options),
    warning: options => message('warning', options),
    success: options => message('success', options),
    error: options => message('danger', options),
    form: options => open(Object.assign({ tone: 'info' }, options))
  });
  window.Modal = window.ValoraDialogs;
  window.ConfirmModal = Object.freeze({
    ask: (description, options = {}) => window.ValoraDialogs.confirm({ ...options, description })
  });
})();
