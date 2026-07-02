const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function ok(c,m){if(!c){console.error(m);process.exit(1)}}
ok(/async function guardedPublicSurveySubmit\(formEl, event = null\)/.test(app),'guardedPublicSurveySubmit ausente');
ok(/async function submitSurvey\(form, event = null\)[\s\S]{0,240}guardedPublicSurveySubmit\(form, event\)/.test(app),'submitSurvey não delega ao guard oficial');
ok(/takeSurvey:\(form,e\)=>submitSurvey\(form,e\)/.test(app),'takeSurvey não encaminha event ao guard');
const direct=[...app.matchAll(/callFirebaseFunction\('submitSurveyResponse'/g)].map(m=>m.index);const cf=app.indexOf('async function submitPublicSurveyViaCloudFunction');ok(direct.length===1&&direct[0]>cf&&direct[0]<app.indexOf('async function hashPublicValue',cf),'callFirebaseFunction submitSurveyResponse fora do provider autorizado');
console.log('validate-legacy-single-public-submit-path: PASS');
