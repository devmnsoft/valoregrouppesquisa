const {read,fail,between}=require('./validate-admin-crud-common');const app=read('app.js'), fn=read('functions/index.js');
for(const e of ['adminCreateForm','adminUpdateForm','adminDeleteForm','adminCloneForm','adminCreateFormVersion']) if(!fn.includes(`exports.${e}`)) fail(`Function ausente ${e}`);
if(between(app,'async function saveBuilder','function addQuestion').includes('saveChanges(')||between(app,'async function saveBuilder','function addQuestion').includes('deleteForm('))fail('saveBuilder usa saveChanges/deleteForm indevido');
if(/adminDeleteForm[\s\S]*collection\('surveys'\)\.doc\([^)]*\)\.delete/.test(fn))fail('adminDeleteForm deleta surveys');
if(!fn.includes('form_breaking_change_requires_version'))fail('sem erro de versionamento estrutural');
process.exit(process.exitCode||0);
