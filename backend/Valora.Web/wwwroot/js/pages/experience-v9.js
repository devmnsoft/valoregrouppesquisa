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
    const host = $one('[data-campaigns]', root); let items = [];
    const actions = status => ({ draft:['schedule','send','cancel'], scheduled:['send','cancel'], sending:['pause','close','cancel'], active:['pause','close','cancel'], paused:['resume','close','cancel'], failed:['send','cancel'] }[status] ?? []);
    const labels = { schedule:'Agendar', send:'Enviar', pause:'Pausar', resume:'Retomar', close:'Encerrar', cancel:'Cancelar' };
    const render = () => {
      const active = items.filter(x => ['sending','active','scheduled'].includes(String(x.status).toLowerCase()));
      const responses = items.reduce((sum, x) => sum + Number(x.responseCount || 0), 0);
      const recipients = items.reduce((sum, x) => sum + Number(x.recipientCount || 0), 0);
      root.querySelector('[data-campaign-kpi="total"]').textContent = items.length;
      root.querySelector('[data-campaign-kpi="active"]').textContent = active.length;
      root.querySelector('[data-campaign-kpi="responses"]').textContent = responses;
      root.querySelector('[data-campaign-kpi="rate"]').textContent = recipients ? `${Math.round(responses / recipients * 100)}%` : 'Dados insuficientes';
      host.innerHTML = items.map(item => { const status = String(item.status).toLowerCase(); return `<article class="v9-campaign"><div class="v9-campaign__main"><span class="v9-status v9-status--${escape(status)}">${escape(statusLabel(status))}</span><h3>${escape(item.name)}</h3><p>${escape(item.message)}</p><small>${escape(item.channel)} · ${item.sentCount}/${item.recipientCount} enviados · ${item.failedCount} falhas</small></div><div class="v9-campaign__metric"><strong>${item.responseCount}</strong><span>respostas (${Number(item.completionRate || 0).toFixed(1)}%)</span></div><div class="v9-campaign__actions">${item.publicUrl ? `<button data-share="${item.surveyId}">Compartilhar</button>` : ''}${actions(status).map(action => `<button data-action="${action}" data-id="${item.surveyId}">${labels[action]}</button>`).join('')}${item.failedCount ? `<button data-action="resendFailures" data-id="${item.surveyId}">Reenviar falhas</button>` : ''}</div></article>`; }).join('') || '<div class="valora-empty"><h3>Crie sua primeira campanha</h3><p>Escolha uma pesquisa publicada e prepare a distribuição.</p></div>';
    };
    const load = async () => { try { items = list(await DiagnosticCampaignsApi.list()); render(); } catch (error) { showError(root, error); } };
    async function loadSurveys() { const surveys = list(await SurveysApi.list()).filter(x => String(x.status).toLowerCase() === 'published'); $one('[data-survey-options]', root).innerHTML = '<option value="">Selecione uma pesquisa publicada</option>' + surveys.map(x => `<option value="${x.id}">${escape(x.title ?? x.name)}</option>`).join(''); }
    $one('[data-new-campaign]', root).addEventListener('click', async () => { try { await loadSurveys(); $one('[data-campaign-dialog]', root).showModal(); } catch (error) { showError(root, error); } });
    $one('[data-refresh]', root).addEventListener('click', load);
    $one('[data-campaign-form]', root).addEventListener('submit', async event => { event.preventDefault(); const data = new FormData(event.target); const emails = String(data.get('recipients') || '').split(/[\n,;]+/).map(x => x.trim()).filter(Boolean); const payload = { name:data.get('name'), message:data.get('message'), audience:data.get('audience') || null, channel:data.get('channel'), subject:data.get('subject') || null, startsAt:data.get('startsAt') || null, endsAt:data.get('endsAt') || null, targetParticipationRate:data.get('targetParticipationRate') ? Number(data.get('targetParticipationRate')) : null, recipients:emails.map(email => ({ email, hasConsent:data.has('hasConsent'), hasLegalBasis:data.has('hasLegalBasis') })) }; try { await DiagnosticCampaignsApi.create(data.get('surveyId'), payload); event.target.closest('dialog').close(); event.target.reset(); toast('success', 'Campanha salva como rascunho.'); await load(); } catch (error) { showError(root, error); } });
    host.addEventListener('click', async event => {
      const action = event.target.closest('[data-action]'); const share = event.target.closest('[data-share]');
      if (action) { const critical = ['pause','close','cancel'].includes(action.dataset.action); if (critical && !await window.ConfirmModal.ask(`${labels[action.dataset.action]} esta campanha? Os dados já coletados serão preservados; campanhas encerradas ou canceladas não podem ser reativadas.`, { title:labels[action.dataset.action], confirmText:labels[action.dataset.action] })) return; action.disabled = true; try { await DiagnosticCampaignsApi[action.dataset.action](action.dataset.id); toast('success', 'Campanha atualizada.'); await load(); } catch (error) { showError(root, error); action.disabled = false; } }
      if (share) { const item = items.find(x => String(x.surveyId) === share.dataset.share); const url = item?.publicUrl && new URL(item.publicUrl, location.origin).href; if (!url) return; $one('[data-share-body]', root).innerHTML = `<p class="eyebrow">Link compartilhável</p><h2>Campanha pronta para circular</h2><label>Link público<input readonly value="${escape(url)}"></label><div class="v9-share-actions"><button class="valora-button valora-button--primary" data-copy-link>Copiar link</button><a class="valora-button valora-button--secondary" target="_blank" rel="noopener" href="https://wa.me/?text=${encodeURIComponent('Participe da nossa pesquisa Valora Insight™: ' + url)}">Abrir WhatsApp</a></div>`; const dialog = $one('[data-share-dialog]', root); dialog.showModal(); $one('[data-copy-link]', dialog).addEventListener('click', async () => { await navigator.clipboard.writeText(url); toast('success', 'Link copiado.'); }, { once:true }); }
    });
    await load();
  }

  async function cockpit(root) {
    try {
      const [surveyData, responseData] = await Promise.all([SurveysApi.list(), ResponsesApi.list({})]); const surveys = list(surveyData), responses = list(responseData);
      const active = surveys.filter(x => ['active','scheduled'].includes(String(x.status).toLowerCase())).length; const done = responses.filter(x => ['completed','submitted'].includes(String(x.status).toLowerCase())).length; const rate = responses.length ? Math.round(done / responses.length * 100) : 0;
      root.querySelector('[data-cockpit="maturity"]').textContent = 'Dados insuficientes'; root.querySelector('[data-cockpit="surveys"]').textContent = active; root.querySelector('[data-cockpit="responses"]').textContent = responses.length; root.querySelector('[data-cockpit="rate"]').textContent = responses.length ? `${rate}%` : 'Dados insuficientes';
      $one('[data-trend]', root).innerHTML = '<div class="valora-empty"><strong>Dados insuficientes</strong><p>A tendência exige medições calculadas em pelo menos dois períodos comparáveis. Nenhum ponto foi estimado.</p></div>';
      const alerts = []; if (!active) alerts.push(['Ação necessária','Nenhuma pesquisa ativa','Inicie uma campanha para coletar novas evidências.','/Experience/Campaigns']); if (!responses.length) alerts.push(['Ação necessária','Leitura executiva pendente','A maturidade só será calculada pelo backend após respostas válidas.','/Surveys']);
      $one('[data-attention]', root).innerHTML = alerts.map(a => `<article><span>${a[0]}</span><div><strong>${a[1]}</strong><small>${a[2]}</small></div><a href="${a[3]}">Agir agora</a></article>`).join('') || '<div class="valora-empty">Não há ação derivada automaticamente sem regra de domínio e evidências suficientes.</div>';
      $one('[data-risks]', root).innerHTML = `<div class="v9-risk"><span>Maturidade</span><strong>Dados insuficientes</strong><p>Fonte necessária: resultado metodológico calculado e rastreável. Respostas brutas não são convertidas em score nesta tela.</p></div><div class="v9-risk"><span>Cadência</span><strong>${active ? 'Coleta ativa' : 'Sem coleta ativa'}</strong><p>Fonte: ${active} pesquisa(s) com status ativo ou agendado; atualizado agora pela API.</p></div>`;
    } catch (error) { showError(root, error); }
    $one('[data-generate-report]', root).addEventListener('click', async event => { event.target.disabled = true; event.target.textContent = 'Preparando…'; try { await AjaxClient.get('/reports/organization'); toast('success', 'Relatório executivo em preparação.'); location.href='/Reports'; } catch (error) { showError(root, error); event.target.disabled=false; event.target.textContent='Gerar relatório executivo'; } });
  }

  function help(root) {
    const articles = [
      ['Criar pesquisa','Use um template oficial ou crie uma estrutura livre no Estúdio.','/Experience/Templates'],['Publicar pesquisa','Revise perguntas, visualize em celular e publique a versão final.','/Forms'],['Compartilhar link','Abra Campanhas, escolha Compartilhar e copie o link ou QR Code.','/Experience/Campaigns'],['Acompanhar respostas','Veja adesão, respostas recebidas e ciclos ativos.','/Responses'],['Interpretar resultado','Comece pelo resumo executivo e siga para dimensões e prioridades.','/Results'],['Gerar relatório','Escolha o escopo e gere um documento executivo.','/Reports'],['Baixar certificado','Acesse certificados emitidos e valide os dados.','/Certificates'],['Criar plano de ação','Transforme uma recomendação em ação com prazo e responsável.','/PlanoDeAcao'],['Convidar membros','Cadastre a equipe e aplique papéis e escopos adequados.','/Users'],['Falar com suporte','Converse com a equipe Valora pelo WhatsApp oficial.','https://wa.me/5591992545353']
    ]; const grid=$one('[data-help-grid]',root); const render=q=>{q=q.toLocaleLowerCase('pt-BR');grid.innerHTML=articles.filter(a=>a.join(' ').toLocaleLowerCase('pt-BR').includes(q)).map((a,i)=>`<article class="valora-card"><span>${String(i+1).padStart(2,'0')}</span><h2>${a[0]}</h2><p>${a[1]}</p><a href="${a[2]}">Abrir recurso →</a></article>`).join('')||'<div class="valora-empty">Nenhum guia encontrado. Tente outra palavra.</div>';}; render(''); $one('[data-help-search]',root).addEventListener('input',e=>render(e.target.value));
  }

  const root = document.querySelector('[data-page$="-v9"]'); if (!root) return; dialogClose(root);
  if (root.dataset.page === 'templates-v9') templates(root); if (root.dataset.page === 'campaigns-v9') campaigns(root); if (root.dataset.page === 'cockpit-v9') cockpit(root); if (root.dataset.page === 'help-v9') help(root);
})();
