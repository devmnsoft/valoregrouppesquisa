const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('ValoraPublicSurveyState exists',/ValoraPublicSurveyState/.test(a));
must('state helpers exist',/function setPublicSurveyState/.test(a)&&/function getPublicSurveyState/.test(a));
must('route helpers exist',/function getPublicSurveyRouteParams/.test(a)&&/function hasPublicSurveyRouteParams/.test(a)&&/function isDemoOrInvalidPublicRoute/.test(a));
must('resolvePublicSurveyContext exists',/async function resolvePublicSurveyContext/.test(a));
