const {read,assert,ok}=require('./legacy-public-submit-validator-lib');const app=read('app.js'), repo=read('firebase-repository.js');
assert(app.includes('function buildPublicSurveySubmitPayload(formEl)'),'buildPublicSurveySubmitPayload ausente');
assert(app.includes("code: 'missing_survey_id'")&&app.includes("code: 'missing_public_token'"),'validação local não bloqueia surveyId/token');
assert(app.includes('const validationError=validatePublicSubmitPayload(submitPayload,context,form);')&&app.indexOf('const validationError=validatePublicSubmitPayload')<app.indexOf('const res=await submitPublicSurveyResponse(submitPayload)'),'submit chama function antes da validação');
assert(repo.includes("if(!payload?.surveyId)")&&repo.includes("e.code='missing_survey_id'"),'repository não bloqueia submit sem surveyId');ok('submit não segue sem surveyId/token');
