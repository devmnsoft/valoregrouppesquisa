#!/usr/bin/env node
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
if(/Valora Pulse™/.test(app)&&/PUBLIC_PRODUCT_NAME\s*=\s*'Valora Insight™'/.test(app)===false)fail('public product name not standardized');
for(const bad of ['Pesquisa gratuita da Home: diagnóstico público','Fale com A valora']) if(app.includes(bad))fail('public bad text: '+bad);
if(!app.includes('Início'))fail('Início label absent');
console.log('legacy public texts: PASS');
