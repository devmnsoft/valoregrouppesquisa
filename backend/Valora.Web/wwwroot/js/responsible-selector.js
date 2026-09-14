(() => {
  'use strict';
  const script = document.currentScript;
  const root = script?.closest('main, [data-action-center-module]') || document;
  const search = root.querySelector('[data-responsible-search]');
  const select = root.querySelector('[data-responsible-select]');
  const status = root.querySelector('[data-responsible-status]');
  const more = root.querySelector('[data-responsible-more]');
  if (!search || !select || !status || !more) return;

  const selected = script?.dataset.selectedResponsible || select.value;
  const state = { page: 0, term: '', request: 0, abort: null, hasMore: false };
  const label = option => option.title || option.label || option.responsibleName || 'Responsável';
  const renderStatus = (message, retry = false) => {
    status.textContent = message;
    more.textContent = retry ? 'Tentar novamente' : 'Carregar mais';
    more.hidden = !retry && !state.hasMore;
    more.disabled = false;
  };
  async function load(term = state.term, append = false) {
    state.abort?.abort();
    state.abort = new AbortController();
    const request = ++state.request;
    const page = append ? state.page + 1 : 1;
    if (!append) state.term = term;
    more.disabled = true;
    status.textContent = 'Carregando responsáveis…';
    try {
      const params = new URLSearchParams({ search: term, page: String(page), pageSize: '20' });
      if (selected) params.set('includeId', selected);
      const response = await fetch('/ActionCenter/ResponsibleOptions?' + params, { signal: state.abort.signal, headers: { Accept: 'application/json' } });
      if (!response.ok) throw new Error('Não foi possível carregar os responsáveis.');
      const data = await response.json();
      if (request !== state.request) return;
      const previous = select.value || selected;
      if (!append) select.replaceChildren(new Option(select.dataset.emptyLabel || 'Todos', ''));
      for (const item of data.items || data.Items || []) {
        const id = item.id || item.Id;
        if (![...select.options].some(option => option.value === id)) select.add(new Option(label(item), id));
      }
      select.value = [...select.options].some(option => option.value === previous) ? previous : '';
      state.page = page;
      state.hasMore = Boolean(data.hasMore ?? data.HasMore);
      const total = data.total ?? data.Total ?? 0;
      renderStatus(total ? `${select.options.length - 1} de ${total} responsáveis carregados.` : 'Nenhum responsável encontrado.');
    } catch (error) {
      if (error.name !== 'AbortError' && request === state.request) renderStatus('Falha ao carregar. Sua seleção foi preservada.', true);
    }
  }
  let timer;
  search.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(() => load(search.value.trim()), 300);
  });
  more.addEventListener('click', () => load(state.term, state.hasMore));
  load();
})();
