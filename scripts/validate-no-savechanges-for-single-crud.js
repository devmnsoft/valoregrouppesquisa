const {read,fail,between}=require('./validate-admin-crud-common');const app=read('app.js');
[['saveSurvey','function openQuickSurvey'],['deleteSurvey','async function markSurveyAsHomeFeatured'],['saveBuilder','function addQuestion'],['deleteForm','function openSurveyEditor']].forEach(([name,end])=>{const block=between(app, name.includes('saveBuilder')?'async function saveBuilder':`function ${name}`, end); if(/saveChanges\(|persist\(/.test(block))fail(`${name} usa saveChanges/persist geral`);});
process.exit(process.exitCode||0);
