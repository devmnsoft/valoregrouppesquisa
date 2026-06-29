const { test, expect } = require('@playwright/test');
test('certificate validation page contract', async () => {
  const fs = require('fs');
  const html = fs.readFileSync('backend/Valora.Web/Views/Certificates/Validate.cshtml','utf8');
  expect(html).toContain('Validar certificado');
});
