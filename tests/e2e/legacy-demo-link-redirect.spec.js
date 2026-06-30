const { test, expect } = require('@playwright/test');
test('legacy survey_demo URL resolves to official free survey without exposing unavailable errors', async ({ page }) => {
  await page.goto('/?survey=survey_demo&token=abc&org=empresa-exemplo');
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
  await expect(page.locator('body')).not.toContainText('official_free_survey_unavailable');
  await expect(page.locator('body')).toContainText(/Diagnóstico gratuito|Valora Insight|Valora Group/i);
  expect(page.url()).not.toContain('empresa-exemplo');
  expect(page.url()).not.toContain('tokenHash=');
});
