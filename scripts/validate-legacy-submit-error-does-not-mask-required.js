const fs=require('fs');const a=fs.readFileSync('app.js','utf8');function must(n,c){if(!c)throw new Error(n)}
must('normalizer maps required details',/details\.questionId[\s\S]*required_question_missing/.test(a));
must('required message preserved',/required_question_missing:'Responda todas as perguntas obrigatórias antes de enviar\.'/.test(a));
must('required errors not routed to provider_unavailable',!/provider_unavailable[\s\S]{0,120}required_question_missing/.test(a));
console.log('validate-legacy-submit-error-does-not-mask-required: PASS');
