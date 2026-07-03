const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
const start=s.indexOf('async function certificatePdf');
const end=s.indexOf('function reportRows',start);
const f=s.slice(start,end);
for(const x of ['try{','catch(err)','ValoraRepository.loadPublicResult','safeBuildCertificateData','createCertificate','lastCertificatePdfError']){
  if(!f.includes(x))throw new Error('certificatePdf inseguro: '+x);
}
if(/\bbuildCertificateData\s*\(/.test(f)&&!/safeBuildCertificateData\s*\(/.test(f))throw new Error('certificatePdf chama buildCertificateData direto');
console.log('legacy certificate pdf safe: PASS');
