const { test, expect } = require('@playwright/test');

const url = process.env.PRD_URL || 'https://valoragroup.mnsoft.com.br';
const email = process.env.PRD_ADMIN_EMAIL || 'admin@valoragroup.com';
const password = process.env.PRD_ADMIN_PASSWORD;

test('PRD mostra dados importados para Admin Valora no Status do Ambiente', async ({ page }) => {
  test.skip(!password, 'Defina PRD_ADMIN_PASSWORD para executar o login real em PRD.');
  const firestoreErrors = [];
  page.on('console', msg => { if (/permission-denied/i.test(msg.text())) firestoreErrors.push(msg.text()); });
  await page.goto(url, { waitUntil: 'domcontentloaded' });
  await page.getByLabel(/e-mail|email/i).fill(email);
  await page.getByLabel(/senha/i).fill(password);
  await page.getByRole('button', { name: /entrar|login|acessar/i }).click();
  await expect(page.getByText(/Status do Ambiente|Diagnóstico PRD/i)).toBeVisible({ timeout: 30000 });
  await page.getByText(/Status do Ambiente|Diagnóstico PRD/i).click().catch(() => {});
  const body = page.locator('body');
  await expect(body).toContainText(/Planos carregados|plans/i);
  for (const label of ['plans', 'companies', 'users', 'forms', 'surveys']) {
    const count = await page.evaluate((key) => {
      const dbg = window.ValoraFirestoreDebug || {};
      const state = window.state || window.ValoraState || {};
      return key === 'settings' ? Object.keys(state.settings || {}).length : (state[key] || []).length || dbg.collections?.[key]?.count || 0;
    }, label);
    expect(count, `${label} deve ser maior que zero`).toBeGreaterThan(0);
  }
  await expect(body).not.toContainText(/ambiente vazio/i);
  expect(firestoreErrors, firestoreErrors.join('\n')).toEqual([]);
});
