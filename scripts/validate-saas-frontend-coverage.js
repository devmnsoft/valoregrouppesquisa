const {assert,readIf,scanForbiddenFrontendSecrets,pass}=require('./production-gate-utils');
const files=['index.html','app.js','style.css','config.js','api-client.js','api-repository.js','repository.js','gateway-client.js','runtime-capabilities.js']; const body=files.map(readIf).join('\n');
scanForbiddenFrontendSecrets(files);
['Login','Cadastro','senha','Dashboard','Organização|Empresas','Usuários','Planos','Formulários','Pesquisas','Links','Respostas','Resultado','Certificados','Comunicação|E-mails','Auditoria|Logs','Migração','Status','Configurações'].forEach(x=>assert(new RegExp(x,'i').test(body),`frontend screen marker missing: ${x}`));
assert(/bootstrap/i.test(body),'Bootstrap not detected'); assert(!/react|angular|vue/i.test(readIf('package.json')),'forbidden frontend framework dependency detected');
assert(/correlationId/i.test(body),'friendly API error correlationId not shown'); assert(!/stack trace|error\.stack/i.test(body),'stack trace exposure marker found');
pass('validate-saas-frontend-coverage');
