const fs=require('fs');const s=fs.readFileSync('app.js','utf8');const body=s.slice(s.indexOf('function createActions'),s.indexOf('async function confirmDialog'));
for(const x of ['reportResponsePdf:el=>reportResponsePdf(el)','downloadResultReport:el=>downloadResultReport(el)','reportPdf:el=>reportPdf(el)','downloadReport:el=>downloadReport(el)','downloadCertificatePdf(el){return certificatePdf(el);}','legacy_run:el=>legacyRun(el)']) if(!body.includes(x)) throw new Error('createActions sem handler: '+x);
const safe=s.slice(s.indexOf('function safeRun'),s.indexOf('async function safeRunAsync')); if(!safe.includes('typeof result.then')||!safe.includes('.catch')) throw new Error('safeRun não captura Promise');
console.log('legacy action handlers no undefined: PASS');
