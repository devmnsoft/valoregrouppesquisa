const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('status do ambiente exibe correlationId em falha controlada', async ({ page }) => {
  await page.route('**/health**', r => r.fulfill({ status: 503, json: { message: 'indisponível', correlationId: 'corr-e2e' } }));
  await page.goto(`${base}/EnvironmentStatus`); await expect(page.locator('body')).toContainText('CorrelationId');
});
