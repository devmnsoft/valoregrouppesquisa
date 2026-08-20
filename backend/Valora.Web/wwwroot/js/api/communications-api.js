window.CommunicationsApi={
  sendResultEmail:(id,payload)=>AjaxClient.post(`/bff/communications/result/${encodeURIComponent(id)}/send-email`,payload||{}),
  jobs:(status)=>AjaxClient.get('/bff/admin/email-jobs'+(status?`?status=${encodeURIComponent(status)}`:'')),
  process:(batchSize)=>AjaxClient.post('/bff/admin/email-jobs/process',{batchSize:batchSize||10}),
  configStatus:()=>AjaxClient.get('/bff/admin/email/config/status')
};
