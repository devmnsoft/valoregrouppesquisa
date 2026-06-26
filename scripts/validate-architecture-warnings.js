#!/usr/bin/env node
const fs=require('fs');
const vm=require('vm');
const source=fs.readFileSync('runtime-capabilities.js','utf8');
const sandbox={window:{ValoraConfig:{RUNTIME_ENV:'production',FIREBASE_PLAN:'spark',DATA_PROVIDER:'api',API_BASE_URL:'',ENABLE_CLOUD_FUNCTIONS:false,PUBLIC_SURVEY_VALIDATION_PROVIDER:'firebase-functions',PUBLIC_SUBMISSION_PROVIDER:'api',RESULT_PROVIDER:'api',COMMUNICATION_GATEWAY:{enabled:true,baseUrl:''}}},console};
vm.createContext(sandbox);
vm.runInContext(source,sandbox);
const runtime=sandbox.window.ValoraRuntime;
if(!runtime?.getArchitectureWarnings)throw new Error('ValoraRuntime.getArchitectureWarnings não exposto.');
const warnings=runtime.getArchitectureWarnings();
for(const code of ['spark-cloud-functions-public-journey','api-base-url-missing','gateway-base-url-missing','cloud-functions-disabled-provider-enabled']){
  if(!warnings.some(w=>w.code===code))throw new Error(`Warning obrigatório ausente: ${code}`);
}
if(runtime.getCapabilities().publicJourney.configurationValid!==false)throw new Error('Configuração firebase-functions com ENABLE_CLOUD_FUNCTIONS=false deve ser inválida.');
console.log('validate-architecture-warnings: PASS');
