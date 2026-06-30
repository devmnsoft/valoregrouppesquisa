const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const fail=[];const save=app.slice(app.indexOf('function saveBuilder'),app.indexOf('function addQuestion'));const del=app.slice(app.indexOf('function deleteForm'),app.indexOf('function openSurveyEditor'));
if(!/function getFormUsage/.test(app)||!/function detectBreakingFormChanges/.test(app))fail.push('helpers de uso/breaking changes ausentes');
if(/Este formulário está vinculado/.test(save))fail.push('saveBuilder contém mensagem de exclusão');
if(!/Criar nova versão do formulário/.test(save)||!/createFormVersionFromDraft/.test(save))fail.push('alteração estrutural não oferece nova versão');
if(!/Este formulário está vinculado a pesquisas ativas/.test(del)||!/usage\.canDelete/.test(del))fail.push('deleteForm não bloqueia vínculo ativo/respostas');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-form-linked-edit-vs-delete: PASS');
