const { test, expect } = require('@playwright/test');
test('legacy structural smoke', async ({ page }) => {
  await page.goto('file://' + process.cwd() + '/index.html');
  await expect(page.locator('body')).toBeVisible();
});
