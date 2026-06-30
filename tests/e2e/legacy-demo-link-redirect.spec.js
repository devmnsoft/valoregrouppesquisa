const { test, expect } = require('@playwright/test');
test('legacy demo link is blocked or redirected', async ({ page }) => {
  await page.goto('/?survey=survey_demo&token=abc&org=empresa-exemplo');
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
  await expect(page.locator('body')).toContainText(/link de demonstração|diagnóstico gratuito oficial|Diagnóstico gratuito/i);
  expect(page.url()).not.toContain('empresa-exemplo');
});
