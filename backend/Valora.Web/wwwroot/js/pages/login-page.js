document.addEventListener('DOMContentLoaded', () => {
  const form = document.querySelector('[data-page="login-page"] form');
  if (!form) return;
  form.email.focus();
  form.addEventListener('submit', async event => {
    event.preventDefault();
    const errorBox = document.querySelector('.error-state');
    const message = errorBox.querySelector('[data-error-message]');
    const reference = errorBox.querySelector('[data-error-reference]');
    errorBox.classList.add('d-none');
    if (!form.checkValidity()) { form.classList.add('was-validated'); return; }
    const button = form.querySelector('button[type="submit"]');
    const label = button.querySelector('[data-submit-label]');
    const spinner = button.querySelector('[data-submit-spinner]');
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    label.textContent = 'Entrando…';
    spinner.classList.remove('d-none');
    try {
      await AuthApi.login({ email: form.email.value.trim(), password: form.password.value });
      const authenticated = await AuthApi.me();
      if (!authenticated?.user) throw new Error('A sessão segura não pôde ser confirmada.');
      window.location.assign('/Dashboard');
    } catch (error) {
      form.password.value = '';
      message.textContent = error.status === 401 ? 'Confira seu e-mail e sua senha e tente novamente.' : 'Encontramos uma inconsistência ao processar sua solicitação. Tente novamente; se continuar, informe a referência ao suporte.';
      if (error.correlationId) { reference.textContent = `Referência: ${error.correlationId}`; reference.hidden = false; } else { reference.hidden = true; }
      errorBox.classList.remove('d-none');
      errorBox.focus();
    } finally {
      button.disabled = false;
      button.removeAttribute('aria-busy');
      label.textContent = 'Entrar';
      spinner.classList.add('d-none');
    }
  });
});
