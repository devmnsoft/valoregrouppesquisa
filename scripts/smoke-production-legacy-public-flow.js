if(process.env.ALLOW_PRODUCTION_SURVEY_SMOKE!=='true'){console.log('Smoke bloqueado: defina ALLOW_PRODUCTION_SURVEY_SMOKE=true.');process.exit(0)}
for(const k of ['TEST_PARTICIPANT_EMAIL','TEST_PARTICIPANT_NAME'])if(!process.env[k]){console.error(`${k} obrigatório`);process.exit(1)}
console.log('Smoke produção deve ser executado contra endpoint configurado com idempotencyKey e logs sanitizados.');
