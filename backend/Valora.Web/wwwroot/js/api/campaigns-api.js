(function () {
  const root = '/bff/campaigns';
  window.CampaignsApi = Object.freeze({
    list: (params = {}) => {
      const query = new URLSearchParams(params).toString();
      return AjaxClient.get(query ? `${root}?${query}` : root);
    },
    get: id => AjaxClient.get(`${root}/${encodeURIComponent(id)}`),
    create: data => AjaxClient.post(root, data),
    update: (id, data) => AjaxClient.put(`${root}/${encodeURIComponent(id)}`, data),
    schedule: id => AjaxClient.post(`${root}/${encodeURIComponent(id)}/schedule`, {}),
    start: id => AjaxClient.post(`${root}/${encodeURIComponent(id)}/start`, {}),
    pause: id => AjaxClient.post(`${root}/${encodeURIComponent(id)}/pause`, {}),
    resume: id => AjaxClient.post(`${root}/${encodeURIComponent(id)}/resume`, {}),
    close: (id, data = {}) => AjaxClient.post(`${root}/${encodeURIComponent(id)}/close`, data),
    cancel: id => AjaxClient.post(`${root}/${encodeURIComponent(id)}/cancel`, {}),
    remove: id => AjaxClient.delete(`${root}/${encodeURIComponent(id)}`)
  });
}());
