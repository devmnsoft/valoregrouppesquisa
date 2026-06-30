#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
const fb=read('firebase-repository.js');
['organizations','companies','settings','slug','planId','status'].forEach(x=>assert(fb.includes(x),'contrato cliente sem '+x));
if(process.exitCode)process.exit(1);
console.log('OK validate-client-create-runtime-contract.js');
