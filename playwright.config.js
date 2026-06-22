const { defineConfig } = require('@playwright/test');

const PORT = process.env.VALORA_PORT || '8095';
const BASE_URL = process.env.VISUAL_BASE_URL || `http://127.0.0.1:${PORT}`;

module.exports = defineConfig({
  testDir: './tests/visual',
  timeout: 45_000,
  expect: { timeout: 8_000 },
  outputDir: 'tests/visual/output',
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  use: {
    baseURL: BASE_URL,
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    acceptDownloads: true,
    actionTimeout: 10_000,
    navigationTimeout: 20_000,
  },
  webServer: process.env.VISUAL_SKIP_WEBSERVER ? undefined : {
    command: `VALORA_PORT=${PORT} python server.py`,
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
  },
});
