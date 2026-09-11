const vm = require('vm');
const fs = require('fs');
const app = fs.readFileSync('app.js', 'utf8');
const config = fs.readFileSync('config.js', 'utf8');
const start = app.indexOf('async function submitPublicSurveyAuto');
const end = app.indexOf('\nasync function submitPublicSurveyResponse', start);
if (start < 0 || end <= start) throw new Error('submitPublicSurveyAuto ausente');
if (!/PUBLIC_SUBMISSION_PROVIDER:\s*'cloud-functions'/.test(config) || !/PUBLIC_SUBMISSION_FALLBACKS:\s*\['cloud-functions'\]/.test(config)) throw new Error('configuração pública não exige somente Cloud Functions');

const calls = { cloudFunctions: 0, firestore: 0, externalApi: 0 };
const sandbox = {
  window: {
    ValoraRepository: { submitPublicSurveyResponse: async payload => { calls.cloudFunctions += 1; return { responseId: 'response-real', resultToken: 'result-token', received: payload }; } },
    ValoraRuntimeDiagnostics: {}
  },
  assertPublicSubmitPayloadReady(payload) { if (!payload.surveyId || !payload.token) throw new Error('contexto inválido'); },
  ensurePublicSubmitIdempotencyKey: payload => ({ ...payload, idempotencyKey: 'idempotency-test' }),
  normalizePublicSubmitResult: result => result,
  submitPublicSurveyViaCloudFunction: async () => { calls.cloudFunctions += 1; return {}; },
  submitPublicSurveyViaFirestoreFallback: async () => { calls.firestore += 1; return {}; },
  submitPublicSurveyViaExternalApi: async () => { calls.externalApi += 1; return {}; },
  publicErrorCode: error => error.code || 'unknown',
  sanitizePublicError: error => error.message
};
vm.runInNewContext(`${app.slice(start, end)};this.submit=submitPublicSurveyAuto`, sandbox);
(async () => {
  const payload = { surveyId: 'survey-cloud', token: 'public-token', org: 'org-cloud' };
  const result = await sandbox.submit(payload);
  if (calls.cloudFunctions !== 1 || calls.firestore !== 0 || calls.externalApi !== 0) throw new Error(`providers incorretos: ${JSON.stringify(calls)}`);
  if (result.received.surveyId !== payload.surveyId || result.received.token !== payload.token || result.received.org !== payload.org) throw new Error('contexto não preservado até Cloud Functions');
  const diagnostic = sandbox.window.ValoraRuntimeDiagnostics.lastPublicSubmit;
  if (JSON.stringify(diagnostic.providersAttempted) !== JSON.stringify(['cloud-functions']) || diagnostic.finalProvider !== 'cloud-functions') throw new Error('diagnóstico de provider incorreto');
  console.log('legacy public submit flow: PASS (Cloud Functions only)');
})().catch(error => { console.error(error); process.exit(1); });
