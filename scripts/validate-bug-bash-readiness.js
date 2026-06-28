const fs=require('fs'); const {assert,writeJson}=require('./live-gate-utils');
const checklist='BUG_BASH_FINAL_CHECKLIST.md', findings='BUG_BASH_FINDINGS.md', risks='BUG_RISK_REGISTER.md';
for(const f of [checklist,findings,risks]) assert(fs.existsSync(f),`${f} ausente`);
const c=fs.readFileSync(checklist,'utf8'); ['cadastro de empresa','login','logout','recuperar senha','criar pesquisa','responder pesquisa no celular','ver resultado','baixar certificado','enviar e-mail','dashboard','auditoria','planos','limite do plano','API offline','token inválido','menu mobile','link público expirado','submissão duplicada','rollback'].forEach(x=>assert(new RegExp(x,'i').test(c),`checklist sem ${x}`));
writeJson('reports/bug-bash-readiness.json',{generatedAt:new Date().toISOString(),status:'PASS'}); console.log('validate-bug-bash-readiness: PASS');
