(function () {
  const host = document.querySelector('[data-page="form-builder"]');
  if (!host) return;
  const formId = host.dataset.formId;
  const error = host.querySelector('[data-error]');
  let form;

  function escape(value) { const node = document.createElement('span'); node.textContent = value == null ? '' : String(value); return node.innerHTML; }
  function fail(problem) { error.textContent = problem.message || 'Não foi possível concluir a operação. Tente novamente.'; error.classList.remove('d-none'); }
  function renderQuestion(question) {
    const options = question.options || [];
    const controls = options.length ? `<ul>${options.map(option => `<li>${escape(option.label)}${option.score == null ? '' : ` · ${option.score} ponto(s)`}</li>`).join('')}</ul>` : '';
    return `<article class="builder-question" tabindex="0"><div><span>${escape(question.code)}</span><strong>${escape(question.title)}</strong></div><small>${escape(question.type)}${question.required ? ' · obrigatória' : ''}${question.dimensionCode ? ` · ${escape(question.dimensionCode)}` : ''}</small>${controls}</article>`;
  }
  function render(data) {
    form = data;
    host.querySelector('[data-loading]').classList.add('d-none'); host.querySelector('[data-builder-content]').classList.remove('d-none');
    host.querySelector('[data-form-name]').textContent = data.name; host.querySelector('[data-form-status]').textContent = data.status;
    const fields = host.querySelector('[data-metadata-form]').elements; fields.name.value = data.name; fields.description.value = data.description || ''; fields.category.value = data.category || ''; fields.estimatedMinutes.value = data.estimatedMinutes;
    host.querySelector('[data-section-nav]').innerHTML = data.sections.map(section => `<li><a href="#section-${section.id}">${escape(section.title)}</a></li>`).join('');
    host.querySelector('[data-canvas]').innerHTML = data.sections.length ? data.sections.map(section => `<section class="builder-section" id="section-${section.id}"><header><span>Seção ${section.position + 1}</span><h2>${escape(section.title)}</h2><p>${escape(section.description)}</p></header>${(section.questions || []).map(renderQuestion).join('') || '<p class="empty-state">Esta seção ainda não possui perguntas.</p>'}</section>`).join('') : '<div class="empty-state"><h2>Estrutura vazia</h2><p>Adicione a primeira seção pela API administrativa para começar.</p></div>';
  }
  async function load() { try { render(FormsApi.normalize(await FormsApi.get(formId))); } catch (problem) { host.querySelector('[data-loading]').classList.add('d-none'); fail(problem); } }
  host.querySelector('[data-metadata-form]').addEventListener('submit', async event => { event.preventDefault(); const values = new FormData(event.currentTarget); try { host.querySelector('[data-save-state]').textContent = 'Salvando…'; render(FormsApi.normalize(await FormsApi.update(formId, { name: values.get('name'), description: values.get('description'), category: values.get('category'), estimatedMinutes: Number(values.get('estimatedMinutes')), expectedVersion: form.version }))); host.querySelector('[data-save-state]').textContent = 'Alterações salvas'; } catch (problem) { host.querySelector('[data-save-state]').textContent = ''; fail(problem); } });
  host.querySelector('[data-preview]').addEventListener('click', () => host.querySelector('[data-canvas]').scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' }));
  host.querySelector('[data-publish]').addEventListener('click', async event => { if (!form.currentDraftVersionId || !confirm('Publicar esta versão? Ela se tornará imutável.')) return; event.currentTarget.disabled = true; try { await FormsApi.publish(formId, { expectedVersion: form.draftVersion }); await load(); } catch (problem) { fail(problem); } finally { event.currentTarget.disabled = false; } });
  load();
}());
