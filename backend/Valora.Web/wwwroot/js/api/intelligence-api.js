window.IntelligenceApi = {
  dashboard: () => AjaxClient.get('/intelligence/dashboard'),
  generate: () => AjaxClient.post('/intelligence/generate', {}),
  journey: () => AjaxClient.get('/intelligence/journey'),
  createJourney: payload => AjaxClient.post('/intelligence/journey', payload),
  indicators: () => AjaxClient.get('/intelligence/indicators')
};
