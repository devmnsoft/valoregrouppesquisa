const {has}=require('./validate-demo-production-common');
has('functions/index.js',/function forbiddenPublicUrl[\s\S]*survey_demo[\s\S]*empresa-exemplo[\s\S]*tokenHash=/,'backend não valida URL pública proibida');
has('functions/index.js',/if\(forbiddenPublicUrl\(publicUrl\)\)throw demoBlockError\('public_url_invalid'/,'preparePublicSurveyDocument aceita URL proibida');
has('app.js',/publicUrlHasForbiddenDemo/,'front não tem bloqueio de URL proibida');
