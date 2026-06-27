const {assert,exists,readIf,pass}=require('./production-gate-utils');
['PRODUCTION_ROLLBACK_CHECKLIST.md','ROLLBACK_PRODUCAO.md','firebase.json','firestore.rules'].forEach(p=>assert(exists(p),`${p} missing`));
const doc=readIf('PRODUCTION_ROLLBACK_CHECKLIST.md')+readIf('ROLLBACK_PRODUCAO.md'); ['backup','restore','Firebase','DATA_PROVIDER=firebase','rollback test'].forEach(x=>assert(new RegExp(x,'i').test(doc),`rollback item missing: ${x}`));
pass('validate-rollback-readiness');
