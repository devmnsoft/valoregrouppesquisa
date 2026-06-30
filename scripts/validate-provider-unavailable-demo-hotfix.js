const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
['resolveProductionPublicSurveyLink','lastPublicSubmit','provider_unavailable','providersAttempted','redirectedFromDemo'].forEach(x=>{if(!app.includes(x))throw new Error('ausente: '+x)});
const route=app.match(/function routeFromLocation[\s\S]*?function renderCertificateValidation/)?.[0]||'';
if(!route.includes('resolveProductionPublicSurveyLink'))throw new Error('rota pública não chama hotfix');
console.log('legacy provider demo hotfix: PASS');
