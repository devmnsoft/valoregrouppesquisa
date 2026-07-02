const {read,assert,ok}=require('./legacy-public-submit-validator-lib');const app=read('app.js');
assert(app.includes('function handlePublicSubmitSuccess(result,payload)'),'handlePublicSubmitSuccess ausente');
assert(app.includes('?result=${encodeURIComponent(result.responseId)}&rt=${encodeURIComponent(result.resultToken)}'),'sucesso não redireciona para resultado');
assert(app.includes('Preparando seu resultado'),'loading de resultado ausente');
assert(app.includes('publicWhatsappContactUrl'),'CTA WhatsApp ausente');ok('sucesso redireciona para resultado');
