const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
const start=s.indexOf('function safeRun');const end=s.indexOf('async function safeRunAsync',start);
if(start<0||end<0)throw new Error('safeRun não localizado');
const body=s.slice(start,end);
if(!body.includes('typeof result.then')||!body.includes('.catch'))throw new Error('safeRun não captura Promise rejeitada');
if(!s.includes('function handleActionError'))throw new Error('handleActionError ausente');
console.log('legacy safe run async: PASS');
