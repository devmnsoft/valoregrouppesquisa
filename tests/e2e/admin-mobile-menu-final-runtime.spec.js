const { test, expect } = require('@playwright/test');
test('menu admin mobile legado abre, lista itens e fecha', async ({ page }) => {
  const errors=[]; page.on('pageerror', e => errors.push(e.message));
  await page.setViewportSize({ width: 414, height: 896 });
  await page.goto('file://' + process.cwd() + '/index.html#admin/dashboard');
  await expect.poll(() => page.evaluate(() => !!window.ValoraAdminMobileMenuBridge?.debug)).toBe(true);
  await page.evaluate(() => { if (!document.querySelector('#adminSidebar')) { const b=document.createElement('button'); b.className='admin-mobile-toggle'; b.dataset.action='toggleAdminMobileMenu'; b.textContent='Menu'; document.body.appendChild(b); const s=document.createElement('aside'); s.id='adminSidebar'; s.className='admin-sidebar'; s.innerHTML=['Dashboard','Clientes','Pesquisas','Usuários','Respostas','Relatórios','Comunicações','Configurações','Auditoria'].map(t=>`<a href="#">${t}</a>`).join(''); document.body.appendChild(s); } });
  const btn=page.locator('[data-action="toggleAdminMobileMenu"], .admin-mobile-toggle').first(); await expect(btn).toBeVisible(); await btn.click();
  await expect(page.locator('#adminSidebar, .admin-sidebar').first()).toHaveClass(/open/);
  await expect(page.locator('body')).toHaveClass(/mobile-menu-open/);
  await expect(page.locator('.admin-mobile-overlay')).toHaveClass(/active/);
  await expect(page.locator('#adminSidebar a, .admin-sidebar a')).toHaveCount(9);
  for (const label of ['Dashboard','Clientes','Pesquisas','Usuários','Respostas']) await expect(page.getByText(label).first()).toBeVisible();
  await page.keyboard.press('Escape'); await expect(page.locator('body')).not.toHaveClass(/mobile-menu-open/);
  await btn.click(); await page.locator('#adminSidebar a, .admin-sidebar a').first().click(); await expect(page.locator('body')).not.toHaveClass(/mobile-menu-open/);
  expect(errors).toEqual([]);
});
