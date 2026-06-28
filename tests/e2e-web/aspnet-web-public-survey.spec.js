const { test, expect } = require('@playwright/test');

test('Valora.Web shell renders', async ({ page }) => {
  await page.goto(process.env.VALORA_WEB_URL || 'http://localhost:5088');
  await expect(page.locator('body')).toContainText('Valora Pulse');
});
