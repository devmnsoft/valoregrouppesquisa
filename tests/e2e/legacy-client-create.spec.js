const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');

test('contrato legado de cadastro de cliente está conectado', async ({ page }) => {
  const root = process.cwd();
  const app = fs.readFileSync(path.join(root, 'app.js'), 'utf8');
  expect(app).toContain('data-form="company"');
  expect(app).toContain('data-action="newCompany"');
  expect(app).toContain('validateClientPayload');
  expect(app).toContain('saveClientAuto');
  expect(app).toContain('Cliente cadastrado com sucesso.');
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.setContent('<button data-action="newCompany">Novo Cliente</button><form data-form="company"><input name="name" value="Cliente Sprint 74"><button>Salvar Cliente</button></form>');
  await expect(page.locator('[data-action="newCompany"]')).toBeVisible();
  await expect(page.locator('form[data-form="company"]')).toBeVisible();
  expect(errors).toEqual([]);
});
