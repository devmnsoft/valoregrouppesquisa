const { defineConfig } = require('@playwright/test');
const PORT = process.env.VALORA_PORT || '5088';
const BASE_URL = process.env.VISUAL_BASE_URL || `http://127.0.0.1:${PORT}`;
module.exports = defineConfig({
  testDir: './tests/e2e-web',
  timeout: 45_000,
  expect: { timeout: 8_000 },
  outputDir: 'reports/playwright-web',
  reporter: [['list'], ['html', { outputFolder: 'playwright-report-web', open: 'never' }]],
  use: { baseURL: BASE_URL, trace: 'retain-on-failure', video: 'retain-on-failure', acceptDownloads: true },
  webServer: process.env.VALORA_WEB_SKIP_WEBSERVER ? undefined : { command: `dotnet run --project backend/Valora.Web/Valora.Web.csproj --urls ${BASE_URL}`, url: BASE_URL, reuseExistingServer: !process.env.CI, timeout: 60_000 },
});
