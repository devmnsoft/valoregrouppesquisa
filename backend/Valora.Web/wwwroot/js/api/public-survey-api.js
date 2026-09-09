window.PublicSurveyApi={
  // Sprint 53: publicToken é o token público compartilhável. tokenHash nunca deve ser usado em URL.
  validate:id=>AjaxClient.get(`/bff/public/surveys/${encodeURIComponent(id)}`),
  submit:(id,p)=>AjaxClient.post(`/bff/public/surveys/${encodeURIComponent(id)}/responses`,p)
};
