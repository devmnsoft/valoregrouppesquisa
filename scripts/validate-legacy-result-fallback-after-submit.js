const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
for(const s of ['function renderFallbackResultAfterSubmit','function normalizeResultRenderError','function normalizePublicResultViewModel','valora:lastPublicResult']) if(!app.includes(s)) fail(`Helper ausente: ${s}`);
const m=app.match(/function handlePublicSubmitSuccess\(result,payload\)\{[\s\S]*?\n\}/);if(!m)fail('handlePublicSubmitSuccess não localizado.');
const b=m[0];for(const s of ['try{','renderResult(responseId,true,resultToken,result)','catch(err)','normalizeResultRenderError(err)','renderFallbackResultAfterSubmit(result,normalized)']) if(!b.includes(s)) fail(`handlePublicSubmitSuccess sem proteção: ${s}`);
console.log('OK fallback after submit');
