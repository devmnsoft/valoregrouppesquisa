#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
const app=read('app.js'), css=read('style.css');
['getAdminMenuItems','renderAdminSidebar','renderAdminMenuItem','bindAdminMobileMenuEvents','toggleAdminMobileMenu'].forEach(x=>assert(app.includes('function '+x)||app.includes(x+'('),x+' ausente'));
['Clientes','Financeiro','Planos','Pesquisas','Formulários','Usuários','Respostas'].forEach(x=>assert(app.includes(x),'item '+x+' ausente'));
assert(app.includes('admin-nav'),'admin-nav ausente');
assert(css.includes('overflow-y:auto!important')||css.includes('overflow-y: auto'),'sidebar sem overflow-y auto');
if(process.exitCode)process.exit(1);
console.log('OK validate-admin-mobile-menu-full-list.js');
