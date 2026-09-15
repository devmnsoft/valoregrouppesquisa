(function(){
  const base = '/bff/forms';
  function normalizeList(response) {
    const payload = response && Object.prototype.hasOwnProperty.call(response, 'data') ? response.data : response;
    if (!payload || !Array.isArray(payload.items)) {
      throw new TypeError('Contrato inválido da listagem de formulários: o envelope paginado não contém items.');
    }
    const integers = ['total', 'page', 'pageSize', 'totalPages'];
    integers.forEach(name => {
      if (!Number.isInteger(payload[name]) || payload[name] < 0) {
        throw new TypeError(`Contrato inválido da listagem de formulários: ${name} não é um inteiro válido.`);
      }
    });
    return {
      items: payload.items,
      total: payload.total,
      page: payload.page,
      pageSize: payload.pageSize,
      totalPages: payload.totalPages,
      hasPreviousPage: Boolean(payload.hasPreviousPage),
      hasNextPage: Boolean(payload.hasNextPage),
      categories: Array.isArray(payload.categories) ? payload.categories : [],
      metrics: payload.metrics || null
    };
  }
  window.FormsApi = {
    normalize: response => response && response.data ? response.data : response,
    normalizeList,
    list: query => AjaxClient.get(base + (query || '')),
    get: id => AjaxClient.get(base + '/' + encodeURIComponent(id)),
    create: data => AjaxClient.post(base, data),
    update: (id, data) => AjaxClient.put(base + '/' + encodeURIComponent(id), data),
    archive: (id, data) => AjaxClient.delete(base + '/' + encodeURIComponent(id), data),
    createVersion: (id, data) => AjaxClient.post(base + '/' + encodeURIComponent(id) + '/versions', data),
    publish: (id, data) => AjaxClient.post(base + '/' + encodeURIComponent(id) + '/publish', data),
    reviewPublication: id => AjaxClient.get(base + '/' + encodeURIComponent(id) + '/publication-review'),
    listDimensions: id => AjaxClient.get(base + '/' + encodeURIComponent(id) + '/dimensions'),
    reorder: (id, data) => AjaxClient.post(base + '/' + encodeURIComponent(id) + '/reorder', data),
    createSection: (id, data) => AjaxClient.post(`${base}/${encodeURIComponent(id)}/sections`, data),
    updateSection: (id, sectionId, data) => AjaxClient.put(`${base}/${encodeURIComponent(id)}/sections/${encodeURIComponent(sectionId)}`, data),
    deleteSection: (id, sectionId, data) => AjaxClient.delete(`${base}/${encodeURIComponent(id)}/sections/${encodeURIComponent(sectionId)}`, data),
    createQuestion: (id, data) => AjaxClient.post(`${base}/${encodeURIComponent(id)}/questions`, data),
    updateQuestion: (id, questionId, data) => AjaxClient.put(`${base}/${encodeURIComponent(id)}/questions/${encodeURIComponent(questionId)}`, data),
    deleteQuestion: (id, questionId, data) => AjaxClient.delete(`${base}/${encodeURIComponent(id)}/questions/${encodeURIComponent(questionId)}`, data),
    createOption: (id, questionId, data) => AjaxClient.post(`${base}/${encodeURIComponent(id)}/questions/${encodeURIComponent(questionId)}/options`, data),
    updateOption: (id, optionId, data) => AjaxClient.put(`${base}/${encodeURIComponent(id)}/options/${encodeURIComponent(optionId)}`, data),
    deleteOption: (id, optionId, data) => AjaxClient.delete(`${base}/${encodeURIComponent(id)}/options/${encodeURIComponent(optionId)}`, data)
  };
}());
