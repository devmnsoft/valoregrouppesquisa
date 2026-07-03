const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){throw new Error(m)}
const start=s.indexOf('function renderPublicResultLoading'); const end=s.indexOf('async function tryEnhancePublicResult', start); const f=start>=0?s.slice(start,end):'';
for(const x of ['Carregando resultado seguro','setTimeout','readLastPublicResultFromSession','renderResultLoadFallback','renderImmediateResultAfterSubmit']) if(!f.includes(x)) fail('loading sem timeout/fallback: '+x);
console.log('legacy result loading timeout: PASS');
