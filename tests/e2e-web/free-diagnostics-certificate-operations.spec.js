const { test, expect } = require('@playwright/test');
test('painel possui operacao de certificado', async ({ page }) => { await page.goto('/FreeDiagnostics'); await expect(page.getByText('Regenerar').first()).toBeVisible({ timeout: 15000 }); });
