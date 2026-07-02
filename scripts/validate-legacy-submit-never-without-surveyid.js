const fs=require('fs');const a=fs.readFileSync('app.js','utf8');function ok(c,m){if(!c){console.error(m);process.exit(1)}}
const g=a.slice(a.indexOf('async function guardedPublicSurveySubmit'),a.indexOf('async function submitSurvey(form, event = null)'));
const vi=g.indexOf('validatePublicSubmitPayload'), ci=g.lastIndexOf('submitPublicSurveyResponse(payload)');ok(vi>-1&&ci>-1&&vi<ci,'validação não ocorre antes da Function');
ok(/if \(!payload\.surveyId \|\| !payload\.token\)/.test(g),'guard missing context ausente');
ok(/assertPublicSubmitPayloadReady\(payload\);return submitPublicSurveyAuto/.test(a),'submitPublicSurveyResponse sem assert');
console.log('validate-legacy-submit-never-without-surveyid: PASS');
