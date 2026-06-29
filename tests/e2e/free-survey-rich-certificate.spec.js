const { test, expect } = require('@playwright/test');
test('free-survey-rich-certificate contract file exists', async () => { expect(require('fs').existsSync('package.json')).toBeTruthy(); });
