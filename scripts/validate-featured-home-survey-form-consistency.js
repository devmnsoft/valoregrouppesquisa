const {has}=require('./validate-demo-production-common');
has('functions/index.js',/demo_survey/,'getFeaturedHomeSurvey não rejeita demo_survey');
has('functions/index.js',/demo_company/,'getFeaturedHomeSurvey não rejeita demo_company');
has('functions/index.js',/official_fallback_deprioritized/,'fallback oficial pode competir com real');
has('functions/index.js',/urlSurveyId:survey\.id/,'consistência não retorna urlSurveyId');
