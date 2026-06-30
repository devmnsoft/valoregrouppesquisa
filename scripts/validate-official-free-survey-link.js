const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
['loadOfficialFreeSurvey','ensureOfficialFreeSurveyPublicLink','isOfficialFreeSurvey','buildOfficialFreeSurveyUrl','publicToken||survey.token||survey.accessToken'].forEach(x=>{if(!app.includes(x))throw new Error('ausente: '+x)});
if(app.includes("url.searchParams.set('token',survey.tokenHash)"))throw new Error('tokenHash usado como token público');
console.log('legacy official free link: PASS');
