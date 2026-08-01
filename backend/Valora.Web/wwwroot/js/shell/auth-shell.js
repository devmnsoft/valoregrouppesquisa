(() => {
  'use strict';
  const button = document.querySelector('[data-action="toggle-password"]');
  const password = document.querySelector('#password');
  if (!button || !password) return;
  button.addEventListener('click', () => {
    const reveal = password.type === 'password';
    password.type = reveal ? 'text' : 'password';
    button.setAttribute('aria-label', reveal ? 'Ocultar senha' : 'Mostrar senha');
    button.setAttribute('title', reveal ? 'Ocultar senha' : 'Mostrar senha');
    const use = button.querySelector('svg');
    if (use) use.style.opacity = reveal ? '.55' : '1';
    password.focus();
  });
})();
