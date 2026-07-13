const {app,has,no}=require('./legacy-premium-validator-utils');
has(/const VALORA_PLAN_CATALOG\s*=\s*\{/,'VALORA_PLAN_CATALOG existe');
['free','essential','growth','professional','corporate','enterprise'].forEach(p=>has(new RegExp(`${p}:\\s*\\{`),`plano ${p} existe`));
no(/Certificado simples|simpleCertificate/,'Certificado simples removido dos planos');
