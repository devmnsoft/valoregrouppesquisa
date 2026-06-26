(function(global){
  const provider=(global.VALORA_CONFIG&&global.VALORA_CONFIG.DATA_PROVIDER)||'firebase';
  const api=global.ValoraApiClient;
  global.ValoraApiRepository={
    provider,
    isApiEnabled(){ return provider==='api'||provider==='hybrid'; },
    publicPlans(){ return this.isApiEnabled()?api.get('/plans/public'):Promise.reject(new Error('DATA_PROVIDER não está em modo api/hybrid.')); },
    registerCompany(payload){ return api.post('/auth/register-company',payload).then(r=>{ if(r.token) localStorage.setItem('valora_api_token',r.token); return r; }); },
    login(payload){ return api.post('/auth/login',payload).then(r=>{ if(r.token) localStorage.setItem('valora_api_token',r.token); return r; }); }
  };
})(window);
