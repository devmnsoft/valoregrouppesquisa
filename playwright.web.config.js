const { defineConfig } = require('@playwright/test');
module.exports = defineConfig({ testDir: './tests/e2e-web', timeout: 45_000, expect: { timeout: 8_000 }, reporter: [['list']], use: { trace: 'retain-on-failure' } });
