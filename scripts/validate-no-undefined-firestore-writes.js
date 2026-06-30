const {read,fail,between}=require('./validate-admin-crud-common');
const fr=read('firebase-repository.js'), fn=read('functions/index.js'), app=read('app.js');
if(!fr.includes('function deepCleanForFirestore'))fail('firebase-repository.js sem deepCleanForFirestore');
for(const needle of ['async function createDoc','async function updateDoc','async function syncCollectionFromState','async saveChanges']){const i=fr.indexOf(needle);const block=i>=0?fr.slice(i,i+900):'';if(!/deepCleanForFirestore|sanitizeStateBeforeSave/.test(block))fail(`${needle} não sanitiza payload`);}
if(/global\s*:\s*undefined/.test(fr+fn+app+read('scripts/build-production.js')))fail('global: undefined encontrado');
if(/function audit[\s\S]{0,500}saveChanges\(/.test(app))fail('audit chama saveChanges');
process.exit(process.exitCode||0);
