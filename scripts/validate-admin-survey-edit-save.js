const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const fail=[];const save=app.slice(app.indexOf('async function saveSurvey'),app.indexOf('async function saveQuickSurvey'));
if(!/window\.ValoraRepository\.updateSurvey\(existing\.id,updatePayload\)/.test(save))fail.push('edição não chama updateSurvey(id,payload)');
if(/deleteForm\(/.test(save))fail.push('saveSurvey chama deleteForm');
if(!/responseCount/.test(save)||!/publicToken/.test(save)||!/createdAt/.test(save))fail.push('saveSurvey não preserva campos críticos');
if(!/preparePublicSurveyLink/.test(save))fail.push('saveSurvey não prepara link público quando destaque');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-admin-survey-edit-save: PASS');
