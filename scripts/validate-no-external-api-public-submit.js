const fs=require('fs');const app=fs.readFileSync('app.js','utf8'),repo=fs.readFileSync('repository.js','utf8'),cfg=fs.readFileSync('config.js','utf8');const fail=[];
const auto=app.slice(app.indexOf('async function submitPublicSurveyAuto'),app.indexOf('async function submitPublicSurveyResponse'));
if(/submitPublicSurveyViaExternalApi|external-api|ValoraApiRepository/.test(auto))fail.push('submit público auto ainda tenta API externa');
if(/PUBLIC_SUBMISSION_FALLBACKS:[^\n]*external-api/.test(cfg))fail.push('config fallback público contém external-api');
if(!/firebaseOnlyPublicOps\(\)&&\['submitPublicSurveyResponse','sendResultEmail'\]/.test(repo))fail.push('repository não bloqueia API externa para submit/email Firebase');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-no-external-api-public-submit: PASS');
