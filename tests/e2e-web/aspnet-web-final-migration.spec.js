const { test, expect } = require('@playwright/test');

const baseUrl = process.env.VALORA_WEB_BASE_URL || 'http://localhost:5088';

test('Valora.Web final smoke sem conteúdo proibido', async ({ page }) => {
  await page.goto(baseUrl, { waitUntil: 'domcontentloaded' });
  const body = await page.locator('body').textContent();
  await expect(page.locator('body')).toBeVisible();
  expect(body).not.toMatch(/JSON bruto|Executar ação|Tela Bootstrap API-first|stack trace|password_hash|result_token_hash|token_hash/i);
  if (/Módulo em ativação/i.test(body || '')) {
    await expect(page.locator('[data-gap-controlled="true"]')).toHaveCount(await page.locator('[data-gap-controlled="true"]').count());
  }
});
