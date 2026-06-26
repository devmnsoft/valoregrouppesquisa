(function(global){
  const cfg=global.VALORA_CONFIG||{};
  const base=(cfg.API_BASE_URL||'http://localhost:5080').replace(/\/$/,'');
  async function request(path, options={}){ const token=global.localStorage&&localStorage.getItem('valora_api_token'); const headers={ 'Content-Type':'application/json', ...(options.headers||{}) }; if(token) headers.Authorization=`Bearer ${token}`; const res=await fetch(`${base}${path}`,{...options,headers}); if(!res.ok) throw new Error(`API ${res.status}: ${await res.text()}`); return res.status===204?null:res.json(); }
  global.ValoraApiClient={ baseUrl:base, request, get:p=>request(p), post:(p,b)=>request(p,{method:'POST',body:JSON.stringify(b)}), patch:(p,b)=>request(p,{method:'PATCH',body:JSON.stringify(b)}) };
})(window);
