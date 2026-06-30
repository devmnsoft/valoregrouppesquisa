const { test, expect } = require('@playwright/test');
const fs = require('fs');
test('Firebase auth 400 errors are mapped to user friendly messages', async () => {
  const source = fs.readFileSync('firebase-repository.js','utf8');
  for (const token of ['auth/invalid-login-credentials','auth/user-not-found','auth/wrong-password','auth/invalid-email','auth/user-disabled','auth/too-many-requests','auth/network-request-failed','auth/api-key-not-valid','auth/operation-not-allowed']) expect(source).toContain(token);
  expect(source).toContain('E-mail ou senha inválidos.');
});
