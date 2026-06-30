const fs=require('fs');
const s=fs.readFileSync('functions/index.js','utf8');
const req=['exports.preparePublicSurveyLink=onCall','preparePublicSurveyDocument','exports.getFeaturedHomeSurvey=onCall','exports.validateSurveyLink=onCall','exports.submitSurveyResponse=onCall','exports.getPublicResult=onCall','exports.lookupCnpj=onCall'];
for(const x of req)if(!s.includes(x))throw new Error('missing '+x);
if(/searchParams\.set\(['"]tokenHash/.test(s))throw new Error('public URL may expose tokenHash');
console.log('validate-functions-public-survey-map: PASS');
