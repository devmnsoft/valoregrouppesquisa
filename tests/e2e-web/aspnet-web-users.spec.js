const { test, expect } = require('@playwright/test');
test('Valora.Web users sem JSON bruto nem stack trace', async ({ page }) => { await page.goto('/Users'); await expect(page.locator('body')).not.toContainText('JSON.stringify'); await expect(page.locator('body')).not.toContainText('stack trace'); });
