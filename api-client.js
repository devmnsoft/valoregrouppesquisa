(function(global){
  const cfg=global.ValoraConfig||global.VALORA_CONFIG||{};
  const base=(cfg.API_BASE_URL||'http://localhost:5080').replace(/\/+$/,'');
  async function request(path, options={}){
    const token=global.localStorage&&localStorage.getItem('valora_api_token');
    const headers={ 'Content-Type':'application/json', ...(options.headers||{}) };
    if(token) headers.Authorization=`Bearer ${token}`;
    const res=await fetch(`${base}${path}`,{...options,headers});
    const text=await res.text();
    if(!res.ok) throw new Error(`API ${res.status}: ${text}`);
    return res.status===204||!text?null:JSON.parse(text);
  }
  global.ValoraApiClient={
    baseUrl:base,
    request,
    get:p=>request(p),
    post:(p,b)=>request(p,{method:'POST',body:JSON.stringify(b)}),
    patch:(p,b)=>request(p,{method:'PATCH',body:JSON.stringify(b)}),
    login:payload=>request('/auth/login',{method:'POST',body:JSON.stringify(payload)}),
    registerCompany:payload=>request('/auth/register-company',{method:'POST',body:JSON.stringify(payload)}),
    publicPlans:()=>request('/plans/public'),
    validatePublicSurvey:(surveyId,payload)=>request(`/public/surveys/${encodeURIComponent(surveyId)}/validate`,{method:'POST',body:JSON.stringify(payload)}),
    submitPublicSurvey:(surveyId,payload)=>request(`/public/surveys/${encodeURIComponent(surveyId)}/responses`,{method:'POST',body:JSON.stringify(payload)}),
    loadPublicResult:(responseId,resultToken)=>request(`/public/results/${encodeURIComponent(responseId)}`,{method:'POST',body:JSON.stringify({resultToken})})
  };
})(window);
