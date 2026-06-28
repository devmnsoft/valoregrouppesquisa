window.PublicSurveyApi={
  validate:(id,inviteToken)=>AjaxClient.post(`/public/surveys/${encodeURIComponent(id)}/validate`,inviteToken?{inviteToken}:{}),
  submit:(id,p)=>AjaxClient.post(`/public/surveys/${encodeURIComponent(id)}/responses`,p)
};
