(function(global){
  const cfg=global.ValoraConfig||global.VALORA_CONFIG||{};
  const provider=cfg.DATA_PROVIDER||'firebase';
  const api=global.ValoraApiClient;
  global.ValoraApiRepository={
    provider,
    isApiEnabled(){ return provider==='api'||provider==='hybrid'; },
    publicPlans(){ return this.isApiEnabled()?api.publicPlans():Promise.reject(new Error('DATA_PROVIDER não está em modo api/hybrid.')); },
    registerCompany(payload){ return api.registerCompany(payload).then(r=>{ if(r?.token) localStorage.setItem('valora_api_token',r.token); return r; }); },
    login(payload){ return api.login(payload).then(r=>{ if(r?.token) localStorage.setItem('valora_api_token',r.token); return r; }); },
    validatePublicSurvey(surveyId,payload){ return api.validatePublicSurvey(surveyId,payload); },
    submitPublicSurvey(surveyId,payload){ return api.submitPublicSurvey(surveyId,payload); },
    submitPublicSurveyResponse(surveyId,payload){ return api.submitPublicSurvey(surveyId,payload); },
    loadPublicResult(responseId,resultToken){ return api.loadPublicResult(responseId,resultToken); },
    downloadCertificatePdf(responseId){ return `${api.baseUrl}/responses/${encodeURIComponent(responseId)}/certificate.pdf`; },
    downloadCertificatePng(responseId){ return `${api.baseUrl}/responses/${encodeURIComponent(responseId)}/certificate.png`; }
  };
})(window);
