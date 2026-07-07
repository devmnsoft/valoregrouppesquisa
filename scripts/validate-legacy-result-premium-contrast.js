#!/usr/bin/env node
const fs=require('fs');const css=fs.readFileSync('style.css','utf8');const app=fs.readFileSync('app.js','utf8');
function fail(m){console.error('FAIL:',m);process.exit(1)}
for(const x of ['.result-hero-premium','#073F4D','#042F3A','#F7FCFD','.result-hero-description','#D8F3F7','.result-score-panel-premium strong','#FFFFFF']) if(!css.includes(x)) fail('contraste premium ausente: '+x);
if(!/\.result-hero-premium\s+h1[\s\S]*color:\s*#FFFFFF/i.test(css)&&!/\.result-hero-copy\s+h1[\s\S]*color:\s*#FFFFFF/i.test(css))fail('h1 do hero premium não está branco');
if(!app.includes('result-hero-premium'))fail('HTML premium não usa result-hero-premium');
console.log('legacy premium result contrast: PASS');
