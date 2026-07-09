const {read,ok,no}=require('./legacy-final-validator-lib');const t=read('app.js')+'\n'+read('pdf.js');
ok(/radarBarPdfSafe/.test(t),'radarBarPdfSafe ausente');
if(/\?{4,}|\? Estruturada|\? Em estruturação|\? Crítico|\? Alta maturidade/.test(t)) throw new Error('artefato de ? encontrado');
console.log('legacy pdf no question artifacts: PASS');
