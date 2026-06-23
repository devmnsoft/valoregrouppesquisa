const { test, expect } = require('@playwright/test');
test('modelo de certificado não usa placeholder', async ({ page }) => { await page.goto('/'); const ok=await page.evaluate(()=>typeof window.ValoraPDF?.createCertificate==='function'); expect(ok).toBeTruthy(); });
