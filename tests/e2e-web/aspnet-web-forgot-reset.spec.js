const { test, expect } = require('@playwright/test');

test('forgot and reset password screens render without exposing secrets', async ({ page }) => {
  await page.goto('/Account/ForgotPassword');
  await expect(page.locator('[data-page="forgot-password-page"]')).toBeVisible();
  await expect(page.locator('input[name="email"]')).toBeVisible();

  await page.goto('/Account/ResetPassword');
  await expect(page.locator('[data-page="reset-password-page"]')).toBeVisible();
  await expect(page.locator('body')).not.toContainText('stack trace');
});
