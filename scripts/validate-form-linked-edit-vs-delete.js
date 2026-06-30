const {read,fail,between}=require('./validate-admin-crud-common');const app=read('app.js'), fn=read('functions/index.js');
const msg='Este formulário está vinculado a pesquisas. Clone ou encerre as pesquisas antes de excluir.';
if(between(app,'async function saveBuilder','function addQuestion').includes(msg))fail('mensagem de exclusão aparece no fluxo de edição');
if(!fn.includes('form_linked_to_active_surveys'))fail('sem bloqueio específico de exclusão vinculada');
process.exit(process.exitCode||0);
