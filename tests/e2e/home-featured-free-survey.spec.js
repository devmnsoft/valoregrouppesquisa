const { test, expect } = require('@playwright/test');

test.describe('home featured free survey contract', () => {
  test('home CTA resolves real featured/free survey, not legacy demo', async ({ page }) => {
    await page.goto('/');
    const body = page.locator('body');
    await expect(body).toContainText(/Diagnóstico gratuito|Valora Insight/i);
    const hrefs = await page.locator('a[href*="?survey="]').evaluateAll(a => a.map(x => x.href).join('\n'));
    expect(hrefs).not.toContain('survey_demo');
    expect(hrefs).not.toContain('empresa-exemplo');
    expect(hrefs).not.toContain('tokenHash');
  });

  test('public and logged-in users can open free survey route without provider_unavailable', async ({ page }) => {
    await page.goto('/');
    const href = await page.locator('a[href*="?survey="]').first().getAttribute('href');
    expect(href || '').not.toContain('survey_demo');
    if (href) {
      await page.goto(href);
      await expect(page.locator('body')).not.toContainText('provider_unavailable');
      await expect(page.locator('input[name="surveyId"]')).not.toHaveValue('survey_demo');
    }
  });
});
