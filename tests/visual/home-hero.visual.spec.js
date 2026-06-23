const { test, expect } = require('@playwright/test');

test('home hero premium composition is accessible and stable', async ({ page }) => {
  await page.goto('/index.html');
  const hero = page.locator('.home-hero-v3');
  await expect(hero).toBeVisible();
  await expect(hero.getByText('25 perguntas')).toHaveCount(1);
  await expect(hero.getByText('Valora Insight™')).toHaveCount(1);
  const cta = hero.getByRole('link', { name: /Responder diagnóstico grátis|Conhecer o plano Grátis/ });
  await expect(cta).toBeVisible();
  await expect(cta).toHaveAttribute('href', /(#plans|survey=|index\.html|https?:)/);
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
  expect(overflow).toBe(false);
  const color = await page.locator('.hero-result-panel .result-panel-top span').evaluate(el => getComputedStyle(el).color);
  expect(color).not.toBe('rgb(8, 42, 55)');
  await expect(page.locator('body')).not.toContainText('undefined');
  await page.screenshot({ path: 'test-results/screenshots/home-desktop.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: 'test-results/screenshots/home-mobile.png', fullPage: true });
});
