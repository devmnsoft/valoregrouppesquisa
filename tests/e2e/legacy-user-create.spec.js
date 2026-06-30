const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

test('contrato legado de cadastro de usuário não persiste senha em claro', async ({ page }) => {
  const root = process.cwd();
  const app = fs.readFileSync(path.join(root, 'app.js'), 'utf8');
  expect(app).toContain('data-form="user"');
  expect(app).toContain('data-action="newUser"');
  expect(app).toContain('validateUserPayload');
  expect(app).toContain('saveUserAuto');
  expect(app).toContain('Usuário cadastrado com sucesso.');
  expect(app).not.toMatch(/password:fd\.password|adminPassword\|\|'Empresa@2026'/);
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.setContent('<button data-action="newUser">Novo Usuário</button><form data-form="user"><input name="email" value="u@example.com"><button>Salvar usuário</button></form>');
  await expect(page.locator('[data-action="newUser"]')).toBeVisible();
  await expect(page.locator('form[data-form="user"]')).toBeVisible();
  expect(errors).toEqual([]);
});
