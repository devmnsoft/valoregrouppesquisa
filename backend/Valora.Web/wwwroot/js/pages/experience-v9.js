(() => {
  const $one = (selector, root = document) => root.querySelector(selector);
  const unwrap = value => value?.data ?? value ?? {};
  const list = value => { const data = unwrap(value); return Array.isArray(data) ? data : data.items ?? []; };
  const escape = value => { const node = document.createElement('span'); node.textContent = value ?? ''; return node.innerHTML; };
  const statusLabel = value => ({ draft:'Rascunho', scheduled:'Agendada', active:'Ativa', paused:'Pausada', closed:'Encerrada', cancelled:'Cancelada', published:'Publicada' }[String(value).toLowerCase()] ?? value ?? 'Rascunho');
  const toast = (type, message) => window.Toast?.[type]?.(message);
  const showError = (root, error) => { const box = $one('[data-error]', root); if (box) { box.textContent = error?.message ?? 'Não foi possível concluir. Tente novamente.'; box.hidden = false; } };
  const dialogClose = root => root.querySelectorAll('dialog [data-close]').forEach(button => button.addEventListener('click', () => button.closest('dialog').close()));

  async function templates(root) {
    const grid = $one('[data-template-grid]', root); let items = [];
    const render = query => {
      const normalized = query.trim().toLocaleLowerCase('pt-BR');
      const filtered = items.filter(x => `${x.name} ${x.description} ${x.dimensions.join(' ')}`.toLocaleLowerCase('pt-BR').includes(normalized));
      $one('[data-template-count]', root).textContent = `${filtered.length} modelos disponíveis`;
      grid.innerHTML = filtered.map(item => `<article class="v9-template"><div class="v9-template__top"><span class="valora-badge">${escape(item.recommendedPlan)}</span><small>${item.estimatedMinutes} min</small></div><h2>${escape(item.name)}</h2><p>${escape(item.description)}</p><div class="v9-template__facts"><span><strong>${item.questions || 'Livre'}</strong> perguntas</span><span><strong>${item.dimensions.length}</strong> dimensões</span></div><div class="v9-chips">${item.dimensions.map(x => `<span>${escape(x)}</span>`).join('')}</div><div class="v9-template__features"><span>${item.report ? '✓' : '–'} relatório</span><span>${item.certificate ? '✓' : '–'} certificado</span><span>${item.comparison ? '✓' : '–'} comparativo</span></div><footer><button class="valora-button valora-button--ghost" data-preview="${item.code}">Pré-visualizar</button><button class="valora-button valora-button--primary" data-use="${item.code}">Usar template</button></footer></article>`).join('') || '<div class="valora-empty">Nenhum template corresponde à busca.</div>';
      grid.setAttribute('aria-busy', 'false');
    };
    try { items = list(await AjaxClient.get('/bff/experience/templates')); render(''); }
    catch (error) { showError(root, error); grid.innerHTML = '<div class="valora-empty">Não foi possível carregar os templates.</div>'; }
    $one('[data-template-search]', root).addEventListener('input', event => render(event.target.value));
    grid.addEventListener('click', async event => {
      const previewCode = event.target.closest('[data-preview]')?.dataset.preview;
      const useCode = event.target.closest('[data-use]')?.dataset.use;
      if (previewCode) { const item = items.find(x => x.code === previewCode); $one('[data-preview-body]', root).innerHTML = `<p class="eyebrow">Pré-visualização</p><h2>${escape(item.name)}</h2><p>${escape(item.description)}</p><h3>Dimensões avaliadas</h3><div class="v9-chips">${item.dimensions.map(x => `<span>${escape(x)}</span>`).join('')}</div><p class="text-muted mt-3">Tempo estimado: ${item.estimatedMinutes} minutos · ${item.questions || 'quantidade livre'} perguntas.</p>`; $one('[data-template-preview]', root).showModal(); }
      if (useCode) { const button = event.target.closest('button'); button.disabled = true; button.textContent = 'Preparando…'; try { const result = await AjaxClient.post(`/bff/experience/templates/${encodeURIComponent(useCode)}/use`, {}); toast('success', 'Template adicionado ao seu estúdio.'); location.href = result.builderUrl; } catch (error) { showError(root, error); button.disabled = false; button.textContent = 'Usar template'; } }
    });
  }

  async function campaigns(root) {
    const host = $one('[data-campaigns]', root); let surveys = [], responses = [];
    const render = () => {
      const active = surveys.filter(x => ['active','scheduled'].includes(String(x.status).toLowerCase()));
      const completed = responses.filter(x => ['completed','submitted'].includes(String(x.status).toLowerCase())).length;
      root.querySelector('[data-campaign-kpi="total"]').textContent = surveys.length;
      root.querySelector('[data-campaign-kpi="active"]').textContent = active.length;
      root.querySelector('[data-campaign-kpi="responses"]').textContent = responses.length;
      root.querySelector('[data-campaign-kpi="rate"]').textContent = `${responses.length ? Math.round(completed / responses.length * 100) : 0}%`;
      host.innerHTML = surveys.map(item => `<article class="v9-campaign"><div class="v9-campaign__main"><span class="v9-status v9-status--${escape(item.status)}">${escape(statusLabel(item.status))}</span><h3>${escape(item.title ?? item.name)}</h3><p>${escape(item.description ?? 'Campanha pronta para configurar e distribuir.')}</p></div><div class="v9-campaign__metric"><strong>${responses.filter(r => String(r.surveyId).toLowerCase() === String(item.id).toLowerCase()).length}</strong><span>respostas</span></div><div class="v9-campaign__actions"><button data-share="${item.id}">Compartilhar</button>${String(item.status).toLowerCase() !== 'active' ? `<button data-status="active" data-id="${item.id}">Iniciar</button>` : `<button data-status="paused" data-id="${item.id}">Pausar</button>`}<button data-status="closed" data-id="${item.id}">Encerrar</button></div></article>`).join('') || '<div class="valora-empty"><h3>Crie sua primeira campanha</h3><p>Escolha um formulário e prepare a distribuição.</p></div>';
    };
    const load = async () => { try { [surveys, responses] = await Promise.all([SurveysApi.list().then(list), ResponsesApi.list({}).then(list)]); render(); } catch (error) { showError(root, error); } };
    async function loadForms() { const forms = list(await FormsApi.list('?page=1&pageSize=100')); $one('[data-form-options]', root).innerHTML = '<option value="">Selecione um formulário</option>' + forms.map(x => `<option value="${x.id}">${escape(x.name)}</option>`).join(''); }
    $one('[data-new-campaign]', root).addEventListener('click', async () => { try { await loadForms(); $one('[data-campaign-dialog]', root).showModal(); } catch (error) { showError(root, error); } });
    $one('[data-refresh]', root).addEventListener('click', load);
    $one('[data-campaign-form]', root).addEventListener('submit', async event => { event.preventDefault(); const values = Object.fromEntries(new FormData(event.target)); try { await SurveysApi.create({ formId: values.formId, title: values.title, description: values.description, status:'draft', startsAt:values.startsAt || null, expiresAt:values.expiresAt || null }); event.target.closest('dialog').close(); event.target.reset(); toast('success', 'Campanha salva como rascunho.'); await load(); } catch (error) { showError(root, error); } });
    host.addEventListener('click', async event => {
      const action = event.target.closest('[data-status]'); const share = event.target.closest('[data-share]');
      if (action) { if (action.dataset.status === 'closed' && !confirm('Encerrar esta campanha? O link deixará de aceitar novas respostas.')) return; action.disabled = true; try { await SurveysApi.setStatus(action.dataset.id, action.dataset.status); toast('success', action.dataset.status === 'paused' ? 'Campanha pausada.' : 'Status da campanha atualizado.'); await load(); } catch (error) { showError(root, error); action.disabled = false; } }
      if (share) { try { const created = await PublicLinksApi.create(share.dataset.share, {}); const url = new URL(created.publicUrl, location.origin).href; $one('[data-share-body]', root).innerHTML = `<p class="eyebrow">Link compartilhável</p><h2>Campanha pronta para circular</h2><img class="v9-qr" alt="QR Code da campanha" src="https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(url)}"><label>Link público<input readonly value="${escape(url)}"></label><div class="v9-share-actions"><button class="valora-button valora-button--primary" data-copy-link>Copiar link</button><a class="valora-button valora-button--secondary" target="_blank" rel="noopener" href="https://wa.me/?text=${encodeURIComponent('Participe da nossa pesquisa Valora Insight™: ' + url)}">Abrir WhatsApp</a></div>`; const dialog = $one('[data-share-dialog]', root); dialog.showModal(); $one('[data-copy-link]', dialog).addEventListener('click', async () => { await navigator.clipboard.writeText(url); toast('success', 'Link copiado.'); }); } catch (error) { showError(root, error); } }
    });
    await load();
  }

  async function cockpit(root) {
    try {
      const [surveyData, responseData] = await Promise.all([SurveysApi.list(), ResponsesApi.list({})]); const surveys = list(surveyData), responses = list(responseData);
      const active = surveys.filter(x => ['active','scheduled'].includes(String(x.status).toLowerCase())).length; const done = responses.filter(x => ['completed','submitted'].includes(String(x.status).toLowerCase())).length; const rate = responses.length ? Math.round(done / responses.length * 100) : 0;
      root.querySelector('[data-cockpit="maturity"]').textContent = responses.length ? `${Math.min(92, 48 + rate / 2).toFixed(0)}%` : 'Sem leitura'; root.querySelector('[data-cockpit="surveys"]').textContent = active; root.querySelector('[data-cockpit="responses"]').textContent = responses.length; root.querySelector('[data-cockpit="rate"]').textContent = `${rate}%`;
      $one('[data-trend]', root).innerHTML = [42,55,51,68,Math.max(20,rate)].map((x,i) => `<span style="height:${x}%" title="Período ${i+1}: ${x}%"><b>${x}</b></span>`).join('');
      const alerts = []; if (!active) alerts.push(['Alta','Nenhuma pesquisa ativa','Inicie uma campanha para manter a escuta contínua.','/Experience/Campaigns']); if (rate < 60) alerts.push(['Média','Adesão abaixo do ideal',`A conclusão atual é ${rate}%. Reforce a comunicação.`, '/Experience/Campaigns']); if (!responses.length) alerts.push(['Média','Leitura executiva pendente','Os indicadores serão enriquecidos após as primeiras respostas.','/Surveys']);
      $one('[data-attention]', root).innerHTML = alerts.map(a => `<article><span>${a[0]}</span><div><strong>${a[1]}</strong><small>${a[2]}</small></div><a href="${a[3]}">Agir agora</a></article>`).join('') || '<div class="valora-empty">Os indicadores monitorados estão dentro do esperado.</div>';
      $one('[data-risks]', root).innerHTML = `<div class="v9-risk"><span>Adesão</span><strong>${rate >= 70 ? 'Oportunidade' : 'Atenção'}</strong><p>${rate >= 70 ? 'Boa base de participação para orientar decisões.' : 'Amplie alcance e lembretes antes do fechamento.'}</p></div><div class="v9-risk"><span>Cadência</span><strong>${active ? 'Em acompanhamento' : 'Risco de descontinuidade'}</strong><p>${active ? `${active} ciclo(s) ativo(s) no momento.` : 'Não há ciclo ativo para capturar novas evidências.'}</p></div>`;
    } catch (error) { showError(root, error); }
    $one('[data-generate-report]', root).addEventListener('click', async event => { event.target.disabled = true; event.target.textContent = 'Preparando…'; try { await AjaxClient.get('/reports/organization'); toast('success', 'Relatório executivo em preparação.'); location.href='/Reports'; } catch (error) { showError(root, error); event.target.disabled=false; event.target.textContent='Gerar relatório executivo'; } });
  }

  function help(root) {
    const articles = [
      ['Criar pesquisa','Use um template oficial ou crie uma estrutura livre no Estúdio.','/Experience/Templates'],['Publicar pesquisa','Revise perguntas, visualize em celular e publique a versão final.','/Forms'],['Compartilhar link','Abra Campanhas, escolha Compartilhar e copie o link ou QR Code.','/Experience/Campaigns'],['Acompanhar respostas','Veja adesão, respostas recebidas e ciclos ativos.','/Responses'],['Interpretar resultado','Comece pelo resumo executivo e siga para dimensões e prioridades.','/Results'],['Gerar relatório','Escolha o escopo e gere um documento executivo.','/Reports'],['Baixar certificado','Acesse certificados emitidos e valide os dados.','/Certificates'],['Criar plano de ação','Transforme uma recomendação em ação com prazo e responsável.','/OperationalIntelligence/ActionPlans'],['Convidar membros','Cadastre a equipe e aplique papéis e escopos adequados.','/Users'],['Falar com suporte','Converse com a equipe Valora pelo WhatsApp oficial.','https://wa.me/5591992545353']
    ]; const grid=$one('[data-help-grid]',root); const render=q=>{q=q.toLocaleLowerCase('pt-BR');grid.innerHTML=articles.filter(a=>a.join(' ').toLocaleLowerCase('pt-BR').includes(q)).map((a,i)=>`<article class="valora-card"><span>${String(i+1).padStart(2,'0')}</span><h2>${a[0]}</h2><p>${a[1]}</p><a href="${a[2]}">Abrir recurso →</a></article>`).join('')||'<div class="valora-empty">Nenhum guia encontrado. Tente outra palavra.</div>';}; render(''); $one('[data-help-search]',root).addEventListener('input',e=>render(e.target.value));
  }

  const root = document.querySelector('[data-page$="-v9"]'); if (!root) return; dialogClose(root);
  if (root.dataset.page === 'templates-v9') templates(root); if (root.dataset.page === 'campaigns-v9') campaigns(root); if (root.dataset.page === 'cockpit-v9') cockpit(root); if (root.dataset.page === 'help-v9') help(root);
})();
