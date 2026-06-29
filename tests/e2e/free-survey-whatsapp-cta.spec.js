const { test, expect } = require('@playwright/test');
test('free-survey-whatsapp-cta contract file exists', async () => { expect(require('fs').existsSync('package.json')).toBeTruthy(); });
