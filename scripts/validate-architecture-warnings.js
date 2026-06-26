#!/usr/bin/env node
const fs=require('fs');
const vm=require('vm');
const source=fs.readFileSync('runtime-capabilities.js','utf8');
const sandbox={window:{ValoraConfig:{RUNTIME_ENV:'production',FIREBASE_PLAN:'spark',DATA_PROVIDER:'api',API_BASE_URL:'',ENABLE_CLOUD_FUNCTIONS:false,PUBLIC_SURVEY_VALIDATION_PROVIDER:'firebase-functions',PUBLIC_SUBMISSION_PROVIDER:'external-api',RESULT_PROVIDER:'api',COMMUNICATION_GATEWAY:{enabled:true,baseUrl:''}}},console};
vm.createContext(sandbox);
vm.runInContext(source,sandbox);
const runtime=sandbox.window.ValoraRuntime;
if(!runtime?.getArchitectureWarnings)throw new Error('ValoraRuntime.getArchitectureWarnings não exposto.');
const warnings=runtime.getArchitectureWarnings();
for(const code of ['SPARK_CLOUD_FUNCTIONS_PUBLIC_JOURNEY','API_PROVIDER_WITHOUT_BASE_URL','PUBLIC_SUBMISSION_EXTERNAL_API_WITHOUT_GATEWAY','CLOUD_FUNCTIONS_DISABLED_PUBLIC_PROVIDER','API_CUTOVER_NOT_ALLOWED','GATEWAY_HEALTH_UNAVAILABLE']){
  if(!warnings.some(w=>w.code===code&&['critical','warning','info'].includes(w.level)))throw new Error(`Warning obrigatório ausente: ${code}`);
}
if(runtime.getCapabilities().publicJourney.configurationValid!==false)throw new Error('Configuração firebase-functions com ENABLE_CLOUD_FUNCTIONS=false deve ser inválida.');
console.log('validate-architecture-warnings: PASS');
