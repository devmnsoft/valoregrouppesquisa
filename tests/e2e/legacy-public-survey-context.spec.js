const { test, expect } = require('@playwright/test');

const surveyPayload = (id, formId = `form-${id}`, questions = [{ id: 'q1', type: 'text', required: true }]) => ({
  ok: true,
  survey: { id, formId, title: `Survey ${id}`, lgpdRequired: false },
  form: { id: formId, name: `Form ${id}`, description: 'Behavior fixture', questions },
  company: { id: `company-${id}`, publicName: 'Valora Group' }
});

test.beforeEach(async ({ page }) => {
  await page.goto('/');
  await page.waitForFunction(() => typeof window.renderTakeSurvey === 'function');
});

test('@legacy-public-context uses sid/token/org arguments even when location.search is empty and keeps them for submit', async ({ page }) => {
  const state = await page.evaluate(async payload => {
    window.validatePublicSurveyLink = async input => ({ ...payload, received: input });
    await window.renderTakeSurvey('argument-survey', 'argument-token', 'argument-org');
    const form = document.querySelector('[data-public-survey-form]');
    form.querySelector('[name="name"]').value = 'Pessoa Teste';
    form.querySelector('[name="email"]').value = 'pessoa@example.com';
    form.querySelector('[name="accessPassword"]').value = 'segredo-teste';
    form.querySelector('[name="q_q1"]').value = 'Resposta';
    const submit = window.buildPublicSurveySubmitPayload(form);
    return { publicState: window.ValoraPublicSurveyState, submit: { surveyId: submit.surveyId, token: submit.token, org: submit.org, answers: submit.answers } };
  }, surveyPayload('argument-survey'));
  expect(state.publicState.status).toBe('ready');
  expect(state.publicState.context.surveyId).toBe('argument-survey');
  expect(state.publicState.context.token).toBe('argument-token');
  expect(state.publicState.context.org).toBe('argument-org');
  expect(state.submit).toEqual({ surveyId: 'argument-survey', token: 'argument-token', org: 'argument-org', answers: { q1: 'Resposta' } });
});

test('@legacy-public-context reload/direct URL preserves the complete route context', async ({ page }) => {
  await page.goto('/?surveyId=direct-survey&token=direct-token&org=direct-org');
  await page.waitForFunction(() => typeof window.renderTakeSurvey === 'function');
  await page.evaluate(async payload => { window.validatePublicSurveyLink = async () => payload; await window.renderTakeSurvey(); }, surveyPayload('direct-survey'));
  await expect(page.locator('[data-public-survey-form]')).toHaveAttribute('data-survey-id', 'direct-survey');
  await expect(page.locator('[data-public-survey-form]')).toHaveAttribute('data-org', 'direct-org');
  await expect(page.locator('[name="token"]')).toHaveValue('direct-token');
  await expect(page.locator('[data-question-id="q1"]')).toBeVisible();
});

test('@legacy-no-form-without-context invalid token and invalid context never render or submit a form', async ({ page }) => {
  const attempts = await page.evaluate(async () => {
    let submitAttempts = 0;
    window.submitPublicSurveyResponse = async () => { submitAttempts += 1; return {}; };
    await window.renderTakeSurvey('missing-token', '', 'org');
    await window.guardedPublicSurveySubmit(document.querySelector('[data-public-survey-form]'));
    return submitAttempts;
  });
  expect(attempts).toBe(0);
  await expect(page.locator('[data-public-survey-form]')).toHaveCount(0);

  for (const [payload, code] of [
    [{ ok: true, survey: { id: 's', formId: 'f' } }, 'public_validation_failed'],
    [surveyPayload('s', 'f'), 'survey_form_mismatch'],
    [surveyPayload('s', 'form-s', []), 'public_form_questions_missing']
  ]) {
    if (code === 'survey_form_mismatch') payload.form.id = 'other';
    const actual = await page.evaluate(async ({ payload, code }) => {
      await window.renderTakeSurvey('s', 'token', 'org', payload);
      return { code: window.ValoraPublicSurveyState.error?.code, forms: document.querySelectorAll('[data-public-survey-form]').length };
    }, { payload, code });
    expect(actual).toEqual({ code, forms: 0 });
  }
  const invalidToken = await page.evaluate(async () => {
    window.validatePublicSurveyLink = async () => { throw Object.assign(new Error('Token inválido.'), { code: 'invalid_public_token' }); };
    await window.renderTakeSurvey('s', 'invalid-token', 'org');
    return { code: window.ValoraPublicSurveyState.error?.code, forms: document.querySelectorAll('[data-public-survey-form]').length };
  });
  expect(invalidToken).toEqual({ code: 'invalid_public_token', forms: 0 });
});

