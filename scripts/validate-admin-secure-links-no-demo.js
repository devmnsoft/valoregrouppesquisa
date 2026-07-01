const {has}=require('./validate-demo-production-common');
has('app.js',/function openShareModal[\s\S]*assertSurveyShareableInProduction[\s\S]*publicUrlHasForbiddenDemo/,'Compartilhar não bloqueia demo no front');
has('app.js',/function markSurveyAsHomeFeatured[\s\S]*assertSurveyShareableInProduction/,'Destacar não bloqueia demo no front');
