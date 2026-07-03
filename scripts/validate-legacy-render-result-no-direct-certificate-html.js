const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
const start=s.indexOf('async function renderResult');
const end=s.indexOf('function isDemoCompany',start);
if(start<0||end<0)throw new Error('renderResult não localizado');
const body=s.slice(start,end);
if(body.includes('${certificateHtml(')||/[^A-Za-z0-9_]certificateHtml\s*\(/.test(body))throw new Error('renderResult chama certificateHtml diretamente');
if(!body.includes('safeCertificateHtml'))throw new Error('renderResult não usa safeCertificateHtml');
console.log('legacy render result no direct certificate: PASS');
