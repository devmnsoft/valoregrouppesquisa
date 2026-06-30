const fs=require('fs');const app=fs.readFileSync('app.js','utf8'),fn=fs.readFileSync('functions/index.js','utf8'),fb=fs.readFileSync('firebase-repository.js','utf8');const fail=[];
const auto=app.slice(app.indexOf('async function submitPublicSurveyAuto'),app.indexOf('async function submitPublicSurveyResponse'));
if(/provider_unavailable/.test(auto))fail.push('submit auto não deve transformar erro da Function em provider_unavailable');
for(const c of ['participant_required','lgpd_required','required_question_missing'])if(!fn.includes(`code:'${c}'`))fail.push(`Function sem details ${c}`);
for(const m of ['Informe nome e e-mail','Aceite o termo de consentimento','Responda todas as perguntas obrigatórias'])if(!fb.includes(m)&&!app.includes(m))fail.push(`mensagem específica ausente: ${m}`);
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-provider-unavailable-final-fix: PASS');
