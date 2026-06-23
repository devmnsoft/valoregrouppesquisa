const { test, expect } = require('@playwright/test');
test('produção não chama endpoints locais de e-mail/log', async ({ page }) => {
  const calls=[];page.on('request', r=>calls.push(r.url()));
  await page.goto('/'); await page.waitForTimeout(500);
  expect(calls.some(u=>/\/api\/email\/|\/api\/outbox|cloudfunctions\.net\/(getEmailStatus|logServerEvent)/.test(u))).toBeFalsy();
});
