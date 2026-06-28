window.PublicSurveyApi={validate:id=>AjaxClient.post(`/public/surveys/${encodeURIComponent(id)}/validate`,{}),submit:(id,p)=>AjaxClient.post(`/public/surveys/${encodeURIComponent(id)}/responses`,p)};
