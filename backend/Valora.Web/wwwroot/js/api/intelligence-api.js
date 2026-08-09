window.IntelligenceApi = {
  dashboard: () => AjaxClient.get('/bff/intelligence/dashboard'),
  generate: () => AjaxClient.post('/bff/intelligence/generate', {}),
  journey: () => AjaxClient.get('/bff/intelligence/journey'),
  createJourney: payload => AjaxClient.post('/bff/intelligence/journey', payload),
  indicators: () => AjaxClient.get('/bff/intelligence/indicators')
};
