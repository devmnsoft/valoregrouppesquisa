#!/usr/bin/env node
const {readApp,validateApp}=require('./action-handler-utils');
const app=readApp();
const {failures}=validateApp(app);
if(!/let\s+actions\s*=\s*\{\s*\}/.test(app))failures.push('actions deve ser let actions={} antes do bootstrap.');
if(!/let\s+formHandlers\s*=\s*\{\s*\}/.test(app))failures.push('formHandlers deve ser let formHandlers={} antes do bootstrap.');
if(/document\.addEventListener\('submit'\s*,\s*(?!handleDocumentSubmit)/.test(app))failures.push('Listener global de submit inline encontrado fora de registerGlobalHandlers.');
if(failures.length){console.error('Validação de action handlers falhou:');failures.forEach(f=>console.error(`* ${f}`));process.exit(1);}console.log('Validação de action handlers concluída sem inconsistências.');
