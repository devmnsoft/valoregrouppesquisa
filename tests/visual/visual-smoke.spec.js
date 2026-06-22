const { test, expect } = require('@playwright/test');
const fs = require('node:fs');

const STORE_KEY = 'valoraPulseFinal800';
const desktop = { width: 1366, height: 768 };
const mobile = { width: 360, height: 800 };

async function resetDemo(page) {
  await page.goto('/');
  await page.evaluate((key) => localStorage.removeItem(key), STORE_KEY);
}

async function demoLinks(page) {
  await page.goto('/');
  return page.evaluate((key) => {
    const state = JSON.parse(localStorage.getItem(key));
    const survey = state.surveys.find((s) => s.id === 'survey_demo') || state.surveys[0];
    const response = state.responses.find((r) => r.id === 'resp_demo') || state.responses[0];
    return {
      survey: `/index.html?survey=${survey.id}&token=${survey.token}&org=empresa-exemplo`,
      result: `/index.html?result=${response.id}&rt=${response.resultToken}&org=empresa-exemplo`,
    };
  }, STORE_KEY);
}

async function screenshot(page, name) {
  fs.mkdirSync('tests/visual/screenshots', { recursive: true });
  await page.screenshot({ path: `tests/visual/screenshots/${name}`, fullPage: true });
}

async function askBot(page, question) {
  await page.locator('[data-action="toggleBot"]').last().click();
  await expect(page.locator('#botPanel')).toHaveClass(/open/);
  await page.locator('form[data-form="bot"] input[name="message"]').fill(question);
  await page.locator('form[data-form="bot"] button').click();
  await expect(page.locator('#botMessages .typing')).toHaveCount(0);
  await expect(page.locator('#botMessages')).toContainText(question);
}

async function login(page, email, password) {
  await page.goto('/#login');
  await page.locator('input[name="email"]').fill(email);
  await page.locator('input[name="password"]').fill(password);
  await page.locator('[data-login-button]').click();
  await expect(page.locator('.page-title, .breadcrumb')).toBeVisible();
}

for (const [name, viewport] of [['desktop', desktop], ['mobile', mobile]]) {
  test(`home visual smoke ${name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await resetDemo(page);
    await page.goto('/');
    await expect(page.getByRole('heading', { name: /Transforme respostas em decisões/i })).toBeVisible();
    await expect(page.getByText(/Iniciar diagnóstico/i)).toBeVisible();
    await expect(page.getByText(/LGPD e confidencialidade desde o convite/i)).toHaveCount(0);
    await expect(page.locator('[data-action="toggleBot"]')).toBeVisible();
    await screenshot(page, `home-${name}.png`);
  });

  test(`plans visual smoke ${name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await resetDemo(page);
    await page.goto('/#plans');
    await expect(page.getByText(/Essencial|Profissional|Corporativo|Enterprise/)).toBeVisible();
    await expect(page.getByText(/Começar diagnóstico|Falar com especialista|Solicitar proposta/)).toBeVisible();
    await expect(page.locator('[data-action="toggleBot"]')).toBeVisible();
    await screenshot(page, `plans-${name}.png`);
  });

  test(`public survey visual smoke ${name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await resetDemo(page);
    const links = await demoLinks(page);
    await page.goto(links.survey);
    await expect(page.getByText(/Pesquisa segura/i)).toBeVisible();
    await expect(page.getByLabel(/Voltar para Home/i)).toBeVisible();
    await expect(page.getByLabel(/Abrir ajuda/i)).toBeVisible();
    await expect(page.getByLabel(/Abrir ValoraBot/i)).toBeVisible();
    await expect(page.locator('#topbar')).not.toContainText(/Planos|Entrar|Criar ambiente|Empresa Exemplo/);
    await screenshot(page, `public-survey-${name}.png`);
  });

  test(`certificate screen visual smoke ${name}`, async ({ page }) => {
    await page.setViewportSize(viewport);
    await resetDemo(page);
    const links = await demoLinks(page);
    await page.goto(links.result);
    await expect(page.getByText(/Resultado individual/i)).toBeVisible();
    await expect(page.getByText(/Certificado/i)).toBeVisible();
    await expect(page.getByText(/NaN/)).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Baixar certificado em PDF/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Baixar certificado em imagem/i })).toBeVisible();
    await expect(page.locator('#topbar')).not.toContainText(/Empresa Exemplo/);
    await screenshot(page, `certificate-screen-${name}.png`);
  });
}

test('certificate PDF and PNG downloads use safe non-empty files', async ({ page }) => {
  await resetDemo(page);
  const links = await demoLinks(page);
  await page.goto(links.result);
  const [pdf] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: /Baixar certificado em PDF/i }).click(),
  ]);
  expect(pdf.suggestedFilename()).toMatch(/certificado/i);
  expect(pdf.suggestedFilename()).toMatch(/\.pdf$/i);
  expect(pdf.suggestedFilename()).not.toMatch(/undefined|Empresa Exemplo/i);
  const pdfPath = await pdf.path();
  expect(fs.statSync(pdfPath).size).toBeGreaterThan(0);

  const [png] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: /Baixar certificado em imagem/i }).click(),
  ]);
  expect(png.suggestedFilename()).toMatch(/certificado/i);
  expect(png.suggestedFilename()).toMatch(/\.png$/i);
  expect(png.suggestedFilename()).not.toMatch(/undefined|Empresa Exemplo/i);
  const pngPath = await png.path();
  expect(fs.statSync(pngPath).size).toBeGreaterThan(0);
});

test('ValoraBot public, public survey and logged profile smoke', async ({ page }) => {
  await resetDemo(page);
  await page.goto('/');
  for (const q of ['O que é o Valora Pulse?', 'Como respondo uma pesquisa?', 'Quais são os planos?', 'Quero falar com suporte.']) await askBot(page, q);
  await expect(page.getByText(/Falar com atendente/i)).toBeVisible();

  const links = await demoLinks(page);
  await page.goto(links.survey);
  for (const q of ['Como respondo esta pesquisa?', 'Meu link expirou.', 'Meus dados estão seguros?']) await askBot(page, q);
  await expect(page.locator('#botMessages')).not.toContainText(/logs de auditoria|Cadastrar cliente/i);

  const users = [
    ['admin@valoragroup.com', 'Valora@2026', ['Como cadastrar cliente?', 'Como ver logs?']],
    ['gestor@empresa.com', 'Empresa@2026', ['Como cadastrar funcionários?', 'Como criar pesquisa?', 'Como enviar convites?']],
    ['participante@empresa.com', '123456', ['Como baixar meu certificado?', 'Posso criar pesquisa?']],
  ];
  for (const [email, password, questions] of users) {
    await login(page, email, password);
    for (const q of questions) await askBot(page, q);
    if (email.includes('participante')) await expect(page.locator('#botMessages')).toContainText(/não tem permissão|Participante/);
    await page.evaluate((key) => { const s = JSON.parse(localStorage.getItem(key)); s.session = null; localStorage.setItem(key, JSON.stringify(s)); }, STORE_KEY);
  }
});
