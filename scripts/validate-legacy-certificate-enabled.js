'use strict';
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8');}
function ok(cond,msg){if(!cond)throw new Error(msg);}
const app=read('app.js'), cfg=read('config.js'), prod=read('config/config.production.js');
ok(!/CERTIFICATE_FEATURE_ENABLED\s*=\s*false/.test(app),'CERTIFICATE_FEATURE_ENABLED não pode ficar false hard-coded');
ok(/CERTIFICATE_FEATURE_ENABLED\s*=\s*true/.test(app),'certificado deve estar habilitado no legado');
ok(!/certificado foi removido desta versão/i.test(app),'certificado não pode ser no-op removido');
ok(/adminCertificatePdf[\s\S]*createCertificate/.test(app),'adminCertificatePdf deve gerar PDF');
ok(/downloadCertificatePdf[\s\S]*createCertificate/.test(app),'download público deve gerar PDF');
ok(/certificateFeatureEnabled:\s*true/.test(cfg)&&/certificateFeatureEnabled:\s*true/.test(prod),'configuração de certificado deve estar ativa');
console.log('validate-legacy-certificate-enabled: PASS');
