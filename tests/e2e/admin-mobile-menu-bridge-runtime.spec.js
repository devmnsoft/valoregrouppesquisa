const { test, expect } = require('@playwright/test');

test('legacy mobile menu bridge opens admin sidebar on real click', async ({ page }) => {
  const errors = [];

  page.on('console', message => {
    if (message.type() === 'error') errors.push(message.text());
  });

  page.on('pageerror', error => errors.push(error.message));

  await page.setViewportSize({ width: 414, height: 896 });
  await page.goto('index.html#admin/dashboard');

  await expect.poll(async () => page.evaluate(() => !!window.ValoraAdminMobileMenuBridge)).toBeTruthy();

  const debugBefore = await page.evaluate(() => window.ValoraAdminMobileMenuBridge.debug());
  expect(debugBefore.hasButton).toBeTruthy();
  expect(debugBefore.hasSidebar).toBeTruthy();

  const button = page.locator('[data-action="toggleAdminMobileMenu"], .admin-mobile-toggle').first();
  await expect(button).toBeVisible();

  await button.click({ force: true });

  const debugAfter = await page.evaluate(() => window.ValoraAdminMobileMenuBridge.debug());

  expect(debugAfter.sidebarClass).toContain('open');
  expect(debugAfter.bodyClass).toContain('mobile-menu-open');
  expect(debugAfter.buttonExpanded).toBe('true');

  const sidebar = page.locator('#adminSidebar, .admin-sidebar').first();
  await expect(sidebar).toHaveClass(/open/);

  const items = page.locator('#adminSidebar .admin-nav button, #adminSidebar .side-menu button, .admin-sidebar .admin-nav button, .admin-sidebar .side-menu button');
  await expect.poll(async () => items.count()).toBeGreaterThanOrEqual(8);

  for (const label of ['Dashboard', 'Clientes', 'Pesquisas', 'Usuários', 'Respostas']) {
    await expect(sidebar.getByText(label, { exact: false })).toBeVisible();
  }

  await page.keyboard.press('Escape');
  await expect(sidebar).not.toHaveClass(/open/);

  expect(errors).toEqual([]);
});
