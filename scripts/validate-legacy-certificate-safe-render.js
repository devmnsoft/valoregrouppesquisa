const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){throw new Error(m)}
const safe=(s.match(/function safeCertificateHtml[\s\S]*?\nfunction assertCertificateCanExport/)||[''])[0];
if(!safe)fail('safeCertificateHtml ausente');
for(const x of ['try','certificateHtml','catch','lastCertificateRenderError','Certificado em preparação']) if(!safe.includes(x)) fail('safeCertificateHtml incompleto: '+x);
console.log('legacy certificate safe render: PASS');
