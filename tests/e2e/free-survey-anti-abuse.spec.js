const { test, expect } = require('@playwright/test');
test('free survey anti abuse contract', async () => {
  const fs = require('fs');
  const source = fs.readFileSync('backend/Valora.Api/Controllers/PublicSurveysController.cs','utf8');
  expect(source).toContain('FREE_SURVEY_SECURITY_BLOCKED');
  expect(source).toContain('suspicious-user-agent');
});
