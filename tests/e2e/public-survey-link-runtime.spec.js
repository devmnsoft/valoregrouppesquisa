const { test, expect } = require('@playwright/test');
const fs = require('fs');
test('legacy public survey link contract is publicToken-first and friendly', async () => {
  const app = fs.readFileSync('app.js', 'utf8');
  expect(app).toContain('publicToken||survey.token||survey.accessToken');
  expect(app).toContain('Não conseguimos abrir este link de pesquisa.');
  expect(app).toContain('Voltar ao diagnóstico gratuito');
  expect(app).not.toContain('url.searchParams.set(\'token\',survey.tokenHash');
});
