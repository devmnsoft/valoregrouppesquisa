const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('login real usa email/senha e não exibe placeholders', async ({ page }) => {
  await page.route('**/auth/login', route => route.fulfill({ json: { token: 'jwt-e2e', email: 'qa@empresa.com', name: 'QA' } }));
  await page.goto(`${base}/Account/Login`);
  await expect(page.getByRole('heading', { name: 'Entrar no Valora Pulse' })).toBeVisible();
  await expect(page.locator('body')).not.toContainText('Executar ação');
  await expect(page.locator('body')).not.toContainText('Tela Bootstrap API-first');
  await page.locator('input[name="email"]').fill('qa@empresa.com');
  await page.locator('input[name="password"]').fill('Senha@2026');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/Dashboard/);
});
