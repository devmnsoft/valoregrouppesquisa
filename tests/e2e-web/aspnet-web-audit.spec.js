const { test, expect } = require('@playwright/test');
test('Valora.Web audit sem JSON bruto nem stack trace', async ({ page }) => { await page.goto('/Audit'); await expect(page.locator('body')).not.toContainText('JSON.stringify'); await expect(page.locator('body')).not.toContainText('stack trace'); });
