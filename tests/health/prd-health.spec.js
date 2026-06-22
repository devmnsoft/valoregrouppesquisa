const { test, expect } = require('@playwright/test');
const url = process.env.PRD_HEALTH_URL || 'https://valoragroup.mnsoft.com.br';
const fatal = /MIME type|CORS policy|Firebase não configurado|getEmailStatus|Failed to load resource.*404|Serviço de autenticação indisponível/i;
for (const viewport of [{ width: 1366, height: 768 }, { width: 360, height: 800 }]) {
  test(`PRD abre sem erros críticos e ValoraBot público ${viewport.width}x${viewport.height}`, async ({ page }) => {
    const errors = [];
    page.setViewportSize(viewport);
    page.on('console', msg => { if (fatal.test(msg.text())) errors.push(msg.text()); });
    page.on('pageerror', err => errors.push(err.message));
    await page.goto(url, { waitUntil: 'domcontentloaded' });
    await expect(page.locator('body')).toBeVisible();
    await expect(page.locator('[data-action="toggleBot"], .public-bot-button, .float-help').first()).toBeVisible();
    await page.locator('[data-action="toggleBot"], .public-bot-button, .float-help').first().click();
    await expect(page.locator('#botPanel, .bot-messages').first()).toBeVisible();
    expect(errors, errors.join('\n')).toEqual([]);
  });
}
