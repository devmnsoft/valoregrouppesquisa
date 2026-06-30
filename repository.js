(function(){
'use strict';

const config=window.ValoraConfig||{};
function currentDataProvider(){return ['firebase','api','hybrid'].includes(config.DATA_PROVIDER)?config.DATA_PROVIDER:'firebase';}
function isApiProvider(){return currentDataProvider()==='api';}
function isFirebaseProvider(){return currentDataProvider()==='firebase';}
function isHybridProvider(){return currentDataProvider()==='hybrid';}
function hybridPrimaryProvider(){return ['firebase','api'].includes(config.HYBRID_PRIMARY_PROVIDER)?config.HYBRID_PRIMARY_PROVIDER:'firebase';}
const shouldUseApi=isApiProvider;
const shouldUseFirebase=isFirebaseProvider;
const shouldUseHybrid=isHybridProvider;
const primaryProvider=hybridPrimaryProvider;
function firebaseRepository(){return window.ValoraFirebaseRepository||window.ValoraLocalRepository;}
function apiRepository(){return window.ValoraApiRepository;}
function provider(name){if(name==='api')return apiRepository();return firebaseRepository();}
function selectedProvider(){if(shouldUseApi())return provider('api');if(shouldUseHybrid())return provider(primaryProvider());return provider('firebase');}
function secondaryProvider(){return primaryProvider()==='api'?provider('firebase'):provider('api');}
function allowApiProductionCutover(){return config.ALLOW_API_PRODUCTION_CUTOVER===true;}
function firebaseOnlyPublicOps(){return isFirebaseProvider()&&!allowApiProductionCutover();}
function comparePayload(a,b){try{return JSON.stringify(a)===JSON.stringify(b);}catch(_){return false;}}
async function hybridCompare(label,primaryData,secondaryData,options={}){return recordDivergence(label,primaryData,secondaryData,options);}
function stableHash(value){try{let h=0;const text=JSON.stringify(value,Object.keys(value||{}).sort());for(let i=0;i<text.length;i++)h=((h<<5)-h)+text.charCodeAt(i)|0;return String(h);}catch(_){return 'hash-error';}}
function recordDivergence(method,primary,secondary,options={}){const equal=comparePayload(primary,secondary);const entry={id:`diag_${Date.now()}_${Math.random().toString(36).slice(2,8)}`,label:method,method,equal,severity:equal?'info':(options.severity||'warning'),primaryProvider:primaryProvider(),secondaryProvider:primaryProvider()==='api'?'firebase':'api',summary:equal?'Dados equivalentes':'Divergência hybrid não crítica',primaryHash:stableHash(primary),secondaryHash:stableHash(secondary),createdAt:new Date().toISOString()};window.ValoraHybridDiagnostics=window.ValoraHybridDiagnostics||[];window.ValoraHybridDiagnostics.unshift(entry);window.ValoraHybridDiagnostics=window.ValoraHybridDiagnostics.slice(0,50);if(window.state){window.state.migrationDiagnostics=Array.isArray(window.state.migrationDiagnostics)?window.state.migrationDiagnostics:[];window.state.migrationDiagnostics.unshift(entry);window.state.migrationDiagnostics=window.state.migrationDiagnostics.slice(0,50);}try{localStorage.setItem('valora_migration_diagnostics',JSON.stringify(window.ValoraHybridDiagnostics));}catch(_){}if(!entry.equal)console.warn('[Valora Pulse] Divergência hybrid não crítica',entry);return entry;}
async function read(method,args){if(firebaseOnlyPublicOps()&&['validatePublicSurvey','loadPublicResult'].includes(method)){const p=firebaseRepository();if(!p?.[method])throw new Error(`Firebase provider não possui método ${method}.`);return p[method](...args);}
const p=selectedProvider();if(!p?.[method])throw new Error(`Provider ${currentDataProvider()==='hybrid'?primaryProvider():currentDataProvider()} não possui método ${method}.`);const result=await p[method](...args);if(shouldUseHybrid()){const s=secondaryProvider();if(s?.[method])Promise.resolve().then(()=>s[method](...args)).then(other=>recordDivergence(method,result,other)).catch(error=>recordDivergence(method,result,{error:error.message}));}return result;}
async function write(method,args){if(firebaseOnlyPublicOps()&&['submitPublicSurveyResponse','sendResultEmail'].includes(method)){const p=firebaseRepository();if(!p?.[method])throw new Error(`Firebase provider não possui método ${method}.`);return p[method](...args);}
const p=selectedProvider();if(!p?.[method])throw new Error(`Provider ${currentDataProvider()==='hybrid'?primaryProvider():currentDataProvider()} não possui método ${method}.`);return p[method](...args);}
function certUrl(method,args){const p=selectedProvider();if(!p?.[method])return '';return p[method](...args);}
function alias(method,fallback){return async function(...args){return read(method,args).catch(err=>{if(fallback)return fallback(...args);throw err;});};}
const base=firebaseRepository();
if(!selectedProvider())throw new Error(`DATA_PROVIDER=${currentDataProvider()} indisponível.`);
if(shouldUseApi()&&!apiRepository())throw new Error('DATA_PROVIDER=api requer ValoraApiRepository carregado.');
if(shouldUseHybrid()&&!['firebase','api'].includes(primaryProvider()))throw new Error('DATA_PROVIDER=hybrid requer HYBRID_PRIMARY_PROVIDER firebase ou api.');

window.ValoraRepository=Object.freeze({
  ...base,
  currentDataProvider,isApiProvider,isFirebaseProvider,isHybridProvider,hybridPrimaryProvider,shouldUseApi,shouldUseFirebase,shouldUseHybrid,hybridCompare,
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
  sendResultEmail:(responseId,payload)=>write('sendResultEmail',[responseId,payload||{}]),
  createClient:(payload)=>write(selectedProvider()?.createClient?'createClient':'createOrganization',[payload]),
  updateClient:(id,payload)=>write(selectedProvider()?.updateClient?'updateClient':'updateOrganization',[id,payload]),
  createUser:(payload)=>write(selectedProvider()?.createUser?'createUser':'createUserProfile',[payload]),
  updateUserProfile:(id,payload)=>write('updateUserProfile',[id,payload]),
  getMigrationStatus:()=>read('getMigrationStatus',[]),
  getArchitectureStatus:async()=>({ok:true,capabilities:window.ValoraRuntime?.getCapabilities?.(),warnings:window.ValoraRuntime?.getArchitectureWarnings?.()||[],hybridDiagnostics:window.ValoraHybridDiagnostics||[]}),
  getHybridDiagnostics:()=>window.ValoraHybridDiagnostics||[]
});
})();
