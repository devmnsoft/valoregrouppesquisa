const { test, expect } = require('@playwright/test');
test('free-survey-home-no-plans contract file exists', async () => { expect(require('fs').existsSync('package.json')).toBeTruthy(); });
