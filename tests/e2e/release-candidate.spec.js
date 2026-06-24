const { test, expect } = require('@playwright/test');
test('rotas principais carregam sem marcadores técnicos', async ({ page }) => {
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto('/');
  await expect(page.locator('body')).toContainText(/Valora|Insight|Pulse/);
  const txt=await page.locator('body').innerText();
  expect(txt).not.toMatch(/undefined|NaN|\[object Object\]|Empresa Exemplo|Cloud Functions|users\/\{uid\}/);
  expect(errors).toEqual([]);
});
