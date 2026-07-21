const { defineConfig, devices } = require('@playwright/test');
module.exports = defineConfig({
  testDir: './tests/playwright',
  timeout: 30_000,
  expect: { timeout: 5_000 },
  reporter: [['list']],
  projects: [
    { name: 'chromium-mobile', use: { ...devices['Pixel 5'], browserName: 'chromium' } },
    { name: 'webkit-mobile', use: { ...devices['iPhone 12'], browserName: 'webkit' } },
  ],
});
