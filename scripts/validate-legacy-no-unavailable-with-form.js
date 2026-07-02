const fs=require('fs');const a=fs.readFileSync('app.js','utf8');function ok(c,m){if(!c){console.error(m);process.exit(1)}}
const f=a.slice(a.indexOf('function renderPublicSurveyUnavailable'),a.indexOf('function publicApiErrorCode'));
ok(f.includes("setPublicSurveyState({status:'unavailable',context:null"),'state unavailable ausente');ok(f.includes("clearPublicSurveyDomArtifacts('render_unavailable')"),'clear ausente');ok(/if\(app\)app\.innerHTML=html/.test(f),'não substitui #app');ok(f.includes("document.querySelector('[data-public-survey-form]')"),'validação pós-render ausente');
console.log('validate-legacy-no-unavailable-with-form: PASS');
