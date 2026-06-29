window.CertificatesApi={
  validate:code=>AjaxClient.get(`/certificates/validate/${encodeURIComponent(code)}`),
  pdf:id=>AjaxClient.requestBinary('GET',`/responses/${encodeURIComponent(id)}/certificate.pdf`),
  png:id=>AjaxClient.requestBinary('GET',`/responses/${encodeURIComponent(id)}/certificate.png`)
};
