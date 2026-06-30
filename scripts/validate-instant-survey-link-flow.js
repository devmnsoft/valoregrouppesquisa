const {read,assert,done}=require('./public-boot-validator-lib'); const app=read('app.js'); const functions=read('functions/index.js');
const homeFn=app.match(/async function getFeaturedHomeSurveyUrl[\s\S]*?function resolveFeaturedFreeSurvey/)?.[0]||'';
assert(/survey_demo\|empresa-exemplo\|tokenHash=/.test(homeFn),'getFeaturedHomeSurveyUrl deve filtrar survey_demo/empresa-exemplo/tokenHash');
assert(!/surveyParams\.set\('tokenHash'/.test(app),'fluxo público não deve gerar tokenHash na URL');
assert(/preparePublicSurveyLink/.test(functions),'Function preparePublicSurveyLink ausente');
assert(/validateSurveyLink/.test(functions),'Function validateSurveyLink ausente');
done('validate-instant-survey-link-flow');
