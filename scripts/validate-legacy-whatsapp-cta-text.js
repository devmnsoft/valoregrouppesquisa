const {read,ok}=require('./legacy-final-validator-lib');const t=read('app.js')+'\n'+read('config.js')+'\n'+read('config/config.production.js');
ok(t.includes('Fale com o Valora Group'),'CTA oficial ausente');
if(/Fale com a Valora Group|Fale com A valora|Falar com a Valora no WhatsApp/.test(t)) throw new Error('CTA antigo encontrado');
console.log('legacy whatsapp cta: PASS');
