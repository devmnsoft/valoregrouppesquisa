#!/usr/bin/env node
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
['handleLoginSubmit','event?.preventDefault','__valoraLoginInProgress','clearPublicRouteParamsBeforePrivateNavigation','Usuário inativo'].forEach(x=>{if(!app.includes(x))fail(`${x} missing`)});
if(!/login:\(form,event\)=>handleLoginSubmit\(form,event\)/.test(app))fail('login action not mapped to robust handler');
console.log('OK legacy login submit stable');
