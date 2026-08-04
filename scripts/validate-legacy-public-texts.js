#!/usr/bin/env node
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
if(/Valora Pulse/i.test(app))fail('legacy brand found in active application');
if(!/const PUBLIC_PRODUCT_NAME=BRAND\.productName/.test(app))fail('public product name not sourced from BRAND');
for(const bad of ['Pesquisa gratuita da Home: diagnóstico público','Fale com A valora']) if(app.includes(bad))fail('public bad text: '+bad);
if(!app.includes('Início'))fail('Início label absent');
console.log('legacy public texts: PASS');
