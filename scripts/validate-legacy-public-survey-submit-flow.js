const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
['submitPublicSurveyAuto','submitPublicSurveyViaCloudFunction','submitPublicSurveyViaFirestoreFallback','submitPublicSurveyViaExternalApi','normalizePublicSubmitResult','ensurePublicSubmitIdempotencyKey'].forEach(x=>{if(!app.includes(x))throw new Error('ausente: '+x)});
if(!app.includes("isFree?['cloud-functions','firestore','external-api']"))throw new Error('ordem free incorreta');
console.log('legacy public submit flow: PASS');
