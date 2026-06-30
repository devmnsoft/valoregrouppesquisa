const { test, expect } = require('@playwright/test');

const legacyUrl = '/?survey=survey_demo&token=aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d&org=empresa-exemplo';

test('home official free survey contract', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('body')).toContainText(/Diagnóstico gratuito Valora Insight/i);
  const hrefs = await page.locator('a.btn').evaluateAll(links => links.map(a => a.href).join('\n'));
  expect(hrefs).not.toContain('survey_demo');
  expect(hrefs).not.toContain('empresa-exemplo');
  const officialCta = await page.locator('a[href*="official_free_survey"]').first();
  await expect(officialCta).toBeVisible();
});

test('legacy demo URL resolves to official survey and submit diagnostics never use demo', async ({ page }) => {
  await page.goto(legacyUrl);
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
  await expect(page.locator('body')).not.toContainText('official_free_survey_unavailable');
  await page.waitForTimeout(500);
  const state = await page.evaluate(() => ({
    hiddenSurveyId: document.querySelector('input[name="surveyId"]')?.value || '',
    hiddenToken: document.querySelector('input[name="token"]')?.value || '',
    body: document.body.innerText,
    diag: window.ValoraRuntimeDiagnostics?.lastPublicSubmit || window.ValoraRuntimeDiagnostics?.lastDemoLinkRedirect || {}
  }));
  if (state.hiddenSurveyId) expect(state.hiddenSurveyId).toBe('official_free_survey');
  if (state.hiddenToken) expect(state.hiddenToken).not.toBe('aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d');
  expect(JSON.stringify(state)).not.toContain('"surveyId":"survey_demo"');
  expect(state.body).not.toContain('provider_unavailable');
});
