window.PublicSurveyApi={
  // Sprint 53: publicToken é o token público compartilhável. tokenHash nunca deve ser usado em URL.
  validate:(id,publicToken)=>AjaxClient.post(`/public/surveys/${encodeURIComponent(id)}/validate`,publicToken?{publicToken,inviteToken:publicToken,token:publicToken}:{}),
  submit:(id,p)=>AjaxClient.post(`/public/surveys/${encodeURIComponent(id)}/responses`,p)
};
