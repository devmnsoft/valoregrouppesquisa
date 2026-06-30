#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
const app=read('app.js');
assert(app.includes('/communications/result/${encodeURIComponent(responseId)}/send-email'),'endpoint oficial ausente');
assert(app.includes('response?.resultToken'),'resultToken real ausente');
assert(!/resp_demo/.test(app),'resp_demo encontrado no app.js');
assert(app.includes('recordCommunication'),'registro comunicação ausente');
assert(app.includes('Seu resultado foi gerado, mas não conseguimos enviar'),'mensagem amigável ausente');
if(process.exitCode)process.exit(1);
console.log('OK validate-legacy-email-real-response-flow.js');
