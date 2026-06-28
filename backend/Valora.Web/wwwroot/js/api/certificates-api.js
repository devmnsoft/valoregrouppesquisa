window.CertificatesApi={
  validate:code=>AjaxClient.get(`/certificates/${encodeURIComponent(code)}/validate`),
  pdf:id=>AjaxClient.requestBinary('GET',`/responses/${encodeURIComponent(id)}/certificate.pdf`),
  png:id=>AjaxClient.requestBinary('GET',`/responses/${encodeURIComponent(id)}/certificate.png`)
};
