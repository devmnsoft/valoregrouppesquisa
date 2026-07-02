const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('payload builder exists',/function buildPublicSurveySubmitPayload/.test(a));
must('payload builder uses form-aware context',/const ctx = getPublicSurveyContext\(formEl\); const fd = data\(formEl\)/.test(a));
must('payload builder falls back to fd surveyId token',/ctx\.surveyId \|\| fd\.surveyId/.test(a)&&/ctx\.token \|\| fd\.token/.test(a));
must('validation before submit function',a.indexOf('const validationError=validatePublicSubmitPayload')>-1&&a.indexOf('const validationError=validatePublicSubmitPayload')<a.indexOf('const res=await submitPublicSurveyResponse'));
must('extra guard blocks missing surveyId token before function',/if\(!submitPayload\.surveyId \|\| !submitPayload\.token\)/.test(a)&&a.indexOf('if(!submitPayload.surveyId || !submitPayload.token)')<a.indexOf('const res=await submitPublicSurveyResponse'));
