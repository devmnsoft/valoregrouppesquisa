window.CertificatesApi={
  validate:code=>AjaxClient.get(`/bff/public/certificates/validate/${encodeURIComponent(code)}`),
  pdf:id=>AjaxClient.requestBinary('GET',`/bff/responses/${encodeURIComponent(id)}/certificate.pdf`),
  png:id=>AjaxClient.requestBinary('GET',`/bff/responses/${encodeURIComponent(id)}/certificate.png`)
};
