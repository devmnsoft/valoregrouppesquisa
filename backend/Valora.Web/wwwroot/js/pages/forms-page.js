(function () {
  const host = document.querySelector('[data-page="forms-page"]');
  if (!host) return;
  const dialog = document.querySelector('[data-form-dialog]');
  const body = host.querySelector('[data-items]');
  const empty = host.querySelector('[data-empty]');
  const error = host.querySelector('[data-error]');

  function escape(value) {
    const node = document.createElement('span');
    node.textContent = value == null ? '' : String(value);
    return node.innerHTML;
  }

  function render(items) {
    ['draft', 'published', 'archived'].forEach(status => {
      host.querySelector(`[data-metric="${status}"]`).textContent = items.filter(item => item.status === status).length;
    });
    host.querySelector('[data-metric="inUse"]').textContent = items.filter(item => item.inUse).length;
    empty.classList.toggle('d-none', items.length > 0);
    body.innerHTML = items.map(item => `<tr><td><strong>${escape(item.name)}</strong><small>${escape(item.description)}</small></td><td>${escape(item.category || 'Diagnóstico')}</td><td>v${item.versionNumber}</td><td>${item.sections} seções · ${item.questions} perguntas · ${item.dimensions} dimensões</td><td>${item.estimatedMinutes} min</td><td><span class="status-badge status-${escape(item.status)}">${escape(item.status)}</span></td><td>${new Date(item.updatedAt).toLocaleDateString('pt-BR')}</td><td><a class="btn btn-sm btn-outline-primary" href="/Forms/${item.id}/Builder">Editar</a></td></tr>`).join('');
  }

  async function load() {
    error.classList.add('d-none');
    try {
      const response = await FormsApi.list('?page=1&pageSize=100');
      render(FormsApi.normalize(response) || []);
    } catch (problem) {
      error.textContent = problem.message || 'Não foi possível carregar os formulários. Tente novamente.';
      error.classList.remove('d-none');
    }
  }

  host.querySelectorAll('[data-new-form]').forEach(button => button.addEventListener('click', () => dialog.showModal()));
  document.querySelectorAll('[data-dialog-close]').forEach(button => button.addEventListener('click', () => dialog.close()));
  host.querySelector('[data-refresh]').addEventListener('click', load);
  dialog.querySelector('form').addEventListener('submit', async event => {
    event.preventDefault();
    const submit = event.currentTarget.querySelector('[type="submit"]');
    submit.disabled = true;
    const values = new FormData(event.currentTarget);
    try {
      await FormsApi.create({ name: values.get('name'), description: values.get('description'), category: values.get('category'), estimatedMinutes: Number(values.get('estimatedMinutes')) });
      dialog.close(); event.currentTarget.reset(); await load();
    } catch (problem) {
      error.textContent = problem.message || 'O formulário não pôde ser criado. Revise os campos e tente novamente.';
      error.classList.remove('d-none'); dialog.close();
    } finally { submit.disabled = false; }
  });
  load();
}());
