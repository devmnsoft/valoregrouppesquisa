const { defineConfig } = require('@playwright/test');
const PORT = process.env.VALORA_PORT || '8095';
const BASE_URL = process.env.VISUAL_BASE_URL || `http://127.0.0.1:${PORT}`;
module.exports = defineConfig({
  testDir: './tests/e2e',
  timeout: 45_000,
  expect: { timeout: 8_000 },
  outputDir: 'reports/playwright',
  reporter: [['list'], ['html', { outputFolder: 'playwright-report-e2e', open: 'never' }]],
  use: { baseURL: BASE_URL, trace: 'retain-on-failure', video: 'retain-on-failure', acceptDownloads: true },
  webServer: process.env.VISUAL_SKIP_WEBSERVER ? undefined : { command: `VALORA_PORT=${PORT} python server.py`, url: BASE_URL, reuseExistingServer: !process.env.CI, timeout: 30_000 },
});
