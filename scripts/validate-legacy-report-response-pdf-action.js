const fs=require('fs');const s=fs.readFileSync('app.js','utf8');
for(const x of ['async function reportResponsePdf','async function downloadResultReport','async function reportPdf','async function downloadReport','ValoraRepository.loadPublicResult','ValoraPdf','createReport']) if(!s.includes(x)) throw new Error('reportResponsePdf incompleto: '+x);
if(/reportResponsePdf\(el\.dataset\.id\)/.test(s)) throw new Error('reportResponsePdf não deve receber apenas id');
console.log('legacy report response pdf action: PASS');
