const { test, expect } = require('@playwright/test');
test('Valora.Web result email flow contract is present', async () => { expect(require('fs').readFileSync('backend/Valora.Web/wwwroot/js/pages/result-page.js','utf8')).toContain('sendResultEmail'); });
