window.OperationsApi={
  health:()=>AjaxClient.get('/bff/admin/operations/health'),
  email:()=>AjaxClient.get('/bff/admin/operations/email'),
  freeSurvey:()=>AjaxClient.get('/bff/admin/operations/free-survey'),
  certificates:()=>AjaxClient.get('/bff/admin/operations/certificates'),
  processQueue:()=>AjaxClient.post('/bff/admin/operations/email/process-queue',{})
};
