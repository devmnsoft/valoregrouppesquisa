const {read,assert,ok}=require('./legacy-public-submit-validator-lib');const app=read('app.js'), repo=read('firebase-repository.js');
assert(app.includes('function normalizePublicSubmitError(err)'),'normalizador app ausente');
assert(app.includes('missing_survey_id')&&repo.includes('missing_survey_id'),'missing_survey_id não mapeado');
assert(!/missing_survey_id[\s\S]{0,300}provider_unavailable/.test(app+repo),'missing_survey_id pode virar provider_unavailable');ok('provider_unavailable não mascara erros conhecidos');
