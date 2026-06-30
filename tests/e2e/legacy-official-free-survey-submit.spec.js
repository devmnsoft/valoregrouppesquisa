const { test, expect } = require('@playwright/test');

test('official free survey submit contract never exposes survey_demo in rendered form', async ({ page }) => {
  await page.goto('/?survey=survey_demo&token=aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d&org=empresa-exemplo');
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
  await expect(page.locator('input[name="surveyId"]')).toHaveValue('official_free_survey');
  await expect(page.locator('input[name="token"]')).not.toHaveValue('');
  const submitDiag = await page.evaluate(() => window.ValoraRuntimeDiagnostics?.lastPublicSubmit || {});
  expect(submitDiag.surveyId || submitDiag.resolvedSurveyId || 'official_free_survey').not.toBe('survey_demo');
});
