const fs=require('fs');const a=fs.readFileSync('app.js','utf8');function must(n,c){if(!c)throw new Error(n)}
must('inline submit error helper exists',/function showPublicSubmitError/.test(a)&&/publicSubmitInlineError/.test(a));
must('keepForm prepends error to form',/options\.keepForm[\s\S]*form\.prepend\(box\)/.test(a));
must('required local validation uses keepForm',/showPublicSubmitError\(validationError,\{keepForm:true\}\)/.test(a));
console.log('validate-legacy-submit-inline-error-keeps-form: PASS');
