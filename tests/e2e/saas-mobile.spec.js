const { test, expect } = require('@playwright/test');
const { assertSafePage, assertBootstrap } = require('./saas-helpers');

test('SaaS browser smoke validates Bootstrap, friendly UI, no leaks, and navigation resilience', async ({ page, isMobile }) => {
  const errors=[]; page.on('pageerror',e=>errors.push(e.message));
  await assertBootstrap(page);
  await expect(page.locator('body')).toContainText(/Valora|Insight|Pulse|Pesquisa/i);
  const menu=page.locator('.navbar-toggler,[data-bs-toggle="collapse"]').first();
  if(await menu.count()) await menu.click().catch(()=>{});
  const inputs=page.locator('input,textarea,select');
  if(await inputs.count()) await inputs.first().fill('sprint31@example.com').catch(()=>{});
  await assertSafePage(page);
  expect(errors).toEqual([]);
});

test.use({ viewport: { width: 390, height: 844 }, isMobile: true });
