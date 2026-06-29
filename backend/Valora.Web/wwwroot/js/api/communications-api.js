window.CommunicationsApi={
  sendResultEmail:(id,payload)=>AjaxClient.post(`/communications/result/${encodeURIComponent(id)}/send-email`,payload||{}),
  jobs:(status)=>AjaxClient.get('/admin/email-jobs'+(status?`?status=${encodeURIComponent(status)}`:'')),
  process:(batchSize)=>AjaxClient.post('/admin/email-jobs/process',{batchSize:batchSize||10}),
  configStatus:()=>AjaxClient.get('/admin/email/config/status')
};
