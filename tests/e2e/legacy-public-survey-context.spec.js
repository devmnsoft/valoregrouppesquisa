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

test('uses sid/token arguments even when location.search is empty and keeps them for submit', async ({ page }) => {
  const state = await page.evaluate(async payload => {
    window.validatePublicSurveyLink = async input => ({ ...payload, received: input });
    await window.renderTakeSurvey('argument-survey', 'argument-token', 'argument-org');
    const form = document.querySelector('[data-public-survey-form]');
    form.querySelector('[name="name"]').value = 'Pessoa Teste';
    form.querySelector('[name="email"]').value = 'pessoa@example.com';
    form.querySelector('[name="accessPassword"]').value = 'segredo-teste';
    form.querySelector('[name="q_q1"]').value = 'Resposta';
    const submit = window.buildPublicSurveySubmitPayload(form);
    return { publicState: window.ValoraPublicSurveyState, submit: { surveyId: submit.surveyId, token: submit.token, answers: submit.answers } };
  }, surveyPayload('argument-survey'));
  expect(state.publicState.status).toBe('ready');
  expect(state.publicState.context.surveyId).toBe('argument-survey');
  expect(state.publicState.context.token).toBe('argument-token');
  expect(state.submit).toEqual({ surveyId: 'argument-survey', token: 'argument-token', answers: { q1: 'Resposta' } });
});

test('reload/direct URL validates aliases and incomplete or invalid contracts stay blocked', async ({ page }) => {
  await page.goto('/?surveyId=direct-survey&token=direct-token&org=direct-org');
  await page.waitForFunction(() => typeof window.renderTakeSurvey === 'function');
  await page.evaluate(async payload => { window.validatePublicSurveyLink = async () => payload; await window.renderTakeSurvey(); }, surveyPayload('direct-survey'));
  await expect(page.locator('[data-public-survey-form]')).toHaveAttribute('data-survey-id', 'direct-survey');
  await page.evaluate(async () => window.renderTakeSurvey('missing-token', '', 'org'));
  await expect(page.locator('[data-public-survey-form]')).toHaveCount(0);
});

test('rejects missing, mismatched and empty forms with their specific error', async ({ page }) => {
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
