const { test, expect } = require('@playwright/test');

test('Valora.Web official module renders without raw diagnostics', async ({ page }) => {
  await page.goto(process.env.VALORA_WEB_URL || 'http://localhost:5088/', { waitUntil: 'domcontentloaded' });
  await expect(page.locator('body')).not.toContainText('JSON.stringify(result,null,2)');
  await expect(page.locator('body')).not.toContainText('Executar ação');
  await expect(page.locator('body')).not.toContainText('Tela Bootstrap API-first');
  await expect(page.locator('body')).not.toContainText('System.Exception');
});
