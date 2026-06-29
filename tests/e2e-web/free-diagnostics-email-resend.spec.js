const { test, expect } = require('@playwright/test');
test('painel possui acao de reenvio sanitizada', async ({ page }) => { await page.goto('/FreeDiagnostics'); await expect(page.getByText('Reenviar').first()).toBeVisible({ timeout: 15000 }); });
