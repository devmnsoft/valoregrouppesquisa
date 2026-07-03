const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
const start=s.indexOf('async function viewResponse');const end=s.indexOf('async function renderResult',start);
if(start<0||end<0)throw new Error('viewResponse não localizado');
const body=s.slice(start,end);
if(!/try\s*\{/.test(body)||!/catch\s*\(/.test(body)||!body.includes('safeRenderResultById'))throw new Error('viewResponse não está blindado');
console.log('legacy view response safe: PASS');
