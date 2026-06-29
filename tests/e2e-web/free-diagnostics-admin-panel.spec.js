const { test, expect } = require('@playwright/test');
test('admin visualiza painel de diagnosticos gratuitos', async ({ page }) => { await page.goto('/FreeDiagnostics'); await expect(page.locator('[data-page="free-diagnostics-page"]')).toBeVisible(); });
