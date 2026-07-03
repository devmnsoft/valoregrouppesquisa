const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
const start=s.indexOf('function renderPublicResultLoading');const end=s.indexOf('async function safeRenderResultById',start);
if(start<0||end<0)throw new Error('renderPublicResultLoading não localizado');
const body=s.slice(start,end);
if(!body.includes('setTimeout')||!body.includes('6000')||!body.includes('renderResultLoadFallback'))throw new Error('loading seguro sem timeout/fallback');
console.log('legacy result loading timeout: PASS');
