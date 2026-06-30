const { test, expect } = require('@playwright/test');

const legacyUrl = '/?survey=survey_demo&token=aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d&org=empresa-exemplo';

test('legacy survey_demo URL renders the official free survey form and hidden fields', async ({ page }) => {
  await page.goto(legacyUrl);
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
  await expect(page.locator('body')).not.toContainText('official_free_survey_unavailable');
  await expect(page.locator('body')).toContainText(/Diagnóstico gratuito|Valora Insight|Valora Group/i);
  await expect(page.locator('input[name="surveyId"]')).toHaveValue('official_free_survey');
  await expect(page.locator('input[name="token"]')).not.toHaveValue('aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d');
});
