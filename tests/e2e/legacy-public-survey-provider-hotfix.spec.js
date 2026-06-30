const { test, expect } = require('@playwright/test');

test('legacy free public survey does not expose provider_unavailable when provider succeeds', async ({ page }) => {
  await page.goto('/');
  await page.addInitScript(() => {
    window.ValoraRuntimeDiagnostics = {};
  });
  const text = await page.locator('body').innerText();
  expect(text).not.toContain('provider_unavailable');
  await expect(page.locator('body')).toContainText(/Valora|diagnóstico|pesquisa/i);
});
