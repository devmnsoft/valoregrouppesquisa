window.ResultsApi={
  public:(id,resultToken)=>AjaxClient.get(`/bff/public/results/${encodeURIComponent(id)}?token=${encodeURIComponent(resultToken||'')}`),
  certificatePdf:(id,resultToken)=>AjaxClient.requestBinary('GET',`/bff/public/results/${encodeURIComponent(id)}/certificate.pdf?token=${encodeURIComponent(resultToken||'')}`),
  certificatePng:(id,resultToken)=>AjaxClient.requestBinary('GET',`/bff/public/results/${encodeURIComponent(id)}/certificate.png?token=${encodeURIComponent(resultToken||'')}`),
  resendEmail:(id,resultToken,toEmail)=>AjaxClient.post(`/bff/public/results/${encodeURIComponent(id)}/email?token=${encodeURIComponent(resultToken||'')}`,{toEmail})
};
