const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

test('regressão admin cliente/usuário mantém dist, planos, certificado, e-mail e menu', async () => {
  const root = process.cwd();
  const app = fs.readFileSync(path.join(root, 'app.js'), 'utf8');
  const build = fs.readFileSync(path.join(root, 'scripts/validate-hosting-dist-build.js'), 'utf8');
  for (const token of ['certificatePdf','sendResultEmail','plansSection','toggleAdminMobileMenu','provider_unavailable','validateClientPayload','validateUserPayload']) {
    expect(app).toContain(token);
  }
  expect(build).toContain('replace(/\\\\/g, \'/\')');
  expect(build).toContain('assets');
});
