window.IntelligenceApi = {
  dashboard: () => AjaxClient.get('/bff/intelligence/dashboard'),
  generate: () => AjaxClient.post('/bff/intelligence/generate', {}),
  journey: () => AjaxClient.get('/bff/intelligence/journey'),
  createJourney: payload => AjaxClient.post('/bff/intelligence/journey', payload),
  indicators: () => AjaxClient.get('/bff/intelligence/indicators')
  ,evolution: () => AjaxClient.get('/bff/intelligence/evolution')
  ,heatmap: () => AjaxClient.get('/bff/intelligence/heatmap')
  ,actions: () => AjaxClient.get('/bff/intelligence/action-plans')
  ,createAction: payload => AjaxClient.post('/bff/intelligence/action-plans', payload)
  ,updateAction: (id, payload) => AjaxClient.patch(`/bff/intelligence/action-plans/${encodeURIComponent(id)}`, payload)
  ,actionHistory: id => AjaxClient.get(`/bff/intelligence/action-plans/${encodeURIComponent(id)}/history`)
  ,deleteAction: id => AjaxClient.delete(`/bff/intelligence/action-plans/${encodeURIComponent(id)}`)
};
