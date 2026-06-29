const { test, expect } = require('@playwright/test');
test('Valora.Web communications queue contract is present', async () => { const s=require('fs').readFileSync('backend/Valora.Web/wwwroot/js/pages/communications-page.js','utf8'); expect(s).toContain('processEmailQueue'); expect(s).not.toMatch(/SMTP.*Password/i); });
