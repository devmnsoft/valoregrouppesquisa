document.addEventListener('DOMContentLoaded', () => {
  const form = document.querySelector('[data-page="login-page"] form');
  if (!form) return;
  const passwordToggle = document.querySelector('[data-action="toggle-password"]');
  const identifier = form.email;
  const identifierIsValid = value => {
    const normalized = value.trim();
    if (normalized.includes('@')) return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalized);
    const digits = normalized.replace(/\D/g, '');
    return digits.length === 11 || digits.length === 14;
  };
  identifier.addEventListener('input', () => identifier.setCustomValidity(identifierIsValid(identifier.value) ? '' : 'Informe um CPF, CNPJ ou e-mail válido.'));
  form.email.focus({ preventScroll: true });
  passwordToggle?.addEventListener('click', () => {
    const reveal = form.password.type === 'password';
    form.password.type = reveal ? 'text' : 'password';
    passwordToggle.setAttribute('aria-label', reveal ? 'Ocultar senha' : 'Mostrar senha');
    passwordToggle.setAttribute('aria-pressed', String(reveal));
    passwordToggle.querySelector('span').textContent = reveal ? 'Ocultar' : 'Mostrar';
    form.password.focus();
  });
  form.addEventListener('submit', async event => {
    event.preventDefault();
    const errorBox = document.querySelector('.auth-error');
    const message = errorBox.querySelector('[data-error-message]');
    const reference = errorBox.querySelector('[data-error-reference]');
    errorBox.classList.add('d-none');
    identifier.setCustomValidity(identifierIsValid(identifier.value) ? '' : 'Informe um CPF, CNPJ ou e-mail válido.');
    if (!form.checkValidity()) { form.classList.add('was-validated'); return; }
    const button = form.querySelector('button[type="submit"]');
    const label = button.querySelector('[data-submit-label]');
    const spinner = button.querySelector('[data-submit-spinner]');
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    label.textContent = 'Entrando...';
    spinner.classList.remove('d-none');
    try {
      await AuthApi.login({ email: form.email.value.trim(), password: form.password.value, rememberMe: form.rememberMe.checked });
      const authenticated = await AuthApi.me();
      if (!authenticated?.user) throw new Error('A sessão segura não pôde ser confirmada.');
      window.location.assign('/Dashboard');
    } catch (error) {
      form.password.value = '';
      message.textContent = error.status === 401 ? 'Identificador ou senha inválidos. Confira os dados e tente novamente.' : error.message || 'Não foi possível concluir o acesso agora. Tente novamente. Se continuar, informe a referência ao suporte.';
      if (error.correlationId) { reference.textContent = `Referência: ${error.correlationId}`; reference.hidden = false; } else { reference.hidden = true; }
      errorBox.classList.remove('d-none');
      errorBox.focus();
    } finally {
      button.disabled = false;
      button.removeAttribute('aria-busy');
      label.textContent = 'Entrar com segurança';
      spinner.classList.add('d-none');
    }
  });
});
