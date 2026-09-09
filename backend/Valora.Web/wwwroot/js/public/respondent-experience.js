(() => {
  'use strict';
  const portal = document.querySelector('[data-respondent-portal]');
  if (!portal) return;
  const message = portal.querySelector('[data-save-message]');
  if (message) message.textContent = 'Este formato de convite foi descontinuado. Solicite um novo link seguro à organização responsável.';
})();