test('@legacy-public-context home button opens the canonical link without losing surveyId, token or org', async ({ page }) => {
  const canonical = '/?survey=home-survey&token=home-token&org=home-org';
  await page.evaluate(url => {
    window.ValoraRepository.resolveFeaturedHomeSurvey = async () => ({ url: new URL(url, location.origin).href });
  }, canonical);
  await page.locator('[data-action="startFreeDiagnostic"]').first().click();
  await page.waitForURL(url => url.searchParams.get('survey') === 'home-survey');
  expect(new URL(page.url()).searchParams.get('token')).toBe('home-token');
  expect(new URL(page.url()).searchParams.get('org')).toBe('home-org');
});

test('@legacy-public-submit-flow public submission uses only Cloud Functions and preserves context', async ({ page }) => {
  const result = await page.evaluate(async payload => {
    const calls = { cloudFunctions: 0, firestore: 0, externalApi: 0 };
    window.ValoraRepository.submitPublicSurveyResponse = async received => {
      calls.cloudFunctions += 1;
      return { responseId: 'response-real', resultToken: 'result-token', received };
    };
    window.submitPublicSurveyViaFirestoreFallback = async () => { calls.firestore += 1; throw new Error('provider antigo chamado'); };
    window.submitPublicSurveyViaExternalApi = async () => { calls.externalApi += 1; throw new Error('provider antigo chamado'); };
    const response = await window.submitPublicSurveyAuto(payload);
    return { calls, response, diagnostics: window.ValoraRuntimeDiagnostics.lastPublicSubmit };
  }, { surveyId: 'survey-cloud', token: 'public-token', org: 'org-cloud', participant: { name: 'Teste', email: 'teste@example.com' }, answers: { q1: 'ok' }, renderedQuestionIds: ['q1'], form: { id: 'f1', questions: [{ id: 'q1', required: true }] }, survey: { id: 'survey-cloud', formId: 'f1' } });

  expect(result.calls).toEqual({ cloudFunctions: 1, firestore: 0, externalApi: 0 });
  expect(result.response.received).toMatchObject({ surveyId: 'survey-cloud', token: 'public-token', org: 'org-cloud' });
  expect(result.diagnostics.providersAttempted).toEqual(['cloud-functions']);
  expect(result.diagnostics.finalProvider).toBe('cloud-functions');
});

test('an older validation cannot overwrite a newer navigation', async ({ page }) => {
  const state = await page.evaluate(async ({ first, second }) => {
    let releaseFirst;
    window.validatePublicSurveyLink = input => input.surveyId === 'old'
      ? new Promise(resolve => { releaseFirst = () => resolve(first); })
      : Promise.resolve(second);
    const oldLoad = window.renderTakeSurvey('old', 'old-token', 'org');
    await new Promise(resolve => setTimeout(resolve, 0));
    await window.renderTakeSurvey('new', 'new-token', 'org');
    releaseFirst();
    await oldLoad;
    return { status: window.ValoraPublicSurveyState.status, surveyId: window.ValoraPublicSurveyState.context?.surveyId, domSurveyId: document.querySelector('[data-public-survey-form]')?.dataset.surveyId };
  }, { first: surveyPayload('old'), second: surveyPayload('new') });
  expect(state).toEqual({ status: 'ready', surveyId: 'new', domSurveyId: 'new' });
});

test('an incomplete newer navigation invalidates a pending validation', async ({ page }) => {
  const state = await page.evaluate(async payload => {
    let release;
    window.validatePublicSurveyLink = () => new Promise(resolve => { release = () => resolve(payload); });
    const pending = window.renderTakeSurvey('old', 'old-token', 'org');
    await new Promise(resolve => setTimeout(resolve, 0));
    await window.renderTakeSurvey('new', '', 'org');
    release();
    await pending;
    return {
      status: window.ValoraPublicSurveyState.status,
      code: window.ValoraPublicSurveyState.error?.code,
      context: window.ValoraPublicSurveyState.context,
      forms: document.querySelectorAll('[data-public-survey-form]').length
    };
  }, surveyPayload('old'));
  expect(state).toEqual({ status: 'invalid_link', code: 'missing_public_token', context: null, forms: 0 });
});
