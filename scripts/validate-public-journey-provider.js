#!/usr/bin/env node
const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
const prod=fs.readFileSync('config/config.production.js','utf8');
const finalCfg=fs.existsSync('config.js')?fs.readFileSync('config.js','utf8'):'';
const rt=fs.readFileSync('runtime-capabilities.js','utf8');
const fail=[];const ok=(r,m)=>{if(!r)fail.push(m)};
const fn=(name)=>{const m=app.match(new RegExp(`async function ${name}\\([^)]*\\)\\{[\\s\\S]*?\\n\\}`));return m?m[0]:''};
ok(/PUBLIC_SUBMISSION_PROVIDER\s*:\s*['"](?:auto|external-api)['"]/.test(prod),'PUBLIC_SUBMISSION_PROVIDER ausente em produção.');
ok(/PUBLIC_SURVEY_VALIDATION_PROVIDER\s*:\s*['"](?:auto|external-api)['"]/.test(prod),'PUBLIC_SURVEY_VALIDATION_PROVIDER ausente em produção.');
ok(/RESULT_PROVIDER\s*:\s*['"](?:auto|external-api)['"]/.test(prod),'RESULT_PROVIDER ausente em produção.');
ok(/EXTERNAL_API_BASE_URL\s*:\s*['"]https?:\/\//.test(prod),'external-api sem EXTERNAL_API_BASE_URL em produção.');
ok(/PUBLIC_SUBMISSION_PROVIDER/.test(finalCfg)&&/PUBLIC_SURVEY_VALIDATION_PROVIDER/.test(finalCfg)&&/RESULT_PROVIDER/.test(finalCfg),'config.js final sem providers públicos.');
ok(/publicJourney/.test(rt)&&/usesGateway/.test(rt)&&/usesCloudFunctions/.test(rt),'runtime-capabilities sem publicJourney completo.');
ok(!/callPublicFunction\('submitSurveyResponse'/.test(fn('submitSurvey')),'submitSurvey usa callPublicFunction direto.');
ok(!/callPublicFunction\('validateSurveyLink'/.test(fn('renderTakeSurvey')),'renderTakeSurvey usa callPublicFunction direto.');
ok((/FIREBASE_PLAN\s*:\s*['"]blaze['"]/.test(prod)&&/ENABLE_CLOUD_FUNCTIONS\s*:\s*true/.test(prod))||(/ENABLE_CLOUD_FUNCTIONS\s*:\s*false/.test(prod)&&!/PUBLIC_SUBMISSION_PROVIDER\s*:\s*['"]cloud-functions/.test(prod)),'Produção deve estar coerente: Blaze com Functions ou Spark sem Functions públicas.');
ok(/validatePublicSurveyLink/.test(app)&&/submitPublicSurveyResponse/.test(app)&&/loadPublicResult/.test(app),'Funções públicas via provider ausentes.');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}
console.log('validate-public-journey-provider: PASS');
