(() => {
  'use strict';

  function initialize(root) {
    const search = root.querySelector('[data-responsible-search]');
    const select = root.querySelector('[data-responsible-select]');
    const status = root.querySelector('[data-responsible-status]');
    const more = root.querySelector('[data-responsible-more]');
    if (!search || !select || !status || !more || root.dataset.responsibleReady) return;
    root.dataset.responsibleReady = 'true';

    const endpoint = root.dataset.responsibleEndpoint || '/ActionCenter/ResponsibleOptions';
    const initialValue = root.dataset.selectedResponsible || select.value;
    const initialText = [...select.options].find(option => option.value === initialValue)?.text || 'Responsável selecionado';
    const state = { page: 0, term: '', request: 0, abort: null, hasMore: false, failedPage: null, failedAppend: false, loaded: 0, selection: initialValue };
    const label = option => option.title || option.label || option.responsibleName || 'Responsável';
    const announce = (message, retry = false) => {
      status.textContent = message;
      more.textContent = retry ? 'Tentar novamente' : 'Carregar mais';
      more.hidden = !retry && !state.hasMore;
      more.disabled = false;
    };
    const preserveCurrent = () => {
      const value = state.selection;
      if (!value) return { value: '', text: '' };
      return { value, text: select.selectedOptions[0]?.text || initialText };
    };

    async function load(term = state.term, append = false, retryPage = null) {
      const preserved = preserveCurrent();
      state.abort?.abort();
      const controller = new AbortController();
      state.abort = controller;
      const request = ++state.request;
      const page = retryPage || (append ? state.page + 1 : 1);
      if (!append) state.term = term;
      more.disabled = true;
      status.textContent = append ? 'Carregando mais responsáveis…' : 'Buscando responsáveis…';
      try {
        const params = new URLSearchParams({ search: state.term, page: String(page), pageSize: '20' });
        if (preserved.value) params.set('includeId', preserved.value);
        const response = await fetch(endpoint + '?' + params, { signal: controller.signal, headers: { Accept: 'application/json' } });
        if (response.status === 401) {
          const here = location.pathname + location.search;
          location.assign('/Account/Login?reason=session-expired&returnUrl=' + encodeURIComponent(here));
          return;
        }
        if (response.status === 403) { announce('Você não tem permissão para consultar estas opções.'); return; }
        if (!response.ok) throw new Error('request-failed');
        const data = await response.json();
        if (request !== state.request) return;
        if (!append) select.replaceChildren(new Option(select.dataset.emptyLabel || 'Todos', ''));
        const rows = data.items || data.Items || [];
        for (const item of rows) {
          const id = String(item.id || item.Id);
          if (![...select.options].some(option => option.value === id)) select.add(new Option(label(item), id));
        }
        if (preserved.value && ![...select.options].some(option => option.value === preserved.value)) select.add(new Option(preserved.text, preserved.value));
        select.value = preserved.value;
        state.page = page;
        state.failedPage = null;
        state.hasMore = Boolean(data.hasMore ?? data.HasMore);
        const total = Number(data.total ?? data.Total ?? 0);
        state.loaded = append ? Math.min(total, state.loaded + rows.length) : Math.min(total, rows.length);
        announce(total ? `${state.loaded} de ${total} responsáveis carregados.` : 'Nenhum responsável encontrado.');
      } catch (error) {
        if (error.name === 'AbortError' || request !== state.request) return;
        state.failedPage = page;
        state.failedAppend = append;
        announce(append ? 'Falha ao carregar mais responsáveis. Tente novamente.' : 'Falha na busca. Sua seleção foi preservada.', true);
      }
    }

    let timer;
    search.addEventListener('input', () => {
      clearTimeout(timer);
      timer = setTimeout(() => load(search.value.trim()), 300);
    });
    select.addEventListener('change', () => { state.selection = select.value; });
    search.addEventListener('keydown', event => {
      if (event.key === 'ArrowDown') { event.preventDefault(); select.focus(); }
    });
    more.addEventListener('click', () => state.failedPage ? load(state.term, state.failedAppend, state.failedPage) : load(state.term, true));
    load();
  }

  document.querySelectorAll('[data-responsible-selector]').forEach(initialize);
})();
