window.ReportsApi = {
  list: () => AjaxClient.get('/bff/reports/generated'),
  get: id => AjaxClient.get(`/bff/reports/generated/${encodeURIComponent(id)}`),
  generateSurvey: (surveyId, format) => AjaxClient.post(`/bff/reports/surveys/${encodeURIComponent(surveyId)}/generate`, { format }),
  generateResponse: (responseId, format) => AjaxClient.post(`/bff/reports/responses/${encodeURIComponent(responseId)}/generate`, { format }),
  downloadUrl: id => `/bff/reports/generated/${encodeURIComponent(id)}/download`,
  generateOrganization: format => AjaxClient.post('/bff/reports/organization/generate', { format })
};
