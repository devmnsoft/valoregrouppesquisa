(function () {
  const host = document.querySelector('[data-page="forms-page"]');
  if (!host) return;
  const dialog = document.querySelector('[data-form-dialog]');
  const body = host.querySelector('[data-items]');
  const empty = host.querySelector('[data-empty]');
  const error = host.querySelector('[data-error]');
  const count = host.querySelector('[data-count]');
  const search = host.querySelector('[data-filter-search]');
  const status = host.querySelector('[data-filter-status]');
  const category = host.querySelector('[data-filter-category]');
  const pagination = host.querySelector('[data-pagination]');
  const createForm = dialog.querySelector('[data-form-create]');
  const createError = createForm.querySelector('[data-create-error]');
  let page = 1;
  let requestSequence = 0;
  let searchTimer;
  let createDirty = false;
  let createSubmitting = false;
  let mutating = false;
  const filterStorageKey = `valora.forms.filters:${host.dataset.storageScope || 'unknown'}`;

  function readStoredFilters() { try { return JSON.parse(window.sessionStorage?.getItem(filterStorageKey) || '{}'); } catch (_) { return {}; } }
  function storeFilters() { try { window.sessionStorage?.setItem(filterStorageKey, JSON.stringify({ search: search.value, status: status.value, category: category.value, page })); } catch (_) { /* optional storage */ } }
  function escape(value) { const node = document.createElement('span'); node.textContent = value == null ? '' : String(value); return node.innerHTML; }
  const statusLabels = { draft: 'Rascunho', published: 'Publicado', archived: 'Arquivado' };
  function skeleton() { body.innerHTML = Array.from({ length: 5 }, () => `<tr class="skeleton-row" aria-hidden="true">${'<td><span class="skeleton-line"></span></td>'.repeat(8)}</tr>`).join(''); }
  function query() { const params = new URLSearchParams({ page, pageSize: 20 }); if (search.value.trim()) params.set('search', search.value.trim()); if (status.value) params.set('status', status.value); if (category.value) params.set('category', category.value); return `?${params}`; }
  function setMetrics(metrics) { ['draft', 'published', 'archived', 'inUse'].forEach(key => { host.querySelector(`[data-metric="${key}"]`).textContent = metrics ? metrics[key === 'draft' ? 'drafts' : key] : '—'; }); }

  function render(result) {
    const items = result.items || [];
    setMetrics(result.metrics);
    count.innerHTML = `<strong>${result.total}</strong> ${result.total === 1 ? 'formulário no filtro aplicado' : 'formulários no filtro aplicado'}`;
    empty.classList.toggle('d-none', items.length > 0);
    empty.querySelector('[data-empty-title]').textContent = result.total ? 'Esta página não possui registros' : 'Nenhum formulário corresponde aos filtros';
    empty.querySelector('[data-empty-description]').textContent = 'Ajuste os filtros ou crie uma nova estrutura para continuar.';
    body.innerHTML = items.map(item => {
      const usage = item.inCurrentUse ? '<span class="status-badge status-published" title="Vinculado a uma coleta ativa, publicada ou aberta">Em uso</span>' : item.hasHistoricalUse ? '<span title="Possui vínculo histórico, sem coleta atual">Histórico</span>' : '—';
      const primary = item.status === 'archived' ? '' : `<a class="btn btn-sm btn-outline-primary" href="/Forms/${item.id}/Builder">${item.status === 'published' ? 'Visualizar' : 'Editar'}</a>`;
      const version = item.status === 'published' ? `<button class="btn btn-sm btn-outline-secondary" data-action="version" data-id="${item.id}" data-name="${escape(item.name)}" data-version="${item.version}" data-version-number="${item.versionNumber}">Nova versão</button>` : '';
      const archive = item.status !== 'archived' ? `<button class="btn btn-sm btn-outline-danger" data-action="archive" data-id="${item.id}" data-name="${escape(item.name)}" data-version="${item.version}" ${item.inCurrentUse ? 'disabled title="Uma coleta em andamento impede o arquivamento"' : ''}>Arquivar</button>` : '';
      return `<tr><td><div class="form-identity"><strong>${escape(item.name)}</strong><small>${escape(item.description || 'Sem descrição executiva.')}</small></div></td><td>${escape(item.category || 'Diagnóstico')}</td><td>v${item.versionNumber}</td><td>${item.sections} seções · ${item.questions} perguntas · ${item.dimensions} dimensões<br><small>${usage}${item.hasResponses ? ' · possui respostas' : ''}</small></td><td>${item.estimatedMinutes} min</td><td><span class="status-badge status-${escape(item.status)}">${escape(statusLabels[item.status] || item.status)}</span></td><td>${new Date(item.updatedAt).toLocaleDateString('pt-BR')}</td><td><div class="row-actions">${primary}${version}${archive}</div></td></tr>`;
    }).join('');
    pagination.innerHTML = result.totalPages > 1 ? `<button class="btn btn-sm btn-outline-secondary" data-page="${result.page - 1}" ${result.hasPreviousPage ? '' : 'disabled'}>Anterior</button><span>Página ${result.page} de ${result.totalPages}</span><button class="btn btn-sm btn-outline-secondary" data-page="${result.page + 1}" ${result.hasNextPage ? '' : 'disabled'}>Próxima</button>` : '';
  }

  async function load() {
    const sequence = ++requestSequence;
    error.classList.add('d-none'); count.textContent = 'Preparando biblioteca…'; setMetrics(null); skeleton();
    try {
      const result = FormsApi.normalize(await FormsApi.list(query()));
      if (sequence !== requestSequence) return;
      if (!result || !Array.isArray(result.items)) throw new Error('Contrato paginado inválido.');
      page = result.page;
      const selected = category.value || category.dataset.savedValue || '';
      category.innerHTML = '<option value="">Todas as categorias</option>' + (result.categories || []).map(value => `<option value="${escape(value)}">${escape(value)}</option>`).join('');
      category.value = (result.categories || []).includes(selected) ? selected : ''; delete category.dataset.savedValue;
      render(result); storeFilters();
    } catch (problem) {
      if (sequence !== requestSequence) return;
      body.innerHTML = ''; pagination.innerHTML = ''; count.textContent = 'Biblioteca temporariamente indisponível.'; setMetrics(null);
      error.querySelector('[data-error-message]').textContent = ` ${problem.message || 'Preserve seu trabalho e tente novamente.'}`; error.classList.remove('d-none');
    }
  }

  function filtersChanged(delayed) { page = 1; storeFilters(); clearTimeout(searchTimer); if (delayed) searchTimer = setTimeout(load, 300); else load(); }
  host.querySelectorAll('[data-new-form], [data-action="new-form"]').forEach(button => button.addEventListener('click', () => dialog.showModal()));
  async function closeCreateDialog() { if (createSubmitting) return; if (createDirty && !await window.ValoraUI.confirm('Descartar os dados ainda não salvos deste formulário?')) return; createForm.reset(); createDirty = false; createError.classList.add('d-none'); dialog.close(); }
  document.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', closeCreateDialog));
  dialog.addEventListener('cancel', event => { event.preventDefault(); closeCreateDialog(); });
  host.querySelector('[data-refresh]').addEventListener('click', load); host.querySelector('[data-retry]').addEventListener('click', load);
  search.addEventListener('input', () => filtersChanged(true)); [status, category].forEach(control => control.addEventListener('change', () => filtersChanged(false)));
  host.querySelector('[data-clear-filters]').addEventListener('click', () => { search.value = ''; status.value = ''; category.value = ''; filtersChanged(false); search.focus(); });
  pagination.addEventListener('click', event => { const button = event.target.closest('[data-page]'); if (!button || button.disabled) return; page = Number(button.dataset.page); load(); });
  body.addEventListener('click', async event => {
    const button = event.target.closest('[data-action]'); if (!button || mutating) return;
    const snapshot = { id: button.dataset.id, name: button.dataset.name, version: Number(button.dataset.version), versionNumber: Number(button.dataset.versionNumber) };
    mutating = true;
    try {
      if (button.dataset.action === 'version') {
        if (!await window.ValoraUI.confirm(`Criar um rascunho a partir da versão ${snapshot.versionNumber} de “${snapshot.name}”? A versão publicada e suas respostas serão preservadas.`)) return;
        const draft = FormsApi.normalize(await FormsApi.createVersion(snapshot.id, { expectedFormVersion: snapshot.version }));
        if (!draft?.id) throw new Error('O servidor não confirmou o novo rascunho. Talvez já exista um rascunho para este formulário.');
        window.location.assign(`/Forms/${encodeURIComponent(draft.id)}/Builder`); return;
      }
      if (!await window.ValoraUI.confirm(`Arquivar “${snapshot.name}”? O histórico e as respostas serão preservados. Esta operação será bloqueada se houver coleta em andamento.`)) return;
      await FormsApi.archive(snapshot.id, { expectedVersion: snapshot.version });
      await load();
      const current = FormsApi.normalize(await FormsApi.list(query()));
      if (page > 1 && Array.isArray(current?.items) && current.items.length === 0) { page -= 1; await load(); }
      window.ValoraUI.toast?.('Formulário arquivado. Histórico e respostas foram preservados.', 'success');
    } catch (problem) { error.querySelector('[data-error-message]').textContent = ` ${problem.message || 'A operação não foi concluída.'}`; error.classList.remove('d-none'); }
    finally { mutating = false; }
  });
  const saved = readStoredFilters(); search.value = saved.search || ''; status.value = saved.status || ''; category.dataset.savedValue = saved.category || ''; page = Math.max(1, Number(saved.page) || 1);
  createForm.addEventListener('input', () => { createDirty = true; createError.classList.add('d-none'); });
  createForm.addEventListener('submit', async event => {
    event.preventDefault();
    // SubmitEvent.currentTarget is cleared when event dispatch finishes. Keep every operation input stable before the first await.
    const form = createForm; if (createSubmitting || !form.reportValidity()) return;
    const submit = form.querySelector('[type="submit"]'); const values = new FormData(form);
    const request = { name: values.get('name'), description: values.get('description'), category: values.get('category'), estimatedMinutes: Number(values.get('estimatedMinutes')) };
    createSubmitting = true; submit.disabled = true; submit.textContent = 'Confirmando…';
    try { if (!await window.ValoraUI.confirm('Criar este formulário e abrir o construtor?')) return; submit.textContent = 'Criando…'; const created = FormsApi.normalize(await FormsApi.create(request)); if (!created?.id) throw new Error('O servidor não confirmou o identificador do formulário criado.'); createDirty = false; window.location.assign(`/Forms/${encodeURIComponent(created.id)}/Builder`); }
    catch (problem) { createError.textContent = problem.message || 'Não foi possível criar o formulário. Os dados foram preservados para nova tentativa.'; createError.classList.remove('d-none'); }
    finally { createSubmitting = false; submit.disabled = false; submit.textContent = 'Criar formulário'; }
  });
  if (new URLSearchParams(window.location.search).get('intent') === 'create') dialog.showModal();
  load();
}());
