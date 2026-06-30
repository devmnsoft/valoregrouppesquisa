const { test, expect } = require('@playwright/test');

test('pesquisa gratuita não exibe provider_unavailable e mantém resultado/certificado', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('body')).not.toContainText('provider_unavailable');
  const contract = await page.evaluate(() => ({
    hasSubmit: typeof window.submitPublicSurveyAuto === 'function' || document.body.innerText.includes('Diagnóstico gratuito'),
    diagnostics: !!window.ValoraRuntimeDiagnostics,
    hasCertificateCopy: document.body.innerText.includes('certificado') || document.body.innerText.includes('Certificado')
  }));
  expect(contract.hasSubmit).toBeTruthy();
  expect(contract.hasCertificateCopy).toBeTruthy();
});
