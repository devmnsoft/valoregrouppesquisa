const { test, expect } = require('@playwright/test');
const base = process.env.VALORA_WEB_URL || 'http://localhost:5088';
test('pesquisa pública renderiza 25 perguntas e salva token temporário', async ({ page }) => {
  await page.route('**/public/surveys/s1/validate', r => r.fulfill({ json: { title: 'Pesquisa QA', questions: Array.from({length:25},(_,i)=>({id:'q'+i,text:'Pergunta '+(i+1)})) } }));
  await page.route('**/public/surveys/s1/responses', r => r.fulfill({ json: { responseId: 'resp1', resultToken: 'segredo-temporario' } }));
  await page.goto(`${base}/s/s1`); await expect(page.getByText('Pesquisa QA')).toBeVisible();
  for (const select of await page.locator('select[name="answers"]').all()) await select.selectOption('5');
  await page.getByRole('button', { name: 'Enviar respostas' }).click(); await expect(page).toHaveURL(/\/r\/resp1/);
});
