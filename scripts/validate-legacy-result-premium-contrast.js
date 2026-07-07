#!/usr/bin/env node
const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
const css=fs.readFileSync('style.css','utf8');
function fail(m){console.error('FAIL:',m);process.exit(1)}
if(!app.includes('Valora Insight™'))fail('public product name missing');
if(!css.includes('.result-hero')||!css.includes('#073F4D')||!css.includes('#F7FCFD'))fail('premium result contrast CSS missing');
if(/Enquadramento geral sem adoçamento/i.test(app))fail('duplicate low contrast summary text still present');
console.log('OK legacy premium result contrast');
