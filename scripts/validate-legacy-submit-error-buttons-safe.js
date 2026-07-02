const {read,assert,ok}=require('./legacy-public-submit-validator-lib');const app=read('app.js');
assert(app.includes('function retryPublicSurvey()'),'retry seguro ausente');
assert(app.includes('Não há link de pesquisa válido para tentar novamente.'),'retry sem contexto não tratado');
assert(app.includes('ctx.surveyId&&ctx.token')&&app.includes('renderTakeSurvey(ctx.surveyId,ctx.token'),'retry não exige surveyId/token');
assert(app.includes('redirectToFeaturedHomeSurvey'),'voltar diagnóstico não gera link oficial');ok('botões de erro seguros');
