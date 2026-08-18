(() => {
  const root = document.querySelector('[data-page="environment-status-page"]');
  if (!root) return;

  const labels = { api: 'API', database: 'PostgreSQL', migration: 'Schema', email: 'E-mail / SMTP', storage: 'Armazenamento', version: 'Versão e build', config: 'Configuração' };
  const statusLabel = status => status === 'healthy' ? 'Saudável' : status === 'critical' ? 'Crítico' : 'Atenção';
  const setCard = (name, status) => { const node = root.querySelector(`[data-card="${name}"]`); if (node) node.textContent = statusLabel(status); };

  function safeDetail(check) {
    const payload = check.payload || {};
    if (payload.email === 'not_configured') return 'Este recurso ainda não está configurado neste ambiente.';
    if (payload.postgresConfigured === false) return 'A conexão obrigatória com PostgreSQL não está configurada.';
    if (payload.database === 'ok') return 'Conexão validada com sucesso.';
    if (payload.version) return `Versão ${payload.version}; build ${payload.build || 'local'}.`;
    return check.message || 'Dependência respondeu sem expor dados sensíveis.';
  }

  function render(data) {
    const checks = Array.isArray(data.checks) ? data.checks : [];
    setCard('web', 'healthy');
    setCard('api', checks.some(x => x.name === 'api' && x.status === 'healthy') ? 'healthy' : 'critical');
    setCard('database', checks.find(x => x.name === 'database')?.status || 'attention');
    const config = checks.find(x => x.name === 'config');
    setCard('config', config?.payload?.postgresConfigured === false ? 'critical' : config?.status || 'attention');
    root.querySelector('[data-environment]').textContent = `${data.environment || 'Ambiente não identificado'} · versão ${data.version || 'local'} · referência ${data.correlationId}`;
    root.querySelector('[data-items]').replaceChildren(...checks.map(check => {
      const row = document.createElement('tr');
      [labels[check.name] || check.name, statusLabel(check.status), safeDetail(check), check.correlationId || data.correlationId].forEach(value => {
        const cell = document.createElement('td'); cell.textContent = value || '—'; row.append(cell);
      });
      return row;
    }));
    const warning = root.querySelector('[data-warning]');
    const hasPending = checks.some(x => x.status !== 'healthy' || x.payload?.email === 'not_configured' || x.payload?.postgresConfigured === false);
    warning.hidden = !hasPending;
    warning.textContent = 'Há dependências indisponíveis ou não configuradas. Os módulos disponíveis continuam seguros para uso.';
  }

  async function load() {
    const error = root.querySelector('[data-error]'); error.hidden = true;
    try {
      window.Loading?.show('Verificando saúde operacional…');
      const response = await fetch('/bff/system-health', { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
      const payload = await response.json();
      if (!response.ok) throw payload;
      render(payload);
    } catch (exception) {
      const correlationId = exception?.correlationId || exception?.details?.correlationId;
      error.textContent = `Não foi possível concluir a verificação. Os dados originais permanecem preservados.${correlationId ? ` Referência: ${correlationId}` : ''}`;
      error.hidden = false;
      ['web', 'api', 'database', 'config'].forEach(name => setCard(name, 'critical'));
    } finally { window.Loading?.hide(); }
  }

  root.querySelector('[data-refresh]')?.addEventListener('click', load);
  load();
})();
