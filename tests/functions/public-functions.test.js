const assert = require('assert');
const crypto = require('crypto');
const { initializeApp } = require('firebase/app');
const { getFunctions, connectFunctionsEmulator, httpsCallable } = require('firebase/functions');
const admin = require('firebase-admin');

const PROJECT_ID = process.env.GCLOUD_PROJECT || 'valora-security-rules-test';
const TOKEN = 'token-valido-123';
const RESULT_TOKEN = 'resultado-valido-123';

function sha256(value) {
  return crypto.createHash('sha256').update(String(value)).digest('hex');
}

function clientFunctions() {
  const app = initializeApp({ projectId: PROJECT_ID, apiKey: 'demo-key', appId: 'demo-app' }, `fn-tests-${Date.now()}-${Math.random()}`);
  const functions = getFunctions(app, 'us-central1');
  connectFunctionsEmulator(functions, '127.0.0.1', 5001);
  return {
    validateSurveyLink: httpsCallable(functions, 'validateSurveyLink'),
    submitSurveyResponse: httpsCallable(functions, 'submitSurveyResponse'),
    getPublicResult: httpsCallable(functions, 'getPublicResult'),
    sendResultEmail: httpsCallable(functions, 'sendResultEmail'),
  };
}

async function resetData(overrides = {}) {
  const app = admin.apps.length ? admin.app() : admin.initializeApp({ projectId: PROJECT_ID });
  const db = app.firestore();
  await db.recursiveDelete(db.collection('companies').doc('empresa-a'));
  await db.recursiveDelete(db.collection('plans').doc('pro'));
  await db.recursiveDelete(db.collection('forms').doc('form-a'));
  await db.recursiveDelete(db.collection('surveys').doc('survey-a'));
  await db.recursiveDelete(db.collection('responses'));
  await db.collection('companies').doc('empresa-a').set({ name: 'Empresa A', status: 'active', planId: 'pro' });
  await db.collection('plans').doc('pro').set({ participationEnabled: true, maxResponsesMonth: 1000 });
  await db.collection('forms').doc('form-a').set({
    name: 'Form A',
    companyId: 'empresa-a',
    questions: [{ id: 'q1', text: 'Pergunta obrigatória', type: 'scale', required: true, maxScore: 5 }],
    resultBands: [{ from: 0, to: 5, label: 'Resultado', recommendation: 'OK' }],
  });
  await db.collection('surveys').doc('survey-a').set({
    title: 'Pesquisa A',
    companyId: 'empresa-a',
    formId: 'form-a',
    status: 'active',
    tokenHash: sha256(TOKEN),
    responseCount: 0,
    allowRepeat: false,
    requireIdentification: true,
    lgpdRequired: true,
    expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000),
    ...overrides.survey,
  });
  if (overrides.response) {
    await db.collection('responses').doc('resp-publica').set({
      surveyId: 'survey-a',
      companyId: 'empresa-a',
      participant: { name: 'Ana', email: 'ana@example.com' },
      resultTokenHash: sha256(RESULT_TOKEN),
      normalized5: 4,
      percentage: 80,
      ...overrides.response,
    });
    if (overrides.resultAccess !== false) {
      await db.collection('responses').doc('resp-publica').collection('resultAccessTokens').doc(sha256(RESULT_TOKEN)).set({
        tokenHash: sha256(RESULT_TOKEN),
        responseId: 'resp-publica',
        status: 'active',
        channel: 'test',
        createdAt: new Date(),
        expiresAt: null,
        ...overrides.resultAccess,
      });
    }
  }
  return db;
}

async function expectCode(promise, code) {
  await assert.rejects(promise, (err) => err.code === `functions/${code}` || err.code === code);
}

