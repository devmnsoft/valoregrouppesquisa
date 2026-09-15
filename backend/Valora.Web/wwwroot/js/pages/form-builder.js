(() => {
  'use strict';
  const host = document.querySelector('[data-page="form-builder"]');
  if (!host) return;
  const formId = host.dataset.formId;
  const errorBox = host.querySelector('[data-error]');
  const properties = host.querySelector('[data-properties-form]');
  const reviewDialog = host.querySelector('[data-review-dialog]');
  let form;
  let dimensionCatalog = [];
  let selection = { type: 'form', item: null };
  let propertiesDirty = false;
  let mutationPending = false;
  const optionTypes = new Set(['likert_1_5', 'single_choice', 'multiple_choice']);
  const typeLabels = { likert_1_5: 'Escala de 1 a 5', single_choice: 'Escolha única', multiple_choice: 'Múltipla escolha', short_text: 'Texto curto', long_text: 'Texto longo', heading: 'Título', description: 'Texto explicativo', separator: 'Separador' };

  const isReadOnly = () => form?.status === 'published' || form?.status === 'archived' || !form?.currentDraftVersionId;
  const escapeHtml = value => { const node = document.createElement('span'); node.textContent = value == null ? '' : String(value); return node.innerHTML; };
  const field = (label, name, value = '', type = 'text', attributes = '') => `<label class="form-label" for="property-${name}">${label}</label><input class="form-control" id="property-${name}" name="${name}" type="${type}" value="${escapeHtml(value)}" ${attributes}>`;
  const fail = problem => { errorBox.textContent = problem?.message || 'Não foi possível concluir a operação. Tente novamente.'; errorBox.classList.remove('d-none'); errorBox.focus(); };
  const clearError = () => errorBox.classList.add('d-none');
  const selected = (type, id) => selection.type === type && selection.item?.id === id ? ' is-selected' : '';
  const allItems = () => form.sections.flatMap(section => [section, ...(section.questions || []), ...(section.questions || []).flatMap(question => question.options || [])]);

  function movementButtons(type, item, index, count) {
    return `<button type="button" data-move="${type}" data-id="${item.id}" data-direction="up" aria-label="Mover para cima" ${index === 0 ? 'disabled' : ''}>Subir</button><button type="button" data-move="${type}" data-id="${item.id}" data-direction="down" aria-label="Mover para baixo" ${index === count - 1 ? 'disabled' : ''}>Descer</button>`;
  }

  function questionMarkup(question, index, questions) {
    const choices = (question.options || []).map((option, optionIndex, options) => `<div class="builder-option-row"><button type="button" class="builder-option${selected('option', option.id)}" data-select="option" data-id="${option.id}"><span>${escapeHtml(option.label)}</span><small>${escapeHtml(option.value)}${option.score == null ? '' : ` · ${option.score} ponto(s)`}</small></button><div class="builder-inline-actions">${movementButtons('option', option, optionIndex, options.length)}<button type="button" class="builder-option-delete" data-delete="option" data-id="${option.id}" aria-label="Excluir opção ${escapeHtml(option.label)}">Excluir</button></div></div>`).join('');
    const sample = question.type === 'long_text' ? '<textarea disabled aria-label="Exemplo de resposta"></textarea>' : question.type === 'short_text' ? '<input disabled aria-label="Exemplo de resposta">' : choices;
    const addOption = optionTypes.has(question.type) ? `<button type="button" data-add-option="${question.id}">Adicionar opção</button>` : '';
    return `<article class="builder-question${selected('question', question.id)}" tabindex="0" role="button" data-select="question" data-id="${question.id}"><div><span>${escapeHtml(question.code)}</span><strong>${escapeHtml(question.title)}</strong></div><small>${escapeHtml(typeLabels[question.type] || question.type)}${question.required ? ' · obrigatória' : ''}</small>${sample}<div class="builder-inline-actions">${addOption}${movementButtons('question', question, index, questions.length)}<button type="button" data-delete="question" data-id="${question.id}">Excluir</button></div></article>`;
  }

  function render() {
    host.querySelector('[data-loading]').classList.add('d-none');
    host.querySelector('[data-builder-content]').classList.remove('d-none');
    host.querySelector('[data-form-name]').textContent = form.name;
    host.querySelector('[data-form-status]').textContent = ({ draft: 'Rascunho', published: 'Publicado', archived: 'Arquivado' })[form.status] || form.status;
    host.querySelector('[data-form-version]').textContent = `Versão ${form.selectedVersionNumber || '—'}`;
    host.querySelector('[data-readonly-notice]').classList.toggle('d-none', !isReadOnly());
    host.querySelector('[data-publish]').classList.toggle('d-none', isReadOnly());
    host.querySelector('[data-section-nav]').innerHTML = form.sections.map(item => `<li><button type="button" data-select="section" data-id="${item.id}">${escapeHtml(item.title)} <small>${(item.questions || []).length}</small></button></li>`).join('');
    host.querySelector('[data-canvas]').innerHTML = form.sections.length ? form.sections.map((item, index) => `<section class="builder-section${selected('section', item.id)}" data-select="section" data-id="${item.id}" tabindex="0"><header><span>Seção ${index + 1}</span><h2>${escapeHtml(item.title)}</h2><p>${escapeHtml(item.description)}</p><div class="builder-inline-actions"><button type="button" data-add-question="${item.id}">Adicionar pergunta</button>${movementButtons('section', item, index, form.sections.length)}<button type="button" data-delete="section" data-id="${item.id}">Excluir</button></div></header>${(item.questions || []).map(questionMarkup).join('') || '<p class="empty-state">Nenhuma pergunta. Use “Adicionar pergunta” para começar.</p>'}</section>`).join('') : '<div class="empty-state"><h2>Comece seu diagnóstico</h2><p>Organize o conteúdo em seções e adicione perguntas.</p><button class="btn btn-primary" type="button" data-add-section>Adicionar seção</button></div>';
    renderProperties();
    host.querySelectorAll('[data-add-section],[data-add-question],[data-add-option],[data-delete],[data-move]').forEach(control => { control.disabled = isReadOnly() || control.disabled; if (isReadOnly()) control.setAttribute('aria-disabled', 'true'); });
  }

  function renderProperties() {
    const title = host.querySelector('[data-properties-title]');
    if (selection.type === 'form') {
      title.textContent = 'Propriedades do formulário';
      properties.innerHTML = field('Nome', 'name', form.name, 'text', 'required maxlength="160"') + `<label class="form-label" for="property-description">Descrição</label><textarea class="form-control" id="property-description" name="description">${escapeHtml(form.description)}</textarea>` + field('Categoria', 'category', form.category, 'text', 'required maxlength="80"') + field('Tempo estimado', 'estimatedMinutes', form.estimatedMinutes, 'number', 'min="1" max="480"');
    } else if (selection.type === 'section') {
      title.textContent = 'Propriedades da seção'; properties.innerHTML = field('Título', 'title', selection.item.title, 'text', 'required') + `<label class="form-label" for="property-description">Descrição</label><textarea class="form-control" id="property-description" name="description">${escapeHtml(selection.item.description)}</textarea>`;
    } else if (selection.type === 'question') {
      title.textContent = 'Propriedades da pergunta'; properties.innerHTML = field('Código técnico', 'code', selection.item.code, 'text', 'required pattern="[A-Za-z0-9_-]+"') + field('Título', 'title', selection.item.title, 'text', 'required') + `<label class="form-label" for="property-type">Tipo</label><select class="form-select" id="property-type" name="type">${Object.entries(typeLabels).map(([type, label]) => `<option value="${type}" ${type === selection.item.type ? 'selected' : ''}>${label}</option>`).join('')}</select><label class="form-label" for="property-dimensionCode">Dimensão metodológica</label><select class="form-select" id="property-dimensionCode" name="dimensionCode"><option value="">Sem vínculo</option>${dimensionCatalog.map(item => `<option value="${escapeHtml(item.code)}" ${item.code === selection.item.dimensionCode ? 'selected' : ''}>${escapeHtml(item.name)}${item.isLegacy ? ' (vínculo antigo/inativo)' : ''}</option>`).join('')}</select><small class="form-text">Somente dimensões publicadas e autorizadas podem ser escolhidas. Vínculos antigos permanecem identificados.</small>` + field('Peso', 'weight', selection.item.weight, 'number', 'min="0" step="0.1"') + `<label><input type="checkbox" name="required" ${selection.item.required ? 'checked' : ''}> Obrigatória</label>`;
    } else {
      title.textContent = 'Propriedades da opção'; properties.innerHTML = field('Rótulo', 'label', selection.item.label, 'text', 'required') + field('Valor', 'value', selection.item.value, 'text', 'required') + field('Pontuação', 'score', selection.item.score, 'number', 'min="1" max="5" step="0.1"');
    }
    properties.querySelectorAll('input,textarea,select').forEach(control => control.disabled = isReadOnly());
    if (!isReadOnly()) properties.insertAdjacentHTML('beforeend', '<button class="btn btn-primary mt-3" type="submit">Salvar alterações</button>');
    propertiesDirty = false;
  }

  async function load() {
    try {
      const [detail, dimensions] = await Promise.all([FormsApi.get(formId), FormsApi.listDimensions(formId)]);
      form = FormsApi.normalize(detail);
      dimensionCatalog = FormsApi.normalize(dimensions) || [];
      if (selection.item) selection.item = allItems().find(item => item.id === selection.item.id) || null;
      if (!selection.item) selection = { type: 'form', item: null };
      render();
    } catch (problem) { host.querySelector('[data-loading]').classList.add('d-none'); fail(problem); }
  }

  function propertyRequest(snapshot, values) {
    if (snapshot.type === 'form') return FormsApi.update(formId, { name: values.get('name'), description: values.get('description'), category: values.get('category'), estimatedMinutes: Number(values.get('estimatedMinutes')), expectedVersion: snapshot.formVersion });
    if (snapshot.type === 'section') return FormsApi.updateSection(formId, snapshot.item.id, { title: values.get('title'), description: values.get('description'), expectedVersion: snapshot.item.version });
    if (snapshot.type === 'question') return FormsApi.updateQuestion(formId, snapshot.item.id, { ...snapshot.item, code: values.get('code'), title: values.get('title'), type: values.get('type'), dimensionCode: values.get('dimensionCode') || null, weight: Number(values.get('weight')), required: values.has('required'), expectedVersion: snapshot.item.version });
    return FormsApi.updateOption(formId, snapshot.item.id, { label: values.get('label'), value: values.get('value'), score: values.get('score') === '' ? null : Number(values.get('score')), expectedVersion: snapshot.item.version });
  }

  async function saveProperties(requireConfirmation = true) {
    if (isReadOnly() || mutationPending || !properties.reportValidity()) return false;
    const snapshot = { type: selection.type, item: selection.item ? { ...selection.item } : null, formVersion: form.version };
    const values = new FormData(properties);
    if (requireConfirmation && !await window.ValoraUI.confirm('Salvar estas alterações?')) return false;
    mutationPending = true; clearError(); host.querySelector('[data-save-state]').textContent = 'Salvando…';
    try { await propertyRequest(snapshot, values); propertiesDirty = false; host.querySelector('[data-save-state]').textContent = 'Salvo'; await load(); return true; }
    catch (problem) { host.querySelector('[data-save-state]').textContent = problem.status === 409 ? 'Conflito de versão — recarregue' : 'Erro ao salvar'; fail(problem); return false; }
    finally { mutationPending = false; }
  }

  async function allowSelectionChange() {
    if (!propertiesDirty) return true;
    if (await window.ValoraUI.confirm('Há alterações pendentes. Deseja salvar e continuar?')) return saveProperties(false);
    if (await window.ValoraUI.confirm('Descartar as alterações e continuar?')) { propertiesDirty = false; return true; }
    properties.querySelector('input,textarea,select')?.focus(); return false;
  }

  async function runMutation(action) {
    if (mutationPending) return false;
    mutationPending = true; clearError();
    try { await action(); await load(); return true; }
    catch (problem) { fail(problem); return false; }
    finally { mutationPending = false; }
  }

  function moveContext(type, id) {
    if (type === 'section') return { items: form.sections, containerId: form.currentDraftVersionId };
    if (type === 'question') { const section = form.sections.find(candidate => candidate.questions.some(item => item.id === id)); return { items: section.questions, containerId: section.id }; }
    const question = form.sections.flatMap(section => section.questions).find(candidate => candidate.options.some(item => item.id === id)); return { items: question.options, containerId: question.id };
  }

  function focusIssue(issue) {
    reviewDialog.close();
    const item = allItems().find(candidate => candidate.id === issue.elementId);
    selection = item ? { type: issue.elementType, item } : { type: 'form', item: null };
    render();
    (issue.elementType === 'question' ? host.querySelector(`[data-select="question"][data-id="${issue.elementId}"]`) : properties.querySelector('input,select,textarea'))?.focus();
  }

  async function showReview() {
    clearError(); reviewDialog.showModal(); host.querySelector('[data-review-content]').innerHTML = '<p>Verificando a versão no servidor…</p>';
    try {
      const review = FormsApi.normalize(await FormsApi.reviewPublication(formId));
      const list = (items, emptyText) => items.length ? `<ul>${items.map((item, index) => `<li><button type="button" data-review-issue="${index}" data-kind="${items === review.blockers ? 'blocker' : 'warning'}">${escapeHtml(item.message)}</button></li>`).join('')}</ul>` : `<p>${emptyText}</p>`;
      host.querySelector('[data-review-content]').innerHTML = `<div class="review-metrics"><span><strong>${review.sections}</strong> seções</span><span><strong>${review.respondableQuestions}</strong> perguntas respondíveis</span><span><strong>${review.informationalElements}</strong> informativos</span><span><strong>${review.methodologicalLinks}</strong> vínculos metodológicos</span></div><h3>Impedimentos</h3>${list(review.blockers, 'Nenhum impedimento encontrado.')}<h3>Avisos</h3>${list(review.warnings, 'Nenhum aviso para esta versão.')}`;
      reviewDialog.dataset.review = JSON.stringify(review);
      host.querySelector('[data-confirm-publish]').disabled = !review.canPublish;
    } catch (problem) { host.querySelector('[data-review-content]').innerHTML = `<p class="text-danger">${escapeHtml(problem.message || 'Não foi possível revisar esta versão.')}</p>`; host.querySelector('[data-confirm-publish]').disabled = true; }
  }

  properties.addEventListener('submit', event => { event.preventDefault(); void saveProperties(); });
  properties.addEventListener('input', () => { propertiesDirty = true; host.querySelector('[data-save-state]').textContent = 'Alterações pendentes'; });
  host.addEventListener('click', async event => {
    const trigger = event.target.closest('[data-select],button'); if (!trigger) return;
    const mutation = trigger.matches('[data-add-section],[data-add-question],[data-add-option],[data-delete],[data-move]');
    if (mutation && isReadOnly()) { window.ValoraToast?.warning?.('Versões publicadas ou arquivadas não podem ser alteradas.'); return; }
    if (trigger.dataset.select) { if (!await allowSelectionChange()) return; const item = allItems().find(candidate => candidate.id === trigger.dataset.id); selection = { type: trigger.dataset.select, item }; render(); return; }
    if (mutation && !await allowSelectionChange()) return;
    if (trigger.matches('[data-add-section]')) { await runMutation(() => FormsApi.createSection(formId, { title: 'Nova seção', description: '', position: form.sections.length, expectedVersion: form.draftVersion })); return; }
    if (trigger.dataset.addQuestion) { const section = form.sections.find(item => item.id === trigger.dataset.addQuestion); await runMutation(() => FormsApi.createQuestion(formId, { sectionId: section.id, code: `Q${Date.now().toString().slice(-6)}`, type: 'likert_1_5', title: 'Nova pergunta', required: false, weight: 1, position: section.questions.length, expectedVersion: form.draftVersion })); return; }
    if (trigger.dataset.addOption) { const question = allItems().find(item => item.id === trigger.dataset.addOption); await runMutation(() => FormsApi.createOption(formId, question.id, { label: 'Nova opção', value: `opcao_${Date.now().toString().slice(-5)}`, score: null, position: question.options.length, expectedVersion: form.draftVersion })); return; }
    if (trigger.dataset.delete && await window.ValoraUI.confirm('Excluir este item do rascunho?')) { const item = allItems().find(candidate => candidate.id === trigger.dataset.id); await runMutation(() => trigger.dataset.delete === 'section' ? FormsApi.deleteSection(formId, item.id, { expectedVersion: item.version }) : trigger.dataset.delete === 'question' ? FormsApi.deleteQuestion(formId, item.id, { expectedVersion: item.version }) : FormsApi.deleteOption(formId, item.id, { expectedVersion: item.version })); selection = { type: 'form', item: null }; return; }
    if (trigger.dataset.move) { const context = moveContext(trigger.dataset.move, trigger.dataset.id); const index = context.items.findIndex(item => item.id === trigger.dataset.id); const destination = trigger.dataset.direction === 'up' ? index - 1 : index + 1; if (destination < 0 || destination >= context.items.length) return; await runMutation(() => FormsApi.reorder(formId, { itemId: trigger.dataset.id, itemType: trigger.dataset.move, sourceContainerId: context.containerId, targetContainerId: context.containerId, previousPosition: index, newPosition: destination, expectedVersion: form.draftVersion })); }
  });

  host.querySelector('[data-preview]').addEventListener('click', () => { host.querySelector('[data-preview-content]').innerHTML = `<h1>${escapeHtml(form.name)}</h1><p>${escapeHtml(form.description)}</p>${form.sections.map(section => `<section><h2>${escapeHtml(section.title)}</h2>${section.questions.map(questionMarkup).join('')}</section>`).join('')}`; host.querySelector('[data-preview-dialog]').showModal(); });
  host.querySelector('[data-close-preview]').addEventListener('click', () => host.querySelector('[data-preview-dialog]').close());
  host.querySelectorAll('[data-preview-size]').forEach(button => button.addEventListener('click', () => host.querySelector('[data-preview-content]').classList.toggle('is-mobile', button.dataset.previewSize === 'mobile')));
  host.querySelector('[data-publish]').addEventListener('click', async () => { if (await allowSelectionChange()) await showReview(); });
  host.querySelectorAll('[data-close-review]').forEach(button => button.addEventListener('click', () => reviewDialog.close()));
  host.querySelector('[data-review-content]').addEventListener('click', event => { const button = event.target.closest('[data-review-issue]'); if (!button) return; const review = JSON.parse(reviewDialog.dataset.review); focusIssue(review[button.dataset.kind === 'blocker' ? 'blockers' : 'warnings'][Number(button.dataset.reviewIssue)]); });
  host.querySelector('[data-confirm-publish]').addEventListener('click', async event => { const button = event.currentTarget; if (button.disabled || mutationPending) return; const review = JSON.parse(reviewDialog.dataset.review || '{}'); if (!review.canPublish || !await window.ValoraUI.confirm(`Publicar a versão ${review.versionNumber}? Ela se tornará imutável.`)) return; button.disabled = true; const succeeded = await runMutation(() => FormsApi.publish(formId, { expectedVersion: form.draftVersion })); if (succeeded) { reviewDialog.close(); window.ValoraToast?.success?.('Formulário publicado. A versão foi protegida para preservar o histórico.'); } button.disabled = false; });
  window.addEventListener('beforeunload', event => { if (!propertiesDirty) return; event.preventDefault(); event.returnValue = ''; });
  load().then(() => { if (form && new URLSearchParams(window.location.search).get('preview') === 'true') host.querySelector('[data-preview]').click(); });
})();
