const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function ok(c,m){if(!c)throw new Error(m)}
ok(/async function saveQuickSurvey/.test(app),'saveQuickSurvey not async');
ok(/preparePublicSurveyLink',\{surveyId:s\.id,featured:false,free:false\}/.test(app),'instant survey does not prepare public link');
ok(!/saveQuickSurvey[\s\S]*tokenHash/.test(app.match(/async function saveQuickSurvey[\s\S]*?function deleteSurvey/)?.[0]||''),'instant flow uses tokenHash');
console.log('validate-instant-survey-link-flow: PASS');
