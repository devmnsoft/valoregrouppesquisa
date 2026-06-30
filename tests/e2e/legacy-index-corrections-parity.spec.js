const { test, expect } = require('@playwright/test');
const fs = require('fs');
test('legacy index has sprint 53 parity corrections', async () => {
  const app = fs.readFileSync('app.js','utf8');
  expect(app).toContain('function isSurveyExpired');
  expect(app).toContain('function ensureSurveyPublicLink');
  expect(app).toContain('function openAdminMobileMenu');
});
