#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
const side=read('backend/Valora.Web/Views/Shared/_Sidebar.cshtml'), layout=read('backend/Valora.Web/Views/Shared/_Layout.cshtml');
['Dashboard','Usuários','Formulários','Pesquisas','Diagnósticos gratuitos','Comunicações','Respostas','Certificados','Status'].forEach(x=>assert(side.includes(x),'Valora.Web sidebar sem '+x));
assert(layout.includes('bootstrap')||layout.includes('jquery'),'Valora.Web sem Bootstrap/jQuery no layout');
if(process.exitCode)process.exit(1);
console.log('OK validate-aspnet-web-correction-parity.js');
