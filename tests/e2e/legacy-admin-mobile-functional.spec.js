const { test, expect } = require('@playwright/test');
test('legacy admin mobile menu opens and closes', async ({ page }) => {
  const errors=[]; page.on('pageerror', e=>errors.push(e.message));
  await page.setViewportSize({ width: 414, height: 896 });
  await page.goto('file://' + process.cwd() + '/index.html#admin/dashboard');
  await page.waitForTimeout(1000);
  const btn = page.locator('[data-action="toggleAdminMobileMenu"], .admin-mobile-toggle').first();
  await expect(btn).toBeVisible({ timeout: 5000 });
  await btn.click();
  await expect(page.locator('#adminSidebar.open, .admin-sidebar.open').first()).toBeVisible({ timeout: 5000 });
  expect(await page.locator('#adminSidebar a, #adminSidebar button, .admin-sidebar a, .admin-sidebar button').count()).toBeGreaterThan(8);
  for (const label of ['Dashboard','Clientes','Planos','Pesquisas','Usuários','Respostas']) await expect(page.getByText(label).first()).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.locator('#adminSidebar.open, .admin-sidebar.open')).toHaveCount(0);
  expect(errors).toEqual([]);
});
