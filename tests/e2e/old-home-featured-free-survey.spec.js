const {test,expect}=require('@playwright/test');

test.describe('old home featured free survey contract',()=>{
  test('home CTA resolves real featured survey and signup failures are friendly',async({page})=>{
    const errors=[];page.on('console',m=>{const t=m.text();if(/Provider firebase não possui método registerCompany|provider_unavailable|lookupCnpj.*401|survey_demo|empresa-exemplo/.test(t))errors.push(t)});
    await page.goto('/',{waitUntil:'domcontentloaded'});
    await expect(page.locator('body')).toContainText(/Diagnóstico gratuito|Valora Insight/i);
    const cta=page.locator('.featured-survey-section a.btn-primary').first();
    await expect(cta).toBeVisible();
    const href=await cta.getAttribute('href');
    expect(href||'').not.toContain('survey_demo');
    expect(href||'').not.toContain('empresa-exemplo');
    expect(href||'').not.toContain('tokenHash');
    expect(href||'').toMatch(/\?survey=|#plans/);
    expect(errors.join('\n')).not.toMatch(/Provider firebase não possui método registerCompany|provider_unavailable|lookupCnpj.*401/);
  });
});
