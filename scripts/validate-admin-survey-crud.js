const {read,fail,between}=require('./validate-admin-crud-common');const app=read('app.js'), fn=read('functions/index.js');
for(const e of ['adminCreateSurvey','adminUpdateSurvey','adminDeleteSurvey','adminArchiveSurvey','adminCloseSurvey']) if(!fn.includes(`exports.${e}`)) fail(`Function ausente ${e}`);
if(between(app,'async function saveSurvey','function openQuickSurvey').includes('saveChanges('))fail('saveSurvey chama saveChanges geral');
if(between(app,'function deleteSurvey','async function markSurveyAsHomeFeatured').includes('saveChanges(')||between(app,'function deleteSurvey','async function markSurveyAsHomeFeatured').includes('persist('))fail('deleteSurvey usa persist/saveChanges geral');
if(/adminDeleteSurvey[\s\S]*collection\('responses'\)\.doc\([^)]*\)\.delete/.test(fn))fail('adminDeleteSurvey deleta responses');
process.exit(process.exitCode||0);
