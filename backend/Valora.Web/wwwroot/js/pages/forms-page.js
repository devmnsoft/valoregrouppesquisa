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
  let forms = [];

  function escape(value) {
    const node = document.createElement('span');
    node.textContent = value == null ? '' : String(value);
    return node.innerHTML;
  }

  const statusLabels = { draft: 'Rascunho', published: 'Publicado', archived: 'Arquivado' };
  function skeleton() { body.innerHTML = Array.from({ length: 5 }, () => `<tr class="skeleton-row" aria-hidden="true">${'<td><span class="skeleton-line"></span></td>'.repeat(8)}</tr>`).join(''); }
  function applyFilters() {
    const term = search.value.trim().toLocaleLowerCase('pt-BR');
    const filtered = forms.filter(item => (!status.value || item.status === status.value) && (!category.value || (item.category || 'Diagnóstico') === category.value) && (!term || [item.name, item.description, item.category].some(value => String(value || '').toLocaleLowerCase('pt-BR').includes(term))));
    render(filtered);
    sessionStorage.setItem('valora.forms.filters', JSON.stringify({ search: search.value, status: status.value, category: category.value }));
  }
  function render(items) {
    ['draft', 'published', 'archived'].forEach(status => {
      host.querySelector(`[data-metric="${status}"]`).textContent = items.filter(item => item.status === status).length;
    });
    host.querySelector('[data-metric="inUse"]').textContent = items.filter(item => item.inUse).length;
    empty.classList.toggle('d-none', items.length > 0);
    empty.querySelector('[data-empty-title]').textContent = forms.length ? 'Nenhum formulário corresponde aos filtros' : 'Comece pelo primeiro formulário';
    empty.querySelector('[data-empty-description]').textContent = forms.length ? 'Ajuste os filtros para ampliar a leitura da biblioteca.' : 'Estruture dimensões e perguntas para iniciar uma leitura organizacional baseada em evidências.';
    empty.querySelector('[data-new-form]').classList.toggle('d-none', forms.length > 0);
    count.innerHTML = `<strong>${items.length}</strong> ${items.length === 1 ? 'formulário encontrado' : 'formulários encontrados'}`;
    body.innerHTML = items.map(item => `<tr><td><div class="form-identity"><strong>${escape(item.name)}</strong><small>${escape(item.description || 'Sem descrição executiva.')}</small></div></td><td>${escape(item.category || 'Diagnóstico')}</td><td>v${item.versionNumber}</td><td>${item.sections} seções · ${item.questions} perguntas · ${item.dimensions} dimensões</td><td>${item.estimatedMinutes} min</td><td><span class="status-badge status-${escape(item.status)}">${escape(statusLabels[item.status] || item.status)}</span></td><td>${new Date(item.updatedAt).toLocaleDateString('pt-BR')}</td><td><div class="row-actions"><a class="btn btn-sm btn-outline-primary" href="/Forms/${item.id}/Builder">${item.status === 'published' ? 'Visualizar' : 'Editar'}</a></div></td></tr>`).join('');
  }

  async function load() {
    error.classList.add('d-none');
    count.textContent = 'Preparando biblioteca…'; skeleton();
    try {
      const response = await FormsApi.list('?page=1&pageSize=100');
      forms = FormsApi.normalize(response) || [];
      const categories = [...new Set(forms.map(item => item.category || 'Diagnóstico'))].sort((a, b) => a.localeCompare(b, 'pt-BR'));
      const selected = category.value || category.dataset.savedValue || ''; category.innerHTML = '<option value="">Todas as categorias</option>' + categories.map(value => `<option>${escape(value)}</option>`).join(''); category.value = categories.includes(selected) ? selected : ''; delete category.dataset.savedValue;
      applyFilters();
    } catch (problem) {
      body.innerHTML = ''; count.textContent = 'Biblioteca temporariamente indisponível.';
      error.querySelector('[data-error-message]').textContent = ' Preserve seu trabalho e tente novamente em instantes.';
      error.classList.remove('d-none');
    }
  }

  host.querySelectorAll('[data-new-form], [data-action="new-form"]').forEach(button => button.addEventListener('click', () => dialog.showModal()));
  document.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
  host.querySelector('[data-refresh]').addEventListener('click', load);
  host.querySelector('[data-retry]').addEventListener('click', load);
  [search, status, category].forEach(control => control.addEventListener(control === search ? 'input' : 'change', applyFilters));
  host.querySelector('[data-clear-filters]').addEventListener('click', () => { search.value = ''; status.value = ''; category.value = ''; applyFilters(); search.focus(); });
  try { const saved = JSON.parse(sessionStorage.getItem('valora.forms.filters') || '{}'); search.value = saved.search || ''; status.value = saved.status || ''; category.dataset.savedValue = saved.category || ''; } catch (_) { /* Preferências inválidas são ignoradas com segurança. */ }
  dialog.querySelector('form').addEventListener('submit', async event => {
    event.preventDefault();
    const submit = event.currentTarget.querySelector('[type="submit"]');
    submit.disabled = true;
    const values = new FormData(event.currentTarget);
    try {
      await FormsApi.create({ name: values.get('name'), description: values.get('description'), category: values.get('category'), estimatedMinutes: Number(values.get('estimatedMinutes')) });
      dialog.close(); event.currentTarget.reset(); window.ValoraToast?.success?.('Formulário criado. Agora organize dimensões e perguntas.'); await load();
    } catch (problem) {
      error.querySelector('[data-error-message]').textContent = ' Revise os campos obrigatórios e tente novamente.';
      error.classList.remove('d-none');
    } finally { submit.disabled = false; }
  });
  if (new URLSearchParams(window.location.search).get('intent') === 'create') dialog.showModal();
  load();
}());
