const {has}=require('./validate-demo-production-common');
has('app.js',/assertPreparedSurveyMatchesSelection[\s\S]*survey\?\.id!==survey\.id[\s\S]*publicUrlHasForbiddenDemo/,'admin não valida retorno do link selecionado');
has('functions/index.js',/surveyIdMatchesRequest:true[\s\S]*selectedSurveyId:surveyId[\s\S]*formMatchesSurvey/,'preparePublicSurveyLink não retorna consistência completa');
