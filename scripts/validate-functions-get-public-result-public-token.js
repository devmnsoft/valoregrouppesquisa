const fs=require('fs');const s=fs.readFileSync('functions/index.js','utf8');const m=s.match(/exports\.getPublicResult=onCall\(([\s\S]*?)\nexports\./);const body=m?m[1]:'';const fail=[];
['region:\'us-central1\'','responseId=cleanText','resultToken=cleanText','rateLimit(`get-result:','timingSafeEqualHex','sha256(resultToken)','sanitizePublicSurveyForClient','sanitizePublicFormForClient','sanitizePublicCompanyForClient'].forEach(k=>{if(!body.includes(k))fail.push(`ausente: ${k}`)});
if(/req\.auth|authedUser|requiredAuth|permission-denied[^;]+auth/i.test(body)) fail.push('getPublicResult não pode exigir req.auth');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-functions-get-public-result-public-token: PASS');
