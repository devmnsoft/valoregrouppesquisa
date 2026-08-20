window.FreeDiagnosticsApi={
  list:q=>AjaxClient.get('/bff/admin/free-diagnostics'+(q?'?'+new URLSearchParams(q).toString():'')),
  summary:q=>AjaxClient.get('/bff/admin/free-diagnostics/summary'+(q?'?'+new URLSearchParams(q).toString():'')),
  detail:id=>AjaxClient.get(`/bff/admin/free-diagnostics/${encodeURIComponent(id)}`),
  resendEmail:(id,payload)=>AjaxClient.post(`/bff/admin/free-diagnostics/${encodeURIComponent(id)}/resend-email`,payload||{}),
  regenerateCertificate:(id,payload)=>AjaxClient.post(`/bff/admin/free-diagnostics/${encodeURIComponent(id)}/regenerate-certificate`,payload||{}),
  markCommunicationReviewed:(id,payload)=>AjaxClient.post(`/bff/admin/free-diagnostics/${encodeURIComponent(id)}/mark-communication-reviewed`,payload||{})
};
