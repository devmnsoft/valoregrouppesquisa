(() => {
  const root = document.querySelector('[data-integrations-page]');
  if (!root) return;
  const grid = root.querySelector('[data-integration-grid]');
  const loading = root.querySelector('[data-loading]');
  const error = root.querySelector('[data-error]');
  const locked = root.querySelector('[data-enterprise-locked]');
  const template = document.querySelector('#integration-card-template');
  const descriptions = {
    'public-api': 'Acesso controlado por escopos, expiração e chave exibida uma única vez.',
    webhooks: 'Eventos assinados, entregas rastreáveis e retentativas sem interromper o fluxo principal.',
    powerbi: 'Dataset preparado e agregado; conexão direta somente quando houver credencial real.',
    exports: 'Arquivos autorizados sem respostas individuais ou grupos abaixo da amostra mínima.',
    smtp: 'Canal transacional configurado pelo ambiente, com falhas operacionais rastreáveis.',
    'certificates-pdf': 'Geração e validação de certificados e documentos institucionais.',
    'assisted-import': 'Importação CSV com validação, preview e dry-run antes da confirmação.'
  };
  const routes = { 'public-api': '/Integrations/ApiKeys', webhooks: '/Integrations/Webhooks', powerbi: '/Integrations/PowerBI', 'assisted-import': '/Administration/Imports' };

  fetch('/bff/integrations', { headers: { Accept: 'application/json' } })
    .then(async response => {
      if (response.status === 403) { locked.classList.remove('d-none'); return []; }
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      return response.json();
    })
    .then(items => items.forEach(item => {
      const card = template.content.cloneNode(true);
      card.querySelector('[data-name]').textContent = item.name;
      const badge = card.querySelector('[data-status]');
      badge.textContent = item.status === 'configured' ? 'Configurado' : item.status === 'disabled' ? 'Desativado' : 'Não configurado';
      badge.classList.add(item.status === 'configured' ? 'text-bg-success' : item.status === 'disabled' ? 'text-bg-secondary' : 'text-bg-warning');
      card.querySelector('[data-description]').textContent = descriptions[item.code] || 'Integração administrada com isolamento por organização.';
      card.querySelector('[data-plan]').textContent = item.requiredPlan || 'Enterprise';
      card.querySelector('[data-last-run]').textContent = item.lastExecutionAt ? new Date(item.lastExecutionAt).toLocaleString('pt-BR') : 'Sem execução';
      const open = card.querySelector('[data-open]');
      open.href = routes[item.code] || `/Integrations?connector=${encodeURIComponent(item.code)}`;
      grid.appendChild(card);
    }))
    .catch(() => error.classList.remove('d-none'))
    .finally(() => loading.classList.add('d-none'));
})();
