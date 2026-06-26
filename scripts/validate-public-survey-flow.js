const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const rt=fs.readFileSync('runtime-capabilities.js','utf8');const fail=[];
function ok(r,m){if(!r)fail.push(m)}
ok(/function resolveHomeFeaturedSurvey/.test(app)&&/Responder diagnóstico grátis/.test(app),'Home sem pesquisa destaque/CTA.');
ok(/function renderTakeSurvey/.test(app)&&/searchParams\.get\('survey'\)/.test(app),'Link público não abre renderTakeSurvey.');
const submit=(app.match(/async function submitSurvey\(form\)[\s\S]*?\n}\nfunction calculateSurveyResult/)||[''])[0];
ok(/submitPublicSurveyResponse/.test(submit),'submitSurvey não usa roteador público.');
ok(!/callPublicFunction\('submitSurveyResponse'/.test(submit),'submitSurvey chama Cloud Functions direto.');
ok(/publicJourney/.test(rt)&&/submissionProvider/.test(rt),'runtime sem publicJourney.');
ok(/callGatewayJson/.test(app)&&/submitSurveyResponseLocally/.test(app),'providers de submissão ausentes.');
ok(/resultToken/.test(app)&&/renderResult/.test(submit),'resultado/resultToken ausente no fluxo.');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-public-survey-flow: PASS');
