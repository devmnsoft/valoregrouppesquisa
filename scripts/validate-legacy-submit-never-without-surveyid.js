const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('payload builder exists',/function buildPublicSurveySubmitPayload/.test(a));
must('payload uses context surveyId not dataset fallback',/const ctx = getPublicSurveyContext\(\); const surveyId = ctx\.surveyId \|\| ctx\.survey\?\.id \|\| ''/.test(a));
must('validation before submit function',a.indexOf('const validationError=validatePublicSubmitPayload')>-1&&a.indexOf('const validationError=validatePublicSubmitPayload')<a.indexOf('const res=await submitPublicSurveyResponse'));
must('missing_survey_id validator exists',/code: 'missing_survey_id'/.test(a));
