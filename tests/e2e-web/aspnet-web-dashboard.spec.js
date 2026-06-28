const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('dashboard consome me planos e health sem JSON bruto', async ({ page }) => {
  await page.route('**/me', r => r.fulfill({ json: { name: 'Ana', companyName: 'Valora QA' } })); await page.route('**/plans/public', r => r.fulfill({ json: [{ code: 'free', name: 'Free', limits: '25 respostas' }] })); await page.route('**/health', r => r.fulfill({ json: { status: 'ok' } }));
  await page.goto(`${base}/Dashboard`); await expect(page.locator('body')).toContainText('Ana'); await expect(page.locator('body')).not.toContainText('{');
});
