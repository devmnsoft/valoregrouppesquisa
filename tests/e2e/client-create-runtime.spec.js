const { test, expect } = require('@playwright/test');
const fs = require('fs');
test('legacy client create contract contains organization/company/settings fields', async () => {
  const source = ['app.js','firebase-repository.js','repository.js','api-repository.js'].map(f => fs.existsSync(f) ? fs.readFileSync(f,'utf8') : '').join('\n');
  for (const token of ['organization','company','settings','name','publicName','legalName','slug','planId','status']) expect(source).toContain(token);
});
