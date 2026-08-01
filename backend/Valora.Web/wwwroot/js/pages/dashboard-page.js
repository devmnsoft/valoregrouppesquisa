(() => {
  const root = document.querySelector('[data-page="dashboard-page"]');
  if (!root) return;

  const unwrap = value => value?.data ?? value ?? {};
  const arrayOf = value => {
    const data = unwrap(value);
    return Array.isArray(data) ? data : Array.isArray(data.items) ? data.items : [];
  };
  const setText = (selector, value) => {
    const element = root.querySelector(selector);
    if (element) element.textContent = value;
  };
  const number = value => new Intl.NumberFormat('pt-BR').format(Number(value) || 0);

  function renderKpis({ surveys, responses, usage }) {
    const activeSurveys = surveys.filter(item => ['active', 'scheduled'].includes(String(item.status).toLowerCase())).length;
    const completed = responses.filter(item => ['completed', 'submitted'].includes(String(item.status).toLowerCase())).length;
    const completionRate = responses.length ? Math.round((completed / responses.length) * 100) : 0;
    setText('[data-kpi="responses"]', number(responses.length));
    setText('[data-kpi="activeSurveys"]', number(activeSurveys));
    setText('[data-kpi="completionRate"]', `${completionRate}%`);
    setText('[data-kpi="activeUsers"]', number(usage.activeUsers ?? usage.usersActive ?? 0));
  }

  function renderActivity(items) {
    const body = root.querySelector('[data-items]');
    const empty = root.querySelector('[data-empty-activities]');
    const table = root.querySelector('[data-activity-table]');
    if (!items.length) return;
    empty.hidden = true;
    table.hidden = false;
    body.replaceChildren(...items.slice(0, 8).map(item => {
      const row = document.createElement('tr');
      [item.name ?? item.title ?? 'Atividade', item.status ?? 'Registrada', item.description ?? item.email ?? ''].forEach(value => {
        const cell = document.createElement('td');
        cell.textContent = String(value);
        row.append(cell);
      });
      return row;
    }));
  }

  async function load() {
    const error = root.querySelector('[data-error]');
    error.hidden = true;
    try {
      window.Loading?.show('Atualizando leitura executiva…');
      const [surveysResult, responsesResult, usageResult] = await Promise.allSettled([
        SurveysApi.list(), ResponsesApi.list({}), UsageApi.usage()
      ]);
      const surveys = surveysResult.status === 'fulfilled' ? arrayOf(surveysResult.value) : [];
      const responses = responsesResult.status === 'fulfilled' ? arrayOf(responsesResult.value) : [];
      const usage = usageResult.status === 'fulfilled' ? unwrap(usageResult.value) : {};
      renderKpis({ surveys, responses, usage });
      renderActivity([...surveys, ...responses]);
      setText('[data-last-update]', `Atualizado em ${new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date())}`);
    } catch (exception) {
      error.textContent = 'Não foi possível atualizar a leitura executiva. Tente novamente em instantes.';
      error.hidden = false;
    } finally {
      window.Loading?.hide();
    }
  }

  load();
})();
