#!/usr/bin/env node
const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');const cfg=fs.readFileSync('config.js','utf8');const runtime=fs.readFileSync('runtime-capabilities.js','utf8');const log=fs.readFileSync('log-service.js','utf8')+fs.readFileSync('logger-service.js','utf8');
const fail=[];
if(!/EMAIL_TRANSPORT:'disabled'/.test(cfg))fail.push('config.js PRD deve usar EMAIL_TRANSPORT disabled.');
if(!/LOCAL_API_ENABLED:false/.test(cfg))fail.push('config.js PRD deve desabilitar LOCAL_API_ENABLED.');
if(!/safeFetchJson/.test(app))fail.push('safeFetchJson deve ser usado para APIs locais.');
if(/fetch\('\/api\/email\/status'\)|fetch\("\/api\/email\/status"\)/.test(app))fail.push('fetch direto /api/email/status proibido.');
if(/fetch\('\/api\/email\/config'\)|fetch\("\/api\/email\/config"\)/.test(app))fail.push('fetch direto /api/email/config proibido.');
if(/fetch\('\/api\/email\/send'\)|fetch\("\/api\/email\/send"\)/.test(app))fail.push('fetch direto /api/email/send proibido.');
if(/fetch\('\/api\/outbox'\)|fetch\("\/api\/outbox"\)/.test(app))fail.push('fetch direto /api/outbox proibido.');
if(!/canConfigureTransport/.test(app)||!/Canal de e-mail não habilitado/.test(app))fail.push('Tela de configurações deve respeitar capacidades de transporte.');
if(!/legacyTraceEnabled/.test(app))fail.push('safeRun deve respeitar legacyTraceEnabled.');
if(!/shouldWriteConsole/.test(log))fail.push('Logger deve respeitar consoleEnabled/consoleLevel.');
if(!/canPersistRemote/.test(log))fail.push('Log remoto deve consultar ValoraRuntime.');
if(!/isExternalBrowserNoise/.test(app))fail.push('Ruído de extensão deve ser ignorado.');
if(!/getRuntimeCapabilities/.test(runtime))fail.push('runtime-capabilities.js inválido.');
if(fail.length){console.error(fail.join('\n'));process.exit(1);}console.log('Ambiente de e-mail validado sem chamadas indevidas para APIs ausentes.');
