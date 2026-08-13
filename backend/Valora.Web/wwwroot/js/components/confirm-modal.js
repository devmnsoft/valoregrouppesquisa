(() => {
  'use strict';
  let resolver;
  const layer = document.createElement('div');
  layer.className = 'valora-dialog-layer'; layer.hidden = true;
  layer.innerHTML = '<div class="valora-dialog-backdrop" data-confirm-cancel></div><section class="valora-dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirmTitle" aria-describedby="confirmMessage"><header><h2 id="confirmTitle">Confirmar ação</h2><button class="valora-dialog-close" type="button" data-confirm-cancel aria-label="Fechar">×</button></header><div class="valora-dialog-content" id="confirmMessage"></div><footer><button class="valora-button valora-button--secondary" type="button" data-confirm-cancel>Cancelar</button><button class="valora-button valora-button--primary" type="button" data-confirm-accept>Confirmar</button></footer></section>';
  document.body.append(layer);
  const finish = value => { layer.hidden = true; document.body.classList.remove('valora-dialog-open'); resolver?.(value); resolver = null; };
  layer.querySelectorAll('[data-confirm-cancel]').forEach(node => node.addEventListener('click', () => finish(false)));
  layer.querySelector('[data-confirm-accept]').addEventListener('click', () => finish(true));
  layer.addEventListener('keydown', event => { if (event.key === 'Escape') finish(false); });
  window.ConfirmModal = { ask(message, options = {}) { if (resolver) finish(false); layer.querySelector('#confirmTitle').textContent = options.title || 'Confirmar ação'; layer.querySelector('#confirmMessage').textContent = message || 'Confirma esta operação?'; layer.hidden = false; document.body.classList.add('valora-dialog-open'); layer.querySelector('[data-confirm-accept]').focus(); return new Promise(resolve => { resolver = resolve; }); } };
})();
