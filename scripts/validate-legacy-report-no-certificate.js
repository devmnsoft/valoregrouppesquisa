const {read,ok}=require('./legacy-final-validator-lib');const a=read('app.js');const fn=a.slice(a.indexOf('function createValoraInsightReportDocument'),a.indexOf('function generateValoraInsightReportPdf'));
ok(fn&&!/certificado|certificate/i.test(fn),'relatório menciona certificado');
console.log('legacy report no certificate: PASS');
