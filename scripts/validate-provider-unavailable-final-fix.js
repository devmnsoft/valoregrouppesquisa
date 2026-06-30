const fs = require('fs');
function fail(message) { console.error(message); process.exit(1); }
function has(file, text) { return fs.readFileSync(file, 'utf8').includes(text); }
const app = fs.readFileSync('app.js', 'utf8');
for (const symbol of ['submitPublicSurveyAuto','submitPublicSurveyViaCloudFunction','submitPublicSurveyViaFirestoreFallback','submitPublicSurveyViaExternalApi','callFirebaseFunction','normalizePublicSubmitResult','ensurePublicSubmitIdempotencyKey']) {
  if (!app.includes(symbol)) fail(`app.js sem ${symbol}`);
}
const auto = app.slice(app.indexOf('async function submitPublicSurveyAuto'), app.indexOf('async function submitPublicSurveyResponse'));
for (const provider of ['cloud-functions','firestore','external-api']) if (!auto.includes(provider)) fail(`fluxo auto sem provider ${provider}`);
if (!(auto.indexOf('cloud-functions') < auto.indexOf('firestore') && auto.indexOf('firestore') < auto.indexOf('external-api'))) fail('ordem obrigatória da pesquisa gratuita deve ser Cloud Functions > Firestore > API externa');
if (!/for\s*\(const provider of providers\)/.test(auto)) fail('submitPublicSurveyAuto deve tentar todos os providers');
if (!/error\.code\s*=\s*['"]provider_unavailable['"]/.test(auto)) fail('provider_unavailable deve ser emitido no final do fluxo');
if (!has('functions/index.js', 'exports.submitSurveyResponse=onCall')) fail('functions/index.js sem submitSurveyResponse callable');
if (!has('firebase-init.js', 'functions') && !has('firebase-init.js', 'getFirebaseFunctionsSafe')) fail('firebase-init.js sem inicialização Functions');
if (!has('api-repository.js', 'submitPublicSurveyResponse')) fail('api-repository.js sem submitPublicSurveyResponse');
if (!has('gateway-client.js', 'callGatewayJson')) fail('gateway-client.js sem callGatewayJson');
console.log('legacy:provider-final-fix OK');
