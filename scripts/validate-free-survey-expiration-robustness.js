#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
const app=read('app.js'), fn=read('functions/index.js');
for (const src of [app,fn]) { assert(src.includes('function isFreeOfficialSurvey'),'isFreeOfficialSurvey ausente'); assert(src.includes('pesquisa gratuita'),'regex sem pesquisa gratuita'); assert(src.includes('seconds')&&(src.includes('toDate')||src.includes('toMillis')),'Timestamp Firestore não tratado'); }
assert(app.includes('isSurveyExpired(survey)&&!isFreeOfficialSurvey(survey)'),'link público grátis ainda bloqueia por expiração');
assert(fn.includes('options.strict===true'),'Functions não flexibiliza expiração grátis');
if(process.exitCode)process.exit(1);
console.log('OK validate-free-survey-expiration-robustness.js');
