const {read,assert,ok}=require('./legacy-public-submit-validator-lib');const fn=read('functions/index.js');
assert(fn.includes('function publicSubmitPayloadFromRequest(req)'),'helper publicSubmitPayloadFromRequest ausente');
assert(fn.includes('raw.payload')&&fn.includes('data.payload'),'não aceita payload flat/aninhado/duplo');
assert(fn.includes("data.surveyId||data.survey")&&fn.includes("data.token||data.publicToken||data.accessToken"),'aliases survey/token ausentes');
assert(fn.includes("code:'missing_survey_id'")&&fn.includes("code:'missing_public_token'"),'erros claros ausentes');ok('function compatível com contratos legado/novo');
