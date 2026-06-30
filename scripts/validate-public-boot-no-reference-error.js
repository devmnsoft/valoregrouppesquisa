#!/usr/bin/env node
const {readApp,validateApp,functionBody}=require('./action-handler-utils');
const app=readApp();
const failures=[];
try{new Function(app);}catch(e){failures.push(`app.js possui erro de sintaxe: ${e.message}`);}
const boot=functionBody(app,'bootstrapApp');
if(!/createActions\s*\(/.test(boot))failures.push('bootstrapApp não chama createActions().');
if(!/createFormHandlers\s*\(/.test(boot))failures.push('bootstrapApp não chama createFormHandlers().');
if(!/registerGlobalHandlers\s*\(/.test(boot))failures.push('bootstrapApp não chama registerGlobalHandlers().');
failures.push(...validateApp(app).failures);
if(!/function\s+registerGlobalHandlers\s*\(\)\s*\{[\s\S]*handleDocumentClick[\s\S]*handleDocumentSubmit/.test(app))failures.push('registerGlobalHandlers deve registrar handleDocumentClick e handleDocumentSubmit.');
if(failures.length){console.error('Validação de boot público sem ReferenceError falhou:');failures.forEach(f=>console.error(`* ${f}`));process.exit(1);}console.log('Boot público validado sem ReferenceError potencial.');
