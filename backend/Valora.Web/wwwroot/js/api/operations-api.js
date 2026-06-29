window.OperationsApi={
  health:()=>AjaxClient.get('/admin/operations/health'),
  email:()=>AjaxClient.get('/admin/operations/email'),
  freeSurvey:()=>AjaxClient.get('/admin/operations/free-survey'),
  certificates:()=>AjaxClient.get('/admin/operations/certificates'),
  processQueue:()=>AjaxClient.post('/admin/operations/email/process-queue',{})
};
