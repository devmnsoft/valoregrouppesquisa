const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
function between(a,b){const start=s.indexOf(a);const end=s.indexOf(b,start);if(start<0||end<0)throw new Error(a+' não localizado');return s.slice(start,end);}
for(const [name,next] of [['certificatePdf','function downloadCertificate'],['certificatePng','async function exportCertificateImage']]){const body=between('async function '+name,next);if(!/try\s*\{/.test(body)||!/catch\s*\(/.test(body))throw new Error(name+' sem try/catch');}
const pdf=between('async function certificatePdf','function downloadCertificate');
if(/buildCertificateData\s*\(/.test(pdf)&&!/safeBuildCertificateData\s*\(/.test(pdf))throw new Error('certificatePdf chama buildCertificateData sem safe');
console.log('legacy certificate actions safe: PASS');
