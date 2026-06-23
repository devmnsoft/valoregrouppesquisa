#!/usr/bin/env node
const fs=require('fs');const files=['app.js','runtime-capabilities.js','config.js','config.local.js','logger-service.js','log-service.js'];const src=files.map(f=>fs.readFileSync(f,'utf8')).join('\n');const fail=[];
if(/fetch\(['"]\/api\/email\//.test(src)||/fetch\(['"]\/api\/outbox/.test(src))fail.push('fetch direto para endpoints locais encontrado.');
if(!/EMAIL_TRANSPORT:'disabled'/.test(fs.readFileSync('config.js','utf8')))fail.push('Produção sem EMAIL_TRANSPORT disabled.');
if(!/LOCAL_API_ENABLED:false/.test(fs.readFileSync('config.js','utf8')))fail.push('Produção com API local habilitada.');
if(!/canPersistRemote/.test(fs.readFileSync('runtime-capabilities.js','utf8')))fail.push('Capacidade de log remoto ausente.');
if(/system\.legacy_run/.test(src))fail.push('Texto system.legacy_run não deve existir.');
if(/\[Valora debug\]|\[Valora info\]/.test(src))fail.push('Prefixos de debug/info proibidos encontrados.');
if(fail.length){console.error(fail.join('\n'));process.exit(1);}console.log('validate-email-runtime: PASS');
