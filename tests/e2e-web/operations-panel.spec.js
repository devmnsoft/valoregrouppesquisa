const { test, expect } = require('@playwright/test');
test('operations panel static contract has no secrets', async () => {
  const fs = require('fs');
  const html = fs.readFileSync('backend/Valora.Web/Views/Operations/Index.cshtml','utf8');
  expect(html).toContain('Painel de saúde operacional');
  expect(html).not.toMatch(/SMTP password|connection string|resultToken|stack trace/i);
});
