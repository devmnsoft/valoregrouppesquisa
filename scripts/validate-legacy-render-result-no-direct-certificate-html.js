const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){throw new Error(m)}
const r=s.slice(s.indexOf('async function renderResult'),s.indexOf('function getCertificateScore'));
if(!r.includes('safeCertificateHtml('))fail('renderResult não usa safeCertificateHtml');
if(/[^A-Za-z0-9_]certificateHtml\s*\(/.test(r))fail('renderResult chama certificateHtml diretamente');
console.log('legacy render result no direct certificate html: PASS');
