const {has}=require('./validate-demo-production-common');
has('data-normalization.js',/function isDemoRecord[\s\S]*empresa-exemplo[\s\S]*demo-token/,'isDemoRecord central incompleto');
has('data-normalization.js',/function isBlockedInProduction/,'isBlockedInProduction ausente');
has('app.js',/function loadSurveys\(\)[\s\S]*isBlockedInProduction/,'loadSurveys não filtra demo em produção');
has('local-repository.js',/withoutProductionDemoRows|isBlockedInProduction/,'local repository não bloqueia demo em produção');
