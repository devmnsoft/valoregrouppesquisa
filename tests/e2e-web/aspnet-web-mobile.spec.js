const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('menu mobile abre e fecha', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 }); await page.goto(`${base}/Account/Login`);
  await page.getByLabel('Abrir menu').click(); await expect(page.locator('#mobileSidebar')).toHaveClass(/show/);
  await page.locator('#mobileSidebar .nav-link').first().click(); await expect(page.locator('body')).not.toHaveCSS('overflow-x', 'scroll');
});
