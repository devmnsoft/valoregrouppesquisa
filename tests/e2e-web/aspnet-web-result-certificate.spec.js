const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('resultado não mostra valores inválidos e certificado valida fallback', async ({ page }) => {
  await page.addInitScript(() => sessionStorage.setItem('valora.publicResultToken.resp1','token'));
  await page.route('**/public/results/resp1', r => r.fulfill({ json: { score: 88, level: 'Alto', dimensions: [{ name: 'Cultura', score: 90 }], recommendation: 'Manter rotina.' } }));
  await page.goto(`${base}/r/resp1`); await expect(page.locator('body')).toContainText('Score 88'); await expect(page.locator('body')).not.toContainText('undefined'); await expect(page.locator('body')).not.toContainText('NaN'); await expect(page.locator('body')).not.toContainText('[object Object]');
  await page.route('**/certificates/CERT/validate', r => r.fulfill({ json: { valid: true } })); await page.goto(`${base}/Certificates/Validate/CERT`); await page.getByRole('button', { name: 'Validar código' }).click();
});
