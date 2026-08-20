const { test, expect } = require('@playwright/test');
const fs = require('node:fs');

test('hidden remains display none across structural component display modes', async ({ page }) => {
  const tokens = fs.readFileSync('backend/Valora.Web/wwwroot/css/design-system/tokens.css', 'utf8');
  await page.setContent(`<style>${tokens}</style><style>
    .grid{display:grid}.flex{display:flex}.inline{display:inline-flex}.fixed{display:block!important}
  </style><main>
    <div hidden class="grid" data-kind="score"></div><span hidden class="inline" data-kind="badge"></span>
    <aside hidden class="flex" data-kind="popover"></aside><dialog hidden class="fixed" data-kind="dialog"></dialog>
    <nav hidden class="flex" data-kind="mobile-menu"></nav><div hidden class="grid" data-kind="loading"></div>
    <table hidden class="fixed" data-kind="table"></table><p hidden class="flex" data-kind="empty"></p>
    <div hidden class="grid" data-kind="alert"></div>
  </main>`);
  for (const element of await page.locator('[hidden]').all()) {
    await expect(element).toHaveCSS('display', 'none');
  }
});
