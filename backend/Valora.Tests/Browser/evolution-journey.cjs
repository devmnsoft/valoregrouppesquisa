const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

(async () => {
  const output = process.env.VALORA_VISUAL_OUTPUT || path.resolve('artifacts/evolution');
  fs.mkdirSync(output, { recursive: true });
  const base = process.env.VALORA_WEB_URL || 'http://localhost:5188';
  const browser = await chromium.launch({ headless: true, channel: process.env.VALORA_BROWSER_CHANNEL || 'msedge' });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  try {
    await page.goto(base + '/Account/Login');
    await page.locator('#email').fill('evolution.qa@valora.test');
    await page.locator('#password').fill('Evolution@123456');
    await Promise.all([page.waitForURL(url => !url.pathname.includes('/Account/Login')), page.getByRole('button', { name: 'Entrar com segurança' }).click()]);
    await page.goto(base + '/Evolution/Cycles/Details/eeeeeeee-0000-4000-8000-000000000020');
    await page.locator('#review-evidence').fill('Arquivo anterior localizado, sem leitura preservada nem versão do processamento.');
    await page.locator('#review-next').fill('Solicitar os registros históricos originais ao responsável.');
    await Promise.all([page.waitForNavigation(), page.getByRole('button', { name: 'Registrar revisão inconclusiva' }).click()]);
    assert.match(await page.locator('#baseline-heading').locator('..').innerText(), /37[,.]50 pontos/);
    assert.match(await page.locator('#baseline-heading').locator('..').innerText(), /Origem histórica ainda não comprovada/);
    for (const width of [1440, 390]) {
      await page.setViewportSize({ width, height: 1000 });
      assert.equal(await page.evaluate(() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) > innerWidth + 2), false);
      await page.screenshot({ path: path.join(output, 'reference-review-' + width + '.png'), fullPage: true });
    }
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.goto(base + '/Evolution');
    assert.equal(new URL(page.url()).pathname, '/Evolution/Cycles');
    await page.getByRole('button', { name: 'Novo ciclo' }).click();
    await page.getByRole('radio', { name: 'Medição calculada' }).check();
    await page.locator('#cycle-result').selectOption('eeeeeeee-0000-4000-8000-000000000014');
    assert.equal(Number(await page.locator('#calculated-score').inputValue()), 60);
    await page.locator('#cycle-name').fill('Jornada QA ' + Date.now());
    await page.locator('#cycle-summary').fill('Acompanhar execução e comparar duas medições rastreáveis.');
    await page.locator('#cycle-start').fill('2026-10-01');
    await page.locator('#cycle-target').fill('85.5');
    await page.locator('#cycle-evidence').fill('Coleta sintética isolada para validar a jornada.');
    await page.screenshot({ path: path.join(output, 'creation-desktop.png'), fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({ path: path.join(output, 'creation-mobile.png'), fullPage: true });
    await page.setViewportSize({ width: 1440, height: 1000 });
    await Promise.all([page.waitForURL('**/Evolution/Cycles/Details/**'), page.getByRole('button', { name: 'Criar ciclo', exact: true }).click()]);
    const cycleUrl = page.url();
    await page.locator('#Evidence').fill('Responsáveis revisaram as atividades da semana.');
    await page.locator('#Interpretation').fill('Execução registrada sem atribuir efeito quantitativo.');
    await page.locator('#Recommendation').fill('Comparar com a coleta posterior.');
    await Promise.all([page.waitForNavigation(), page.getByRole('button', { name: 'Registrar acompanhamento' }).click()]);
    await page.locator('#SourceSurveyId').selectOption('eeeeeeee-0000-4000-8000-000000000015');
    await Promise.all([page.waitForNavigation(), page.getByRole('button', { name: 'Comparar medições' }).click()]);
    assert.match(await page.locator('#comparison-preview').innerText(), /10[,.]00 pontos/);
    await page.screenshot({ path: path.join(output, 'comparison-desktop.png'), fullPage: true });
    await Promise.all([page.waitForNavigation(), page.getByRole('button', { name: 'Confirmar reavaliação' }).click()]);
    await page.goto(cycleUrl);
    await Promise.all([page.waitForURL('**/ActionCenter/Plans?**'), page.getByRole('link', { name: 'Consultar planos vinculados' }).click()]);
    assert.equal(new URL(page.url()).searchParams.get('diagnosticId'), 'eeeeeeee-0000-4000-8000-000000000014');
    await Promise.all([page.waitForURL('**/Evolution/Cycles/Details/**'), page.getByRole('link', { name: 'Voltar ao contexto' }).click()]);
    assert.match(await page.locator('#baseline-heading').locator('..').innerText(), /60[,.]00 pontos/);
    assert.match(await page.locator('#reevaluation-heading').locator('..').innerText(), /10[,.]00 pontos/);
    for (const viewport of [{ width: 1440, height: 1000 }, { width: 768, height: 1024 }, { width: 390, height: 844 }]) {
      await page.setViewportSize(viewport);
      await page.screenshot({ path: path.join(output, 'cycle-' + viewport.width + '.png'), fullPage: true });
      const overflow = await page.evaluate(() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) > window.innerWidth + 2);
      assert.equal(overflow, false, 'Horizontal overflow at ' + viewport.width);
      const clipped = await page.locator('.evolution-details input,.evolution-details textarea,.evolution-details select,.evolution-details button').evaluateAll(elements => elements.filter(element => {
        const rect = element.getBoundingClientRect();
        return rect.width > 0 && (rect.right > window.innerWidth + 2 || rect.left < -2);
      }).map(element => element.id || element.textContent));
      assert.deepEqual(clipped, [], 'Clipped controls at ' + viewport.width);
      if (viewport.width < 992) {
        await page.locator('[data-action="toggle-navigation"]').click();
        await page.locator('#mobileSidebar.show').waitFor();
        await page.keyboard.press('Escape');
        await page.locator('#mobileSidebar.show').waitFor({ state: 'hidden' });
      }
    }
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.locator('#learning').fill('A comparação preservou as leituras e seus limites de interpretação.');
    await page.locator('#pendingTreatment').fill('As atividades continuarão no plano e serão revisadas pela equipe.');
    await page.getByRole('checkbox', { name: /Confirmo o encerramento/ }).check();
    await Promise.all([page.waitForNavigation(), page.getByRole('button', { name: 'Encerrar ciclo', exact: true }).click()]);
    assert.match(await page.locator('#objective-heading').locator('..').innerText(), /Encerrado/);
    await page.goto(base + '/Diagnostics/eeeeeeee-0000-4000-8000-000000000014/Workspace?tab=evolution');
    await page.getByRole('link', { name: 'Continuar ciclo' }).first().waitFor();
    await page.screenshot({ path: path.join(output, 'diagnostic-evolution.png'), fullPage: true });
    await page.goto(base + '/Evolution/Cycles');
    await page.getByRole('button', { name: 'Novo ciclo' }).click();
    await page.getByRole('radio', { name: 'Valor manual' }).check();
    await page.locator('#cycle-name').fill('Referência manual QA ' + Date.now());
    await page.locator('#cycle-summary').fill('Validar referência informada e dados mantidos após erro.');
    await page.locator('#cycle-baseline').fill('12.5');
    await page.locator('#cycle-target').fill('13.75');
    await page.locator('#cycle-start').fill('2026-10-01');
    await page.locator('#cycle-end').fill('2026-09-30');
    await page.locator('#cycle-evidence').fill('Valor manual sintético para QA.');
    await Promise.all([page.waitForNavigation(), page.getByRole('button', { name: 'Criar ciclo', exact: true }).click()]);
    assert.equal(await page.locator('#cycle-baseline').inputValue(), '12.5');
    assert.equal(await page.locator('#cycle-target').inputValue(), '13.75');
    await page.locator('#cycle-end').fill('');
    await Promise.all([page.waitForURL('**/Evolution/Cycles/Details/**'), page.getByRole('button', { name: 'Criar ciclo', exact: true }).click()]);
    assert.match(await page.locator('#baseline-heading').locator('..').innerText(), /12[,.]50 pontos/);
    await page.locator('#learning').fill('A referência manual permaneceu identificada como informada.');
    await page.locator('#pendingTreatment').fill('Sem plano vinculado a esta referência manual.');
    await page.getByRole('checkbox', { name: /Confirmo o encerramento/ }).check();
    await Promise.all([page.waitForNavigation(), page.getByRole('button', { name: 'Encerrar ciclo', exact: true }).click()]);
    assert.match(await page.locator('#learning-heading').locator('..').innerText(), /Ciclo encerrado sem reavaliação quantitativa/);
    assert.deepEqual(errors, []);
    console.log(JSON.stringify({ cycleUrl, screenshots: output, browserErrors: errors }));
  } catch (error) {
    await page.screenshot({ path: path.join(output, 'failure.png'), fullPage: true });
    console.error({ url: page.url(), text: (await page.locator('body').innerText()).slice(0, 4000), browserErrors: errors });
    throw error;
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
