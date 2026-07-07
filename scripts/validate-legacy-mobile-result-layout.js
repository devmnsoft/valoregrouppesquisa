#!/usr/bin/env node
const fs=require('fs');const css=fs.readFileSync('style.css','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
for(const x of ['box-sizing: border-box','overflow-x: hidden','.public-result-container','width: min(100%, 1040px)','max-width: 100%','overflow-wrap: anywhere']) if(!css.includes(x)) fail('layout mobile seguro ausente: '+x);
if(!/@media \(max-width:\s*760px\)[\s\S]*\.result-hero-grid[\s\S]*grid-template-columns:\s*1fr/.test(css))fail('media query mobile para result-hero-grid ausente');
if(!/@media \(max-width:\s*760px\)[\s\S]*\.public-result-actions[\s\S]*display:\s*grid/.test(css))fail('botões mobile não empilhados');
console.log('legacy mobile result layout: PASS');
