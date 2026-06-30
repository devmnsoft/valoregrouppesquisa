if(process.env.ALLOW_PRODUCTION_EMAIL_TEST!=='true'){console.log('Teste de e-mail bloqueado: defina ALLOW_PRODUCTION_EMAIL_TEST=true.');process.exit(0)}
for(const k of ['TEST_PARTICIPANT_EMAIL','TEST_PARTICIPANT_NAME'])if(!process.env[k]){console.error(`${k} obrigatório`);process.exit(1)}
console.log('Teste de e-mail produção deve usar sendResultEmail e nunca logar token/senha.');
