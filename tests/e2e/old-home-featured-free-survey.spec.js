const { test, expect } = require('@playwright/test');

test.describe('old home featured free survey', () => {
  test('home uses real featured/free survey CTA and never legacy demo values', async ({ page }) => {
    const submitted=[];
    await page.addInitScript(() => {
      window.__submitPayloads=[];
      window.submitPublicSurveyAuto = async (payload) => {
        window.__submitPayloads.push(payload);
        if (payload.surveyId === 'survey_demo') throw new Error('survey_demo submitted');
        return { ok:true, responseId:'resp_real_featured_home', resultToken:'result_token_real_featured_home', accessToken:'result_token_real_featured_home', score:{ rawScore:100, maxScore:125 }, level:{ label:'Estruturada' } };
      };
    });
    await page.goto('/');
    await expect(page.locator('body')).toContainText(/Diagnóstico gratuito|Valora Insight/i);
    const hrefs=(await page.locator('a').evaluateAll(a=>a.map(x=>x.href))).join('\n');
    expect(hrefs).not.toContain('survey_demo');
    expect(hrefs).not.toContain('empresa-exemplo');
    const cta=page.getByRole('link', { name:/Responder diagnóstico grátis/i }).first();
    await expect(cta).toBeVisible();
    const href=await cta.getAttribute('href');
    expect(href||'').not.toContain('survey_demo');
    expect(href||'').not.toContain('empresa-exemplo');
  });
});
