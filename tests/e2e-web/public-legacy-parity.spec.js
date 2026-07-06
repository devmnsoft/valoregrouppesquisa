const { test, expect } = require('@playwright/test');
const pages=['/','/diagnostico-gratuito','/resultado/fixture-seguro','/certificado/fixture-seguro','/lgpd'];
test.describe('paridade pública legado ASP.NET',()=>{
  for(const route of pages){ test(`página pública sem sidebar ${route}`, async({page})=>{ await page.goto(route); await expect(page.locator('.topbar')).toBeVisible(); await expect(page.locator('.sidebar,.app-shell')).toHaveCount(0); }); }
  test('home expõe hero, diagnóstico, WhatsApp e LGPD',async({page})=>{ await page.goto('/'); await expect(page.getByRole('heading',{name:/diagnóstico gratuito/i})).toBeVisible(); await expect(page.getByRole('link',{name:/fazer diagnóstico gratuito/i})).toBeVisible(); await expect(page.getByText(/whatsapp/i).first()).toBeVisible(); await expect(page.getByText(/LGPD/i).first()).toBeVisible(); });
  test('fluxo público básico com fixture segura',async({page})=>{ await page.goto('/diagnostico-gratuito'); await expect(page.locator('[data-free-diagnostic-form]')).toBeVisible(); await page.goto('/pesquisa/fixture-seguro/responder'); await expect(page.locator('[data-public-survey-form]')).toBeVisible(); await page.goto('/resultado/fixture-seguro'); await expect(page.locator('[data-result-card]')).toBeVisible(); await page.goto('/certificado/fixture-seguro'); await expect(page.locator('[data-certificate-card]')).toBeVisible(); });
});
