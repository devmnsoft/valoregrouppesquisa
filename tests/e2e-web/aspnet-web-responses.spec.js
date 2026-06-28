const { test, expect } = require('@playwright/test');
test('Valora.Web responses sem JSON bruto nem stack trace', async ({ page }) => { await page.goto('/Responses'); await expect(page.locator('body')).not.toContainText('JSON.stringify'); await expect(page.locator('body')).not.toContainText('stack trace'); });
