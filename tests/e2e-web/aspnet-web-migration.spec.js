const { test, expect } = require('@playwright/test');
test('Valora.Web migration sem JSON bruto nem stack trace', async ({ page }) => { await page.goto('/Migration'); await expect(page.locator('body')).not.toContainText('JSON.stringify'); await expect(page.locator('body')).not.toContainText('stack trace'); });
