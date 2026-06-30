const { test, expect } = require('@playwright/test');

test('admin_valora mobile menu shows full legacy admin list', async ({ page }) => {
  const errors = [];
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(error.message));
  await page.setViewportSize({ width: 414, height: 896 });
  await page.goto('index.html#admin/dashboard');

  const button = page.locator('[data-action="toggleAdminMobileMenu"]').first();
  await expect(button).toBeVisible();
  await button.click();

  const sidebar = page.locator('#adminSidebar.admin-sidebar').first();
  await expect(sidebar).toHaveClass(/open/);
  const items = page.locator('#adminSidebar .admin-nav button, #adminSidebar .side-menu button');
  await expect.poll(async () => items.count()).toBeGreaterThanOrEqual(10);

  for (const label of ['Dashboard', 'Clientes', 'Financeiro', 'Planos', 'Pesquisas', 'Formulários', 'Usuários', 'Respostas']) {
    await expect(sidebar.getByText(label, { exact: false })).toBeVisible();
  }
  expect(await items.count()).toBeGreaterThan(1);
  await expect.poll(async () => sidebar.evaluate(el => el.scrollHeight > el.clientHeight)).toBeTruthy();

  await sidebar.getByRole('button', { name: /Clientes/ }).click();
  await expect(page).toHaveURL(/#admin\/clients/);
  await expect(sidebar).not.toHaveClass(/open/);

  await button.click();
  await expect(sidebar).toHaveClass(/open/);
  await page.keyboard.press('Escape');
  await expect(sidebar).not.toHaveClass(/open/);
  expect(errors).toEqual([]);
});
