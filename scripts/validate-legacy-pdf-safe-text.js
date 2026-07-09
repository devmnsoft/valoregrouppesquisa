const {read,ok,no}=require('./legacy-final-validator-lib');const p=read('pdf.js'),a=read('app.js');
for(const x of ['toPdfSafeText','pdfProductName','pdfLevelTitle','pdfScoreLine']) ok(p.includes(x)||a.includes(x),`ausente ${x}`);
no('pdf.js',/[🔴🟡🟢🔵█░]/,'pdf.js contém unicode inseguro');
console.log('legacy pdf safe text: PASS');
