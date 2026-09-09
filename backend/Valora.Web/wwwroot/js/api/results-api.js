window.ResultsApi={
  public:id=>AjaxClient.get(`/bff/public/results/${encodeURIComponent(id)}`),
  certificatePdf:id=>AjaxClient.requestBinary('GET',`/bff/public/results/${encodeURIComponent(id)}/certificate.pdf`),
  certificatePng:id=>AjaxClient.requestBinary('GET',`/bff/public/results/${encodeURIComponent(id)}/certificate.png`),
  resendEmail:(id,toEmail)=>AjaxClient.post(`/bff/public/results/${encodeURIComponent(id)}/email`,{toEmail})
};
