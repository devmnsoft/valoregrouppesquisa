const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
for(const x of ['function logPublicSubmitResult','lastSubmitFunctionResult',"[Valora] submitSurveyResponse result",'acceptedCount','rejectedCount','errorMessage'])if(!s.includes(x))throw new Error('submit result trace ausente: '+x);
if(!/const result\s*=\s*await submitPublicSurveyResponse\(payload\);\s*logPublicSubmitResult\(result\);/s.test(s))throw new Error('logPublicSubmitResult não é chamado após submitPublicSurveyResponse(payload)');
console.log('legacy submit result trace: PASS');
