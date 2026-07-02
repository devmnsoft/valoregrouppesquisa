const fs=require('fs');const a=fs.readFileSync('app.js','utf8');function ok(c,m){if(!c){console.error(m);process.exit(1)}}
ok(/sid=stateNow\.context\.surveyId; token=stateNow\.context\.token; orgSlug=stateNow\.context\.org/.test(a),'renderTakeSurvey não usa contexto validado');
['name="surveyId" value="${esc(sid)}"','name="token" value="${esc(token)}"','name="org" value="${esc(orgSlug||\'\')}"'].forEach(x=>ok(a.includes(x),'hidden ausente: '+x));
ok(/const surveyId = ctx\.surveyId \|\| formEl\.querySelector\('\[name="surveyId"\]'\)\?\.value \|\| formEl\.dataset\.surveyId \|\| route\.surveyId \|\| ''/.test(a),'ordem surveyId incorreta');
console.log('validate-legacy-submit-hidden-context-fields: PASS');
