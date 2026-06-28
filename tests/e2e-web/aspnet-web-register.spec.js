const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('cadastro de empresa envia payload real com LGPD', async ({ page }) => {
  await page.route('**/auth/register-company', async route => {
    const body = route.request().postDataJSON();
    expect(body.companyName).toBe('Empresa QA'); expect(body.lgpdAccepted).toBe(true);
    await route.fulfill({ json: { message: 'ok' } });
  });
  await page.goto(`${base}/Account/Register`);
  await page.locator('[name="name"]').fill('Responsável QA'); await page.locator('[name="companyName"]').fill('Empresa QA');
  await page.locator('[name="email"]').fill('qa@empresa.com'); await page.locator('[name="password"]').fill('Senha@2026'); await page.locator('[name="confirmPassword"]').fill('Senha@2026');
  await page.locator('[name="lgpd"]').check(); await page.getByRole('button', { name: 'Criar conta' }).click();
  await expect(page.locator('body')).not.toContainText('JSON.stringify');
});
