(function(){
  const base = '/bff/forms';
  window.FormsApi = {
    normalize: response => response && response.data ? response.data : response,
    list: query => AjaxClient.get(base + (query || '')),
    get: id => AjaxClient.get(base + '/' + encodeURIComponent(id)),
    create: data => AjaxClient.post(base, data),
    update: (id, data) => AjaxClient.put(base + '/' + encodeURIComponent(id), data),
    publish: (id, data) => AjaxClient.post(base + '/' + encodeURIComponent(id) + '/publish', data),
    reorder: (id, data) => AjaxClient.post(base + '/' + encodeURIComponent(id) + '/reorder', data)
  };
}());
