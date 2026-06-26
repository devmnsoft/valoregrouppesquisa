#!/usr/bin/env node
const fs=require('fs');
const read=f=>fs.existsSync(f)?fs.readFileSync(f,'utf8'):'';
const fail=[];const ok=(cond,msg)=>{if(!cond)fail.push(msg)};
const cfg=read('config.js')+read('config/config.production.js');
ok(/DATA_PROVIDER:\s*['"]firebase['"]/.test(cfg),'Produção deve permanecer DATA_PROVIDER=firebase.');
ok(/ALLOW_API_PRODUCTION_CUTOVER:\s*false/.test(cfg),'ALLOW_API_PRODUCTION_CUTOVER deve ser false por padrão.');
ok(fs.existsSync('migration/validate-valorapesquisa.js'),'Dry-run de migração ausente.');
ok(fs.existsSync('migration/compare-firebase-postgres.js'),'Comparador Firebase x PostgreSQL ausente.');
ok(/DATA_PROVIDER=firebase/.test(read('ROLLBACK_PLAN_FIREBASE.md')),'Rollback Firebase não documentado.');
ok(/PUBLIC_SUBMISSION_PROVIDER/.test(cfg)&&!/PUBLIC_SUBMISSION_PROVIDER:\s*['"]firebase-functions['"]/.test(cfg),'Provider público inválido para produção Spark.');
ok(!/SMTP_(PASSWORD|PASS|SECRET)|smtpPassword\s*[:=]\s*['"][^'"]+/i.test(read('config.js')+read('app.js')),'Possível segredo SMTP no frontend.');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}
console.log('validate-cutover-readiness: PASS');
