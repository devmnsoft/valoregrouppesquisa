#!/usr/bin/env node
const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
const start=s.indexOf('async function renderResult');const end=s.indexOf('function isDemoCompany',start);if(start<0||end<0)fail('renderResult não localizado');const r=s.slice(start,end);
if(!/function\s+renderBasicPublicResult\s*\(/.test(s))fail('renderBasicPublicResult ausente');
for(const x of ['try {','catch (err)','renderResultLoadFallback','typeof renderPremiumPublicResult','renderBasicPublicResult','lastResultRenderError']) if(!r.includes(x)) fail('fallback incompleto: '+x);
console.log('legacy render result fallback: PASS');
