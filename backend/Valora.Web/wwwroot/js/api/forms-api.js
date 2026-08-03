(function(){
  const base = '/bff/forms';
  window.FormsApi = {
    normalize: response => response && response.data ? response.data : response,
    list: query => AjaxClient.get(base + (query || '')),
    get: id => AjaxClient.get(base + '/' + encodeURIComponent(id)),
    create: data => AjaxClient.post(base, data),
    update: (id, data) => AjaxClient.put(base + '/' + encodeURIComponent(id), data),
    archive: (id, data) => AjaxClient.delete(base + '/' + encodeURIComponent(id), data),
    publish: (id, data) => AjaxClient.post(base + '/' + encodeURIComponent(id) + '/publish', data),
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
