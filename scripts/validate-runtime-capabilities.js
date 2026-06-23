#!/usr/bin/env node
const fs=require('fs');const vm=require('vm');
function loadConfig(file){const ctx={window:{}};vm.createContext(ctx);vm.runInContext(fs.readFileSync(file,'utf8'),ctx,{filename:file});vm.runInContext(fs.readFileSync('runtime-capabilities.js','utf8'),ctx,{filename:'runtime-capabilities.js'});return ctx.window.ValoraRuntime.getCapabilities();}
const cap=loadConfig('config.js');const failures=[];
if(cap.environment!=='production')failures.push('RUNTIME_ENV de config.js deve ser production.');
if(cap.storageMode!=='firebase')failures.push('STORAGE_MODE PRD deve ser firebase.');
if(cap.firebasePlan!=='spark')failures.push('FIREBASE_PLAN PRD deve ser spark.');
if(cap.cloudFunctions.enabled)failures.push('Cloud Functions devem estar desabilitadas no Spark.');
if(cap.localApi.enabled)failures.push('API local deve estar desabilitada na PRD.');
if(cap.email.transport!=='disabled'||cap.email.canSend||cap.email.canReadStatus||cap.email.hasOutbox||cap.email.canConfigureTransport)failures.push('E-mail PRD Spark deve estar disabled e sem chamadas de rede.');
if(cap.logs.canPersistRemote)failures.push('Logs remotos devem estar desabilitados no Spark.');
if(failures.length){console.error(failures.join('\n'));process.exit(1);}console.log('Capacidades PRD Spark validadas.');
