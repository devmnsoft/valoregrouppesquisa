const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8')}
function has(p,s){const t=read(p); if(!t.includes(s)){throw new Error(`${p} missing ${s}`)}}
function no(p,rx){const t=read(p); if(rx.test(t)){throw new Error(`${p} has forbidden ${rx}`)}}
function ok(m){console.log('OK:',m)}
const api='backend/Valora.Api/Controllers/PublicResultsController.cs';
has('backend/Valora.Api/Controllers/PublicSurveysController.cs','/public/surveys/{surveyId:guid}/responses');
has(api,'/public/results/{responseId:guid}'); has(api,'certificate.pdf'); has(api,'certificate.png'); has(api,'/email');
has('backend/Valora.Application/Services/PublicSurveys/PublicResponseTransactionService.cs','CreateEmailJobAsync'); has('backend/Valora.Application/Services/PublicSurveys/PublicResponseTransactionService.cs','CreateMetadataAsync');
has('backend/Valora.Api/Controllers/PublicResultsController.cs','application/pdf');
has('backend/Valora.Web/wwwroot/js/pages/result-page.js','wa.me/5591992545353'); has('backend/Valora.Web/wwwroot/js/core/loading.js','withLoading');
no('backend/Valora.Web/wwwroot/js/pages/public-survey-page.js',/httpsCallable|cloudfunctions|firestore|localStorage\.setItem/i);
ok(process.argv[2]||'dotnet public flow');
