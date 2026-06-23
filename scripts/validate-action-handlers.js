#!/usr/bin/env node
const fs=require('fs');
const path=require('path');
const root=path.resolve(__dirname,'..');
const app=fs.readFileSync(path.join(root,'app.js'),'utf8');
const failures=[];
function fail(m){failures.push(m);}
function indexOfRequired(label,re){const m=app.match(re); if(!m){fail(`${label} não encontrado.`); return -1;} return m.index;}
const createActionsIdx=indexOfRequired('createActions',/function\s+createActions\s*\(\)\s*\{\s*return\s*\{/);
const createFormsIdx=indexOfRequired('createFormHandlers',/function\s+createFormHandlers\s*\(\)\s*\{\s*return\s*\{/);
const registerIdx=indexOfRequired('registerGlobalHandlers',/function\s+registerGlobalHandlers\s*\(/);
const bootstrapIdx=indexOfRequired('bootstrapApp',/function\s+bootstrapApp\s*\(\)/);
if(registerIdx!==-1&&createActionsIdx!==-1&&registerIdx>createActionsIdx){} // factories may be below because bootstrap calls them after parse
if(!/let\s+actions\s*=\s*\{\s*\}/.test(app))fail('actions deve ser let actions={} antes do bootstrap.');
if(!/let\s+formHandlers\s*=\s*\{\s*\}/.test(app))fail('formHandlers deve ser let formHandlers={} antes do bootstrap.');
if(!/actions\s*=\s*createActions\s*\(\s*\)/.test(app))fail('bootstrapApp deve inicializar actions via createActions().');
if(!/formHandlers\s*=\s*createFormHandlers\s*\(\s*\)/.test(app))fail('bootstrapApp deve inicializar formHandlers via createFormHandlers().');
if(!/registerGlobalHandlers\s*\(\s*\)\s*;\s*\n\s*run\('a inicialização'/.test(app))fail('bootstrapApp deve registrar handlers antes de init().');
if(/document\.addEventListener\('click'\s*,\s*(?!handleDocumentClick)/.test(app))fail('Listener global de click inline encontrado fora de registerGlobalHandlers.');
if(/document\.addEventListener\('submit'\s*,\s*(?!handleDocumentSubmit)/.test(app))fail('Listener global de submit inline encontrado fora de registerGlobalHandlers.');
if(/openOnboardingWizard/.test(app)&&!/function\s+openOnboardingWizard\s*\(/.test(app))fail('openOnboardingWizard é referenciado mas não foi declarado.');
const actionBody=(app.match(/function\s+createActions\s*\(\)\s*\{\s*return\s*\{([\s\S]*?)\n\};\}/)||[])[1]||'';
const formBody=(app.match(/function\s+createFormHandlers\s*\(\)\s*\{\s*return\s*\{([\s\S]*?)\n\};\}/)||[])[1]||'';

function declared(name){return new RegExp(`function\\s+${name}\\s*\\(`).test(app)||new RegExp(`const\\s+${name}\\s*=`).test(app)||new RegExp(`let\\s+${name}\\s*=`).test(app);}
function referencedNames(body){
  const names=new Set();
  for(const m of body.matchAll(/(?:^|[,\n])\s*([A-Za-z_$][\w$]*)\s*(?=,|\n|}|$)/g))names.add(m[1]);
  for(const m of body.matchAll(/:\s*([A-Za-z_$][\w$]*)\s*(?=,|\n|})/g))names.add(m[1]);
  return [...names].filter(n=>!['true','false','null','undefined'].includes(n));
}

function hasKey(body,key){const e=key.replace(/[.*+?^${}()|[\]\\]/g,'\\$&');return new RegExp(`(^|[,\\n\\s])${e}\\s*(?:[:,(,}]|$)`).test(body);}
const sourceFiles=['index.html','app.js'].filter(f=>fs.existsSync(path.join(root,f)));
const sources=sourceFiles.map(f=>fs.readFileSync(path.join(root,f),'utf8')).join('\n');
for(const m of sources.matchAll(/data-action=["']([^"']+)["']/g)){const name=m[1]; if(!name.includes('${')&&!hasKey(actionBody,name))fail(`data-action não registrado em actions: ${name}`);}
for(const m of sources.matchAll(/data-form=["']([^"']+)["']/g)){const name=m[1]; if(!name.includes('${')&&!hasKey(formBody,name))fail(`data-form não registrado em formHandlers: ${name}`);}
for(const n of referencedNames(formBody))if(!declared(n)&&!new RegExp(`${n}\\s*\\(`).test(formBody))fail(`createFormHandlers referencia função inexistente: ${n}`);
if(/saveOnboardingWizard/.test(app)&&!declared('saveOnboardingWizard'))fail('saveOnboardingWizard é referenciado mas não foi declarado.');
if(/openOnboardingWizard/.test(app)&&!declared('openOnboardingWizard'))fail('openOnboardingWizard é referenciado mas não foi declarado.');
if(failures.length){console.error('Validação de action/form handlers falhou:'); failures.forEach(f=>console.error(`- ${f}`)); process.exit(1);} 
console.log('Validação de action/form handlers concluída sem inconsistências.');
