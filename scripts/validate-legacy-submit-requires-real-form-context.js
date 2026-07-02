const fs=require('fs');const a=fs.readFileSync('app.js','utf8');function must(n,c){if(!c)throw new Error(n)}
must('setPublicSurveyContext rejects incomplete context',/function setPublicSurveyContext[\s\S]*public_survey_context_incomplete/.test(a));
must('payload builder rejects missing questions',/function buildPublicSurveySubmitPayload[\s\S]*public_form_questions_missing[\s\S]*collectPublicSurveyAnswers\(formEl, ctx\.form\)/.test(a));
must('validator requires real questions',/function validatePublicSubmitPayload[\s\S]*!Array\.isArray\(formDefinition\.questions\)[\s\S]*public_form_questions_missing/.test(a));
console.log('validate-legacy-submit-requires-real-form-context: PASS');
