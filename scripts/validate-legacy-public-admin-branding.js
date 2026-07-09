const {read,ok}=require('./legacy-final-validator-lib');const a=read('app.js'),p=read('pdf.js');
ok(a.includes("PUBLIC_PRODUCT_NAME='Valora Insight™'")&&a.includes("PLATFORM_NAME='Valora Pulse™'"),'marcas base ausentes');
ok(p.includes("pdfProductName(){ return 'Valora Insight'; }")||a.includes("function pdfProductName(){return 'Valora Insight';}"),'nome PDF safe ausente');
console.log('legacy public/admin branding: PASS');
