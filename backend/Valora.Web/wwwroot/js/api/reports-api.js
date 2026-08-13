window.ReportsApi = {
  list: () => AjaxClient.get('/bff/reports/generated'),
  get: id => AjaxClient.get(`/bff/reports/generated/${encodeURIComponent(id)}`),
  generateSurvey: (surveyId, format) => AjaxClient.post(`/bff/reports/surveys/${encodeURIComponent(surveyId)}/generate`, { format }),
  generateOrganization: format => AjaxClient.post('/bff/reports/organization/generate', { format })
};
