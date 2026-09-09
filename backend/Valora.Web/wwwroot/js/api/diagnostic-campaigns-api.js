(function () {
  const root = '/bff/diagnostics';
  const resource = id => `${root}/${encodeURIComponent(id)}/campaign`;
  window.DiagnosticCampaignsApi = Object.freeze({
    list: () => AjaxClient.get('/bff/diagnostic-campaigns'),
    get: surveyId => AjaxClient.get(resource(surveyId)),
    create: (surveyId, data) => AjaxClient.post(resource(surveyId), data),
    schedule: surveyId => AjaxClient.post(`${resource(surveyId)}/schedule`, {}),
    send: surveyId => AjaxClient.post(`${resource(surveyId)}/send`, {}),
    pause: surveyId => AjaxClient.post(`${resource(surveyId)}/pause`, {}),
    resume: surveyId => AjaxClient.post(`${resource(surveyId)}/resume`, {}),
    close: (surveyId, data = {}) => AjaxClient.post(`${resource(surveyId)}/close`, data),
    cancel: surveyId => AjaxClient.post(`${resource(surveyId)}/cancel`, {}),
    resendFailures: surveyId => AjaxClient.post(`${resource(surveyId)}/resend-failures`, {}),
    recipients: surveyId => AjaxClient.get(`${resource(surveyId)}/recipients`),
    metrics: surveyId => AjaxClient.get(`${resource(surveyId)}/metrics`),
    history: surveyId => AjaxClient.get(`${resource(surveyId)}/history`)
  });
}());

