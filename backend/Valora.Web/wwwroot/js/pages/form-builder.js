(() => {
  'use strict';
  const host = document.querySelector('[data-page="form-builder"]');
  if (!host) return;
  const formId = host.dataset.formId;
  const errorBox = host.querySelector('[data-error]');
  const properties = host.querySelector('[data-properties-form]');
  let form;
  let selection = { type: 'form', item: null };
  let saveTimer;

  const escapeHtml = (value) => { const node = document.createElement('span'); node.textContent = value == null ? '' : String(value); return node.innerHTML; };
  const field = (label, name, value = '', type = 'text', attributes = '') => `<label class="form-label" for="property-${name}">${label}</label><input class="form-control" id="property-${name}" name="${name}" type="${type}" value="${escapeHtml(value)}" ${attributes}>`;
  const fail = (problem) => { errorBox.textContent = problem.message || 'Não foi possível concluir a operação. Tente novamente.'; errorBox.classList.remove('d-none'); };
  const clearError = () => errorBox.classList.add('d-none');
  const selected = (type, id) => selection.type === type && selection.item?.id === id ? ' is-selected' : '';

  function questionMarkup(question) {
    const choices = (question.options || []).map(option => `<button type="button" class="builder-option${selected('option', option.id)}" data-select="option" data-id="${option.id}"><span>${escapeHtml(option.label)}</span><small>${escapeHtml(option.value)}${option.score == null ? '' : ` · ${option.score} ponto(s)`}</small></button>`).join('');
    const sample = question.type === 'long_text' ? '<textarea disabled aria-label="Exemplo de resposta"></textarea>' : question.type === 'short_text' ? '<input disabled aria-label="Exemplo de resposta">' : choices;
    return `<article class="builder-question${selected('question', question.id)}" tabindex="0" role="button" data-select="question" data-id="${question.id}"><div><span>${escapeHtml(question.code)}</span><strong>${escapeHtml(question.title)}</strong></div><small>${escapeHtml(question.type)}${question.required ? ' · obrigatória' : ''}</small>${sample}<div class="builder-inline-actions"><button type="button" data-add-option="${question.id}">Adicionar opção</button><button type="button" data-delete="question" data-id="${question.id}">Excluir</button></div></article>`;
  }

  function render() {
    host.querySelector('[data-loading]').classList.add('d-none');
    host.querySelector('[data-builder-content]').classList.remove('d-none');
    host.querySelector('[data-form-name]').textContent = form.name;
    host.querySelector('[data-form-status]').textContent = form.status;
    host.querySelector('[data-section-nav]').innerHTML = form.sections.map(item => `<li><button type="button" data-select="section" data-id="${item.id}">${escapeHtml(item.title)} <small>${(item.questions || []).length}</small></button></li>`).join('');
    host.querySelector('[data-canvas]').innerHTML = form.sections.length ? form.sections.map((item, index) => `<section class="builder-section${selected('section', item.id)}" data-select="section" data-id="${item.id}" tabindex="0"><header><span>Seção ${index + 1}</span><h2>${escapeHtml(item.title)}</h2><p>${escapeHtml(item.description)}</p><div class="builder-inline-actions"><button type="button" data-add-question="${item.id}">Adicionar pergunta</button><button type="button" data-move="section" data-id="${item.id}" data-direction="up" ${index === 0 ? 'disabled' : ''}>Subir</button><button type="button" data-move="section" data-id="${item.id}" data-direction="down" ${index === form.sections.length - 1 ? 'disabled' : ''}>Descer</button><button type="button" data-delete="section" data-id="${item.id}">Excluir</button></div></header>${(item.questions || []).map(questionMarkup).join('') || '<p class="empty-state">Nenhuma pergunta. Use “Adicionar pergunta” para começar.</p>'}</section>`).join('') : '<div class="empty-state"><h2>Comece seu diagnóstico</h2><p>Organize o conteúdo em seções e adicione perguntas.</p><button class="btn btn-primary" type="button" data-add-section>Adicionar seção</button></div>';
    renderProperties();
  }

  function renderProperties() {
    const title = host.querySelector('[data-properties-title]');
    if (selection.type === 'form') {
      title.textContent = 'Propriedades do formulário';
      properties.innerHTML = field('Nome', 'name', form.name, 'text', 'required maxlength="160"') + `<label class="form-label" for="property-description">Descrição</label><textarea class="form-control" id="property-description" name="description">${escapeHtml(form.description)}</textarea>` + field('Categoria', 'category', form.category) + field('Tempo estimado', 'estimatedMinutes', form.estimatedMinutes, 'number', 'min="1" max="480"');
    } else if (selection.type === 'section') {
      title.textContent = 'Propriedades da seção'; properties.innerHTML = field('Título', 'title', selection.item.title, 'text', 'required') + `<label class="form-label" for="property-description">Descrição</label><textarea class="form-control" id="property-description" name="description">${escapeHtml(selection.item.description)}</textarea>`;
    } else if (selection.type === 'question') {
      title.textContent = 'Propriedades da pergunta'; properties.innerHTML = field('Código técnico', 'code', selection.item.code, 'text', 'required pattern="[A-Za-z0-9_-]+"') + field('Título', 'title', selection.item.title, 'text', 'required') + `<label class="form-label" for="property-type">Tipo</label><select class="form-select" id="property-type" name="type">${['likert_1_5','single_choice','multiple_choice','short_text','long_text','heading','explanatory_text','separator'].map(type => `<option ${type === selection.item.type ? 'selected' : ''}>${type}</option>`).join('')}</select>` + field('Dimensão', 'dimensionCode', selection.item.dimensionCode) + field('Peso', 'weight', selection.item.weight, 'number', 'min="0" step="0.1"') + `<label><input type="checkbox" name="required" ${selection.item.required ? 'checked' : ''}> Obrigatória</label>`;
    } else {
      title.textContent = 'Propriedades da opção'; properties.innerHTML = field('Label', 'label', selection.item.label, 'text', 'required') + field('Valor', 'value', selection.item.value, 'text', 'required') + field('Score', 'score', selection.item.score, 'number', 'step="0.1"');
    }
    properties.insertAdjacentHTML('beforeend', '<button class="btn btn-primary mt-3" type="submit">Salvar alterações</button>');
  }

  async function load() { try { form = FormsApi.normalize(await FormsApi.get(formId)); render(); } catch (problem) { host.querySelector('[data-loading]').classList.add('d-none'); fail(problem); } }
  async function saveProperties() {
    clearError(); const values = new FormData(properties); host.querySelector('[data-save-state]').textContent = 'Salvando…';
    let request;
    if (selection.type === 'form') request = FormsApi.update(formId, { name: values.get('name'), description: values.get('description'), category: values.get('category'), estimatedMinutes: Number(values.get('estimatedMinutes')), expectedVersion: form.version });
    if (selection.type === 'section') request = FormsApi.updateSection(formId, selection.item.id, { title: values.get('title'), description: values.get('description'), expectedVersion: selection.item.version });
    if (selection.type === 'question') request = FormsApi.updateQuestion(formId, selection.item.id, { ...selection.item, code: values.get('code'), title: values.get('title'), type: values.get('type'), dimensionCode: values.get('dimensionCode') || null, weight: Number(values.get('weight')), required: values.has('required'), expectedVersion: selection.item.version });
    if (selection.type === 'option') request = FormsApi.updateOption(formId, selection.item.id, { label: values.get('label'), value: values.get('value'), score: values.get('score') === '' ? null : Number(values.get('score')), expectedVersion: selection.item.version });
    try { await request; host.querySelector('[data-save-state]').textContent = 'Salvo'; await load(); } catch (problem) { host.querySelector('[data-save-state]').textContent = problem.status === 409 ? 'Conflito de versão — recarregue' : 'Erro ao salvar'; fail(problem); }
  }

  properties.addEventListener('submit', event => { event.preventDefault(); saveProperties(); });
  properties.addEventListener('input', () => { clearTimeout(saveTimer); host.querySelector('[data-save-state]').textContent = 'Alterações pendentes'; saveTimer = setTimeout(saveProperties, 800); });
  host.addEventListener('click', async event => {
    const trigger = event.target.closest('[data-select],button'); if (!trigger) return;
    if (trigger.matches('[data-add-section]')) { await FormsApi.createSection(formId, { title: 'Nova seção', description: '', position: form.sections.length, expectedVersion: form.draftVersion }); selection = { type: 'form', item: null }; await load(); return; }
    if (trigger.dataset.select) { const item = form.sections.flatMap(section => [section, ...(section.questions || []), ...(section.questions || []).flatMap(question => question.options || [])]).find(candidate => candidate.id === trigger.dataset.id); selection = { type: trigger.dataset.select, item }; render(); return; }
    if (trigger.dataset.addQuestion) { const section = form.sections.find(item => item.id === trigger.dataset.addQuestion); await FormsApi.createQuestion(formId, { sectionId: section.id, code: `Q${Date.now().toString().slice(-6)}`, type: 'likert_1_5', title: 'Nova pergunta', required: false, weight: 1, position: section.questions.length, expectedVersion: form.draftVersion }); await load(); return; }
    if (trigger.dataset.addOption) { await FormsApi.createOption(formId, trigger.dataset.addOption, { label: 'Nova opção', value: `opcao_${Date.now().toString().slice(-5)}`, score: null, position: 999, expectedVersion: form.draftVersion }); await load(); return; }
    if (trigger.dataset.delete && confirm('Excluir este item do rascunho?')) {
      const item = form.sections.flatMap(section => [section, ...(section.questions || []), ...(section.questions || []).flatMap(question => question.options || [])]).find(candidate => candidate.id === trigger.dataset.id);
      if (trigger.dataset.delete === 'section') await FormsApi.deleteSection(formId, item.id, { expectedVersion: item.version });
      if (trigger.dataset.delete === 'question') await FormsApi.deleteQuestion(formId, item.id, { expectedVersion: item.version });
      selection = { type: 'form', item: null }; await load(); return;
    }
    if (trigger.dataset.move === 'section') {
      const index = form.sections.findIndex(item => item.id === trigger.dataset.id);
      const destination = trigger.dataset.direction === 'up' ? index - 1 : index + 1;
      await FormsApi.reorder(formId, { itemId: trigger.dataset.id, itemType: 'section', sourceContainerId: form.currentDraftVersionId, targetContainerId: form.currentDraftVersionId, previousPosition: index, newPosition: destination, expectedVersion: form.draftVersion });
      await load();
    }
  });
  host.querySelector('[data-preview]').addEventListener('click', () => { host.querySelector('[data-preview-content]').innerHTML = `<h1>${escapeHtml(form.name)}</h1><p>${escapeHtml(form.description)}</p>${form.sections.map(section => `<section><h2>${escapeHtml(section.title)}</h2>${section.questions.map(questionMarkup).join('')}</section>`).join('')}`; host.querySelector('[data-preview-dialog]').showModal(); });
  host.querySelector('[data-close-preview]').addEventListener('click', () => host.querySelector('[data-preview-dialog]').close());
  host.querySelectorAll('[data-preview-size]').forEach(button => button.addEventListener('click', () => host.querySelector('[data-preview-content]').classList.toggle('is-mobile', button.dataset.previewSize === 'mobile')));
  host.querySelector('[data-publish]').addEventListener('click', async event => { if (!form.currentDraftVersionId || !confirm('Publicar esta versão? Ela se tornará imutável.')) return; event.currentTarget.disabled = true; try { await FormsApi.publish(formId, { expectedVersion: form.draftVersion }); await load(); } catch (problem) { fail(problem); } finally { event.currentTarget.disabled = false; } });
  load();
})();
