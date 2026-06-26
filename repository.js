(function(){
'use strict';

const config=window.ValoraConfig||{};
function currentDataProvider(){return ['firebase','api','hybrid'].includes(config.DATA_PROVIDER)?config.DATA_PROVIDER:'firebase';}
function shouldUseApi(){return currentDataProvider()==='api';}
function shouldUseFirebase(){return currentDataProvider()==='firebase';}
function shouldUseHybrid(){return currentDataProvider()==='hybrid';}
function primaryProvider(){return ['firebase','api'].includes(config.HYBRID_PRIMARY_PROVIDER)?config.HYBRID_PRIMARY_PROVIDER:'firebase';}
function firebaseRepository(){return window.ValoraFirebaseRepository||window.ValoraLocalRepository;}
function apiRepository(){return window.ValoraApiRepository;}
function provider(name){if(name==='api')return apiRepository();return firebaseRepository();}
function selectedProvider(){if(shouldUseApi())return provider('api');if(shouldUseHybrid())return provider(primaryProvider());return provider('firebase');}
function secondaryProvider(){return primaryProvider()==='api'?provider('firebase'):provider('api');}
function comparePayload(a,b){try{return JSON.stringify(a)===JSON.stringify(b);}catch(_){return false;}}
function recordDivergence(method,primary,secondary){const entry={method,equal:comparePayload(primary,secondary),createdAt:new Date().toISOString(),severity:'warning'};window.ValoraHybridDiagnostics=window.ValoraHybridDiagnostics||[];window.ValoraHybridDiagnostics.unshift(entry);window.ValoraHybridDiagnostics=window.ValoraHybridDiagnostics.slice(0,50);if(!entry.equal)console.warn('[Valora Pulse] Divergência hybrid não crítica',entry);return entry;}
async function read(method,args){const p=selectedProvider();if(!p?.[method])throw new Error(`Provider sem método ${method}.`);const result=await p[method](...args);if(shouldUseHybrid()){const s=secondaryProvider();if(s?.[method])Promise.resolve().then(()=>s[method](...args)).then(other=>recordDivergence(method,result,other)).catch(error=>recordDivergence(method,result,{error:error.message}));}return result;}
async function write(method,args){const p=selectedProvider();if(!p?.[method])throw new Error(`Provider sem método ${method}.`);return p[method](...args);}
function certUrl(method,args){const p=selectedProvider();if(!p?.[method])return '';return p[method](...args);}
function alias(method,fallback){return async function(...args){return read(method,args).catch(err=>{if(fallback)return fallback(...args);throw err;});};}
const base=firebaseRepository();
if(!selectedProvider())throw new Error(`DATA_PROVIDER=${currentDataProvider()} indisponível.`);
if(shouldUseApi()&&!apiRepository())throw new Error('DATA_PROVIDER=api requer ValoraApiRepository carregado.');
if(shouldUseHybrid()&&!['firebase','api'].includes(primaryProvider()))throw new Error('DATA_PROVIDER=hybrid requer HYBRID_PRIMARY_PROVIDER firebase ou api.');

window.ValoraRepository=Object.freeze({
  ...base,
  currentDataProvider,shouldUseApi,shouldUseFirebase,shouldUseHybrid,
  login:(payload)=>write('login',[payload]),
  registerCompany:(payload)=>write(apiRepository()?.registerCompany?'registerCompany':'registerCompanyAccount',[payload]),
  registerCompanyAccount:(payload)=>write(apiRepository()?.registerCompany?'registerCompany':'registerCompanyAccount',[payload]),
  getMe:()=>read('getMe',[]),
  getPublicPlans:()=>read('getPublicPlans',[]),
  validatePublicSurvey:(payload)=>read('validatePublicSurvey',[payload]),
  submitPublicSurveyResponse:(payload)=>write('submitPublicSurveyResponse',[payload]),
  loadPublicResult:(responseId,resultToken)=>read('loadPublicResult',[responseId,resultToken]),
  downloadCertificatePdf:(responseId)=>certUrl('downloadCertificatePdf',[responseId]),
  downloadCertificatePng:(responseId)=>certUrl('downloadCertificatePng',[responseId]),
  getMigrationStatus:()=>read('getMigrationStatus',[]),
  getHybridDiagnostics:()=>window.ValoraHybridDiagnostics||[]
});
})();
