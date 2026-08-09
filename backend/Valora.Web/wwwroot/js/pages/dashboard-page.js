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
    renderJourney(surveys, responses, usage);
    renderAttention(surveys, responses, completionRate);
  }

  function renderJourney(surveys, responses, usage) {
    const host = root.querySelector('[data-onboarding]');
    if (!host || localStorage.getItem('valora.onboarding.hidden') === 'true') { if (host) host.hidden = true; return; }
    const steps = [
      ['Configurar empresa', true, '/Organization', 'Revise marca e dados institucionais.'],
      ['Cadastrar unidades e setores', Boolean(usage.units || usage.departments), '/Organization#org-structure', 'Estruture análises com escopo seguro.'],
      ['Convidar equipe', Number(usage.activeUsers || 0) > 1, '/Users', 'Distribua responsabilidades.'],
      ['Escolher template', surveys.length > 0, '/Experience/Templates', 'Comece com um modelo oficial.'],
      ['Publicar pesquisa', surveys.some(x => ['active','published'].includes(String(x.status).toLowerCase())), '/Forms', 'Revise e publique a versão.'],
      ['Acompanhar respostas', responses.length > 0, '/Responses', 'Monitore a adesão do ciclo.'],
      ['Gerar relatório', responses.length > 0, '/Reports', 'Compartilhe a leitura executiva.'],
      ['Criar plano de ação', false, '/OperationalIntelligence/ActionPlans', 'Transforme evidências em execução.']
    ];
    const progress = Math.round(steps.filter(x => x[1]).length / steps.length * 100);
    root.querySelector('[data-onboarding-progress]').textContent = `${progress}% concluído`; root.querySelector('[data-onboarding-bar]').style.width = `${progress}%`;
    root.querySelector('[data-onboarding-items]').innerHTML = steps.map(step => `<a class="${step[1] ? 'is-complete' : ''}" href="${step[2]}"><span>${step[1] ? '✓' : '○'}</span><div><strong>${step[0]}</strong><small>${step[3]}</small></div></a>`).join('');
  }

  function renderAttention(surveys, responses, completionRate) {
    const host = root.querySelector('[data-dashboard-attention]'); if (!host) return; const items = [];
    if (!surveys.some(x => String(x.status).toLowerCase() === 'active')) items.push(['Alta','Nenhuma pesquisa ativa','Inicie uma campanha para manter a escuta contínua.','/Experience/Campaigns']);
    if (completionRate < 60) items.push(['Média','Adesão pede atenção',`A taxa atual é ${completionRate}%. Compartilhe um lembrete.`, '/Experience/Campaigns']);
    if (!responses.length) items.push(['Informativa','Resultado executivo pendente','Receba as primeiras respostas para ativar a leitura.','/Surveys']);
    host.innerHTML = items.map(x => `<article><span>${x[0]}</span><div><strong>${x[1]}</strong><small>${x[2]}</small></div><a href="${x[3]}">Resolver</a></article>`).join('') || '<div class="valora-empty">Nenhuma prioridade crítica no momento.</div>';
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

  root.querySelector('[data-hide-onboarding]')?.addEventListener('click', () => { localStorage.setItem('valora.onboarding.hidden','true'); root.querySelector('[data-onboarding]').hidden = true; window.Toast?.success?.('Jornada guiada ocultada.'); });
  load();
})();
