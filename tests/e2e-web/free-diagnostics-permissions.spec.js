const { test, expect } = require('@playwright/test');
test('painel declara permissao canViewResponses', async ({ page }) => { await page.goto('/FreeDiagnostics'); await expect(page.locator('[data-required-permission="canViewResponses"]')).toBeVisible(); });
