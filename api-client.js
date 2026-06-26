(function(global){
  'use strict';
  let memoryToken='';
  const TOKEN_KEY='valora_api_token';
  function storage(){try{return global.sessionStorage||null;}catch(_){return null;}}
  function getToken(){return storage()?.getItem(TOKEN_KEY)||memoryToken||'';}
  function setToken(token){memoryToken=String(token||'');try{if(memoryToken)storage()?.setItem(TOKEN_KEY,memoryToken);else storage()?.removeItem(TOKEN_KEY);}catch(_){}}
  function clearToken(){memoryToken='';try{storage()?.removeItem(TOKEN_KEY);}catch(_){}}
  function baseUrl(){return (global.ValoraConfig?.API_BASE_URL||global.VALORA_CONFIG?.API_BASE_URL||'').replace(/\/+$/,'');}
  async function apiFetch(path,options={}){
    const base=baseUrl();
    if(!base)throw new Error('API indisponível.');
    const token=getToken();
    let response;
    try{
      response=await fetch(`${base}${path}`,{...options,headers:{'Content-Type':'application/json',...(token?{Authorization:`Bearer ${token}`}:{ }),...(options.headers||{})}});
    }catch(_){throw new Error('Não foi possível conectar à API.');}
    const contentType=String(response.headers.get('content-type')||'').toLowerCase();
    const text=await response.text();
    if(!contentType.includes('application/json'))throw new Error('A API retornou formato inesperado.');
    let body={};
    try{body=text?JSON.parse(text):{};}catch(_){throw new Error('A API retornou JSON inválido.');}
    if(!response.ok||body.ok===false)throw new Error(body.message||`API retornou HTTP ${response.status}.`);
    return body;
  }
  const json=(method,path,body)=>apiFetch(path,{method,body:body===undefined?undefined:JSON.stringify(body)});
  global.ValoraApiClient=Object.freeze({
    getBaseUrl:baseUrl,
    request:apiFetch,
    get:path=>apiFetch(path,{method:'GET'}),
    post:(path,body)=>json('POST',path,body),
    put:(path,body)=>json('PUT',path,body),
    patch:(path,body)=>json('PATCH',path,body),
    delete:path=>apiFetch(path,{method:'DELETE'}),
    setToken,clearToken,getToken
  });
})(window);
