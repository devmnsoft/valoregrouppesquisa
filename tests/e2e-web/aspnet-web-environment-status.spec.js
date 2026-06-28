const { test, expect } = require('@playwright/test');

test('environment status screen has health flow affordances', async ({ page }) => {
  await page.goto('/EnvironmentStatus');
  await expect(page.locator('[data-page="environment-status-page"]')).toBeVisible();
  await expect(page.locator('body')).toContainText(/Status|Ambiente|API/i);
});
