window.ResultsApi={
  public:(id,resultToken)=>AjaxClient.post(`/public/results/${encodeURIComponent(id)}`,resultToken?{resultToken}:{}),
  certificatePdf:id=>AjaxClient.requestBinary('GET',`/responses/${encodeURIComponent(id)}/certificate.pdf`),
  certificatePng:id=>AjaxClient.requestBinary('GET',`/responses/${encodeURIComponent(id)}/certificate.png`)
};
