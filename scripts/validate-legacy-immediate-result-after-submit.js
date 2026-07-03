const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
const h=s.indexOf('function handlePublicSubmitSuccess');if(h<0)fail('handlePublicSubmitSuccess não existe');
const body=s.slice(h,s.indexOf('\n\nfunction assertPublicSubmitPayloadReady',h));
if(!s.includes('function renderImmediateResultAfterSubmit'))fail('renderImmediateResultAfterSubmit não existe');
if(!s.includes('function normalizeImmediateSubmitResultViewModel'))fail('normalizeImmediateSubmitResultViewModel não existe');
const a=body.indexOf('renderImmediateResultAfterSubmit'), b=body.indexOf('renderResult'); if(b>=0 && (a<0||b<a)) fail('handlePublicSubmitSuccess chama renderResult antes da tela imediata');
console.log('OK legacy immediate result after submit');
