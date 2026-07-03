const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){throw new Error(m)}
for(const name of ['downloadCertificatePdf','downloadCertificatePng']){const m=s.match(new RegExp('async function '+name+'[\\s\\S]*?\\nfunction '));if(!m)fail(name+' ausente');const b=m[0];for(const x of ['try','catch','buildCertificateData','lastCertificateDownloadError','Certificado em preparação']) if(!b.includes(x)) fail(name+' sem proteção: '+x)}
console.log('legacy certificate download safe: PASS');
