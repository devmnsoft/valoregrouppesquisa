const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('normalizer exists',/function normalizePublicSubmitError/.test(a));
must('missing_survey_id message mapped',/missing_survey_id:'Link da pesquisa incompleto/.test(a));
must('public error uses normalized code',/const code=submitNormalized\.code/.test(a));
must('no provider_unavailable mask for missing survey',!/missing_survey_id[\s\S]{0,300}provider_unavailable/.test(a));