describe('Cloud Functions públicas — links, respostas e resultados', () => {
  let fn;

  beforeEach(async () => {
    fn = clientFunctions();
    await resetData();
  });

  it('validateSurveyLink aceita token válido', async () => {
    const res = await fn.validateSurveyLink({ surveyId: 'survey-a', token: TOKEN });
    assert.equal(res.data.survey.id, 'survey-a');
    assert.equal(res.data.company.name, 'Empresa A');
  });

  it('validateSurveyLink rejeita token inválido', async () => {
    await expectCode(fn.validateSurveyLink({ surveyId: 'survey-a', token: 'invalido' }), 'permission-denied');
  });

  it('validateSurveyLink rejeita pesquisa expirada', async () => {
    await resetData({ survey: { expiresAt: new Date(Date.now() - 60_000) } });
    await expectCode(fn.validateSurveyLink({ surveyId: 'survey-a', token: TOKEN }), 'failed-precondition');
  });

  it('validateSurveyLink rejeita pesquisa encerrada', async () => {
    await resetData({ survey: { status: 'closed' } });
    await expectCode(fn.validateSurveyLink({ surveyId: 'survey-a', token: TOKEN }), 'failed-precondition');
  });

  it('submitSurveyResponse aceita resposta válida e ignora companyId adulterado', async () => {
    const res = await fn.submitSurveyResponse({ payload: { surveyId: 'survey-a', token: TOKEN, companyId: 'empresa-b', participant: { name: 'Ana', email: 'ana@example.com' }, lgpdConsent: true, answers: { q1: 5 } } });
    assert.ok(res.data.responseId);
    const snap = await admin.firestore().collection('responses').doc(res.data.responseId).get();
    assert.equal(snap.data().companyId, 'empresa-a');
  });

  it('submitSurveyResponse rejeita pergunta obrigatória sem resposta', async () => {
    await expectCode(fn.submitSurveyResponse({ payload: { surveyId: 'survey-a', token: TOKEN, participant: { name: 'Ana', email: 'ana@example.com' }, lgpdConsent: true, answers: {} } }), 'invalid-argument');
  });

  it('submitSurveyResponse rejeita duplicado quando allowRepeat=false', async () => {
    const payload = { surveyId: 'survey-a', token: TOKEN, participant: { name: 'Ana', email: 'ana@example.com' }, lgpdConsent: true, answers: { q1: 5 } };
    await fn.submitSurveyResponse({ payload });
    await expectCode(fn.submitSurveyResponse({ payload }), 'already-exists');
  });

  it('getPublicResult aceita token válido', async () => {
    await resetData({ response: {} });
    const res = await fn.getPublicResult({ responseId: 'resp-publica', resultToken: RESULT_TOKEN });
    assert.equal(res.data.id, 'resp-publica');
  });

  it('getPublicResult rejeita token inválido', async () => {
    await resetData({ response: {} });
    await expectCode(fn.getPublicResult({ responseId: 'resp-publica', resultToken: 'invalido' }), 'permission-denied');
  });

  it('getPublicResult rejeita token revogado', async () => {
    await resetData({ response: {}, resultAccess: { status: 'revoked', revokedAt: new Date() } });
    await expectCode(fn.getPublicResult({ responseId: 'resp-publica', resultToken: RESULT_TOKEN }), 'permission-denied');
  });

  it('getPublicResult rejeita token expirado', async () => {
    await resetData({ response: {}, resultAccess: { expiresAt: new Date(Date.now() - 60_000) } });
    await expectCode(fn.getPublicResult({ responseId: 'resp-publica', resultToken: RESULT_TOKEN }), 'permission-denied');
  });

  it('getPublicResult rejeita token emitido para outra resposta', async () => {
    await resetData({ response: {}, resultAccess: false });
    const db = admin.firestore();
    await db.collection('responses').doc('resp-outra').set({ companyId: 'empresa-a', surveyId: 'survey-a' });
    await db.collection('responses').doc('resp-outra').collection('resultAccessTokens').doc(sha256(RESULT_TOKEN)).set({ tokenHash: sha256(RESULT_TOKEN), responseId: 'resp-outra', status: 'active' });
    await expectCode(fn.getPublicResult({ responseId: 'resp-publica', resultToken: RESULT_TOKEN }), 'permission-denied');
  });

  it('sendResultEmail rejeita token inválido antes de enviar e-mail', async () => {
    await resetData({ response: {} });
    await expectCode(fn.sendResultEmail({ responseId: 'resp-publica', resultToken: 'invalido', to: 'ana@example.com' }), 'permission-denied');
  });

  it('diagnósticos e erros não persistem token bruto', async () => {
    const raw = 'token-bruto-super-secreto';
    await resetData({ response: {} });
    await expectCode(fn.getPublicResult({ responseId: 'resp-publica', resultToken: raw }), 'permission-denied');
    const docs = await admin.firestore().collection('public_result_access_errors').get();
    for (const doc of docs.docs) {
      assert.equal(JSON.stringify(doc.data()).includes(raw), false, 'raw token must not be written to diagnostics');
    }
  });

});
