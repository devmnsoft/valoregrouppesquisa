#!/usr/bin/env node
const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
function fail(m){console.error('FAIL:',m);process.exit(1)}
const idx=s.indexOf('async function renderResult');
if(idx<0)fail('renderResult não localizado');
const before=s.slice(0,idx);
const render=s.slice(idx,s.indexOf('function isDemoCompany',idx)>idx?s.indexOf('function isDemoCompany',idx):idx+8000);
if(!render.includes('renderPremiumPublicResult'))fail('renderResult não chama renderPremiumPublicResult');
if(!/function\s+renderPremiumPublicResult\s*\(/.test(before))fail('renderPremiumPublicResult não existe antes de renderResult');
if(!/window\.renderPremiumPublicResult\s*=\s*renderPremiumPublicResult/.test(s))fail('renderPremiumPublicResult não exposta em window');
console.log('legacy premium result defined: PASS');
