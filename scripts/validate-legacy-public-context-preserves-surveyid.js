const {read,assert,ok}=require('./legacy-public-submit-validator-lib');const app=read('app.js');
assert(app.includes('function getPublicSurveyRouteParams()'),'getPublicSurveyRouteParams ausente');
assert(app.includes('function setPublicSurveyContext(context = {})'),'setPublicSurveyContext ausente');
assert(app.includes('function getPublicSurveyContext()'),'getPublicSurveyContext ausente');
assert(app.includes('setPublicSurveyContext({surveyId:sid,token,org:orgSlug'),'validateSurveyLink não salva contexto');
assert(app.includes('data-public-survey-form')&&app.includes('data-survey-id'),'form não persiste surveyId em data-*');ok('contexto público preserva surveyId/token/org');
