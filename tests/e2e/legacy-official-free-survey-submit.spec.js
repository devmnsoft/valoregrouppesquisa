const { test, expect } = require('@playwright/test');
test('legacy official free survey submit contract', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
  await expect(page.locator('body')).toContainText(/Diagnóstico gratuito|Valora Insight|Responder diagnóstico/i);
});
