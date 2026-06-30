const { test, expect } = require('@playwright/test');
const fs = require('fs');
test('legacy user create goes through createCompanyUser', async () => {
  const repo = fs.readFileSync('firebase-repository.js','utf8');
  const fn = fs.readFileSync('functions/index.js','utf8');
  expect(repo).toContain('createCompanyUser');
  expect(fn).toContain('admin.auth().createUser');
  expect(fn).toContain('setCustomUserClaims');
  expect(fn).toContain('generatePasswordResetLink');
});
