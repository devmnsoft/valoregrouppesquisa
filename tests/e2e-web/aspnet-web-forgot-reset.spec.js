const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('forgot e reset password usam campos seguros', async ({ page }) => {
  await page.route('**/auth/forgot-password', route => route.fulfill({ json: { message: 'ok' } }));
  await page.goto(`${base}/Account/ForgotPassword`); await page.locator('[name="email"]').fill('qa@empresa.com'); await page.getByRole('button', { name: 'Enviar instruções' }).click();
  await expect(page.locator('.success-state')).toContainText('Se o e-mail estiver cadastrado');
  await page.route('**/auth/reset-password', route => route.fulfill({ json: { message: 'ok' } }));
  await page.goto(`${base}/Account/ResetPassword`); await page.locator('[name="token"]').fill('codigo-seguro'); await page.locator('[name="password"]').fill('Senha@2026'); await page.locator('[name="confirmPassword"]').fill('Senha@2026');
  await page.getByRole('button', { name: 'Redefinir senha' }).click(); await expect(page.locator('body')).not.toContainText('stack trace');
});
