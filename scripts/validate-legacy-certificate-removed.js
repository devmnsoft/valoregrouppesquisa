const {read,ok,no}=require('./legacy-final-validator-lib');
no('app.js',/Baixar certificado|Baixar\/Imprimir certificado|Certificado simples/,'certificado visível ainda existe');
ok(/CERTIFICATE_FEATURE_ENABLED=false/.test(read('app.js')),'flag de certificado ausente');
ok(/async function certificatePdf[\s\S]*certificado foi removido/.test(read('app.js')),'fallback certificatePdf ausente');
console.log('legacy certificate removed: PASS');
