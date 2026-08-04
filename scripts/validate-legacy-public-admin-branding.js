const {read,ok}=require('./legacy-final-validator-lib');const a=read('app.js'),p=read('pdf.js');
ok(a.includes("const PUBLIC_PRODUCT_NAME=BRAND.productName")&&a.includes("const PLATFORM_NAME=BRAND.productName"),'fonte única de marca ausente');
ok(!/Valora Pulse/.test(a),'marca legada visível encontrada');
ok(p.includes("pdfProductName(){ return 'Valora Insight'; }")||a.includes("function pdfProductName(){return 'Valora Insight';}"),'nome PDF safe ausente');
console.log('legacy public/admin branding: PASS');
