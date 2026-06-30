const { test, expect } = require('@playwright/test');

test('legacy demo submit diagnostics resolve to official survey instead of provider_unavailable', async ({ page }) => {
  await page.goto('/?survey=survey_demo&token=aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d&org=empresa-exemplo');
  await expect(page.locator('input[name="surveyId"]')).toHaveValue('official_free_survey');
  const diagnostics = await page.evaluate(() => window.ValoraRuntimeDiagnostics?.lastDemoLinkRedirect || {});
  expect(diagnostics.targetSurveyId).toBe('official_free_survey');
  expect(diagnostics.targetOrg || 'valora-group').toBe('valora-group');
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
});
