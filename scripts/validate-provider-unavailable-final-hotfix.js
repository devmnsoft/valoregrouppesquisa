#!/usr/bin/env node
const fs=require('fs');
function fail(m){console.error(`❌ ${m}`);process.exitCode=1;}
function ok(m){console.log(`✅ ${m}`);}
const app=fs.readFileSync('app.js','utf8');
const cfg=fs.readFileSync('config.js','utf8');
const init=fs.readFileSync('firebase-init.js','utf8');
[
  [cfg,"APP_VERSION: '8.7.7'",'config versão 8.7.7'],
  [cfg,"PUBLIC_SUBMISSION_FALLBACKS: ['cloud-functions', 'firestore', 'external-api']",'fallbacks submit na ordem CF > Firestore > API'],
  [cfg,"RESULT_FALLBACKS: ['cloud-functions', 'firestore', 'external-api']",'fallbacks de resultado incluem Firestore'],
  [cfg,"EMAIL_FALLBACKS: ['cloud-functions', 'external-api', 'email-job']",'fallbacks de email incluem email-job'],
  [app,'async function submitPublicSurveyAuto(payload)','submit auto existe'],
  [app,"['cloud-functions','firestore','external-api']",'pesquisa gratuita tenta CF > Firestore > API'],
  [app,'async function submitPublicSurveyViaFirestoreFallback(payload)','fallback Firestore existe'],
  [app,"source:'legacy_firestore_fallback'",'fallback registra source'],
  [app,'async function callFirebaseFunction(name,data={})','callFirebaseFunction compat existe'],
  [app,"error.code='provider_unavailable'",'provider_unavailable só após tentativas'],
  [init,'window.ValoraGetFirestoreSafe=function()','helper Firestore exposto'],
  [init,'window.ValoraGetFunctionsSafe=getFirebaseFunctionsSafe','helper Functions exposto']
].forEach(([txt,needle,msg])=>txt.includes(needle)?ok(msg):fail(`${msg} ausente`));
if(process.exitCode)process.exit(process.exitCode);
