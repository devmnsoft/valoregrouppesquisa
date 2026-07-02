const fs=require('fs');const a=fs.readFileSync('app.js','utf8');function must(n,c){if(!c)throw new Error(n)}
const submit=a.slice(a.indexOf('async function submitSurvey'));
must('local validation before Function call',submit.indexOf('validatePublicSubmitPayload')>-1&&submit.indexOf('validatePublicSubmitPayload')<submit.indexOf('submitPublicSurveyResponse'));
must('validation returns before Function',/if\(validationError\)[\s\S]*return false;[\s\S]*submitPublicSurveyResponse/.test(submit));
must('required question highlighted',/question-card-error/.test(a)&&/required_question_missing/.test(a));
console.log('validate-legacy-required-questions-before-function: PASS');
