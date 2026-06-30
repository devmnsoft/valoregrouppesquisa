#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
const fb=read('firebase-repository.js'), fn=read('functions/index.js');
assert(fb.includes("callFunction('createCompanyUser'"),'legado não chama createCompanyUser');
['createUser','setCustomUserClaims','users'].forEach(x=>assert(fn.includes(x),'functions sem '+x));
['role','companyId','status','resetLink'].forEach(x=>assert(fn.includes(x),'contrato usuário sem '+x));
if(process.exitCode)process.exit(1);
console.log('OK validate-user-create-runtime-contract.js');
