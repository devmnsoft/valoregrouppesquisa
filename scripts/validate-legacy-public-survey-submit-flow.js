const {assert,has,regex,done}=require('./_legacy-validator-lib');
['submitPublicSurveyAuto','submitPublicSurveyViaExternalApi','submitPublicSurveyViaCloudFunction','getPublicResultAuto','sendResultEmailAuto','generateCertificateAfterSubmit','idempotencyKey','Não conseguimos concluir sua pesquisa agora','Detalhe técnico'].forEach(x=>assert(has('app.js',x),`app.js missing ${x}`));
assert(!has('app.js','response_demo'),'response_demo must not be used');assert(regex('app.js',/API.*Cloud Function|Cloud Function.*API/s),'fallback API/Functions missing');done('legacy public submit flow');
